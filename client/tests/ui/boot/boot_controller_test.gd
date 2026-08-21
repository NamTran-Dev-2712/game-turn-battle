# Test BootController — Phase 17.
# Chứng minh: happy-path boot (mock health ok + config ok) → tới main hub qua SceneRouter;
# health fail/mất mạng → màn lỗi + KHÔNG điều hướng; config lỗi = best-effort (vẫn vào hub);
# retry sau lỗi → vào hub, điều hướng đúng MỘT lần (không trùng listener/điều hướng).
# Tất định — NetworkClient thật + FakeHttpTransport, SceneRouter = stub (không load hub thật),
# ConfigProvider thật với cache tạm. (docs/testing/godot-testing.md)
extends GdUnitTestSuite

const _NETWORK_CLIENT := preload("res://src/core/net/network_client.gd")
const _CONFIG_PROVIDER := preload("res://src/core/config/config_provider.gd")
const _MAIN_HUB_PATH := "res://src/ui/main_hub/main_hub.tscn"

var _cache_dir: String
var _network_errors: Array = []


func before_test() -> void:
	_cache_dir = "user://test_boot_%d" % Time.get_ticks_usec()
	_network_errors = []
	EventBus.subscribe(&"network_error", _on_network_error)


func after_test() -> void:
	EventBus.unsubscribe(&"network_error", _on_network_error)
	_remove_dir_recursive(_cache_dir)


func _on_network_error(payload) -> void:
	_network_errors.append(payload)


# ── Helpers ──────────────────────────────────────────────────────────────────────────────────────

# Envelope config bundle tối giản (không cần entry — chỉ cần version hợp lệ).
func _bundle(version: int) -> Dictionary:
	return {"config_version": "config@v%d" % version, "schema_version": 1, "data": {}}


# Wire NetworkClient thật (fake transport) + ConfigProvider (cache tạm) + stub router + controller.
func _build(fake: FakeHttpTransport) -> Dictionary:
	var net: Node = _NETWORK_CLIENT.new()
	add_child(net)
	auto_free(net)
	net.set_transport(fake)
	var provider: Node = _CONFIG_PROVIDER.new()
	provider.cache_dir = _cache_dir
	provider.network_client = net
	add_child(provider)
	auto_free(provider)
	var stub := _StubRouter.new()
	add_child(stub)
	auto_free(stub)
	var controller := BootController.new()
	controller.auto_start = false
	controller.network_client = net
	controller.config_provider = provider
	controller.scene_router = stub
	controller.main_hub_path = _MAIN_HUB_PATH
	add_child(controller)
	auto_free(controller)
	return {"net": net, "provider": provider, "stub": stub, "controller": controller}


func _remove_dir_recursive(path: String) -> void:
	var dir := DirAccess.open(path)
	if dir == null:
		return
	dir.list_dir_begin()
	var name := dir.get_next()
	while name != "":
		if dir.current_is_dir():
			_remove_dir_recursive(path.path_join(name))
		else:
			dir.remove(path.path_join(name))
		name = dir.get_next()
	dir.list_dir_end()
	DirAccess.remove_absolute(path)


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_happy_path_reaches_main_hub() -> void:
	var fake := FakeHttpTransport.new()
	fake.queue_ok(200, '{"status":"ok"}')  # /health
	fake.queue_ok(200, '{"version":{"bundle":1,"schema":1}}')  # config version
	fake.queue_ok(200, JSON.stringify(_bundle(1)))  # config bundle
	var ctx := _build(fake)
	var controller: BootController = ctx["controller"]
	var succeeded: Array = []
	controller.boot_succeeded.connect(func() -> void: succeeded.append(true))
	await controller.start()
	assert_int(succeeded.size()).is_equal(1)
	assert_int(controller.state).is_equal(BootController.State.DONE)
	assert_str(ctx["stub"].last_goto).is_equal(_MAIN_HUB_PATH)
	assert_int(ctx["stub"].goto_count).is_equal(1)
	assert_int(ctx["stub"].clear_count).is_equal(1)  # clear_history sau khi vào hub
	assert_int(_network_errors.size()).is_equal(0)  # mọi bước thành công
	assert_int(ctx["provider"].current_version()).is_equal(1)  # config đã áp


func test_config_failure_is_graceful_and_boot_still_reaches_hub() -> void:
	# Health ok nhưng config endpoint lỗi (Config Service = phase 21) ⇒ best-effort, vẫn vào hub.
	var fake := FakeHttpTransport.new()
	fake.queue_ok(200, '{"status":"ok"}')  # /health ok
	fake.queue_transport(HTTPRequest.RESULT_CANT_CONNECT)  # config version fail
	var ctx := _build(fake)
	var controller: BootController = ctx["controller"]
	var succeeded: Array = []
	controller.boot_succeeded.connect(func() -> void: succeeded.append(true))
	await controller.start()
	assert_int(succeeded.size()).is_equal(1)
	assert_str(ctx["stub"].last_goto).is_equal(_MAIN_HUB_PATH)
	assert_int(ctx["provider"].current_version()).is_equal(0)  # config không áp được (giữ cache rỗng)


func test_health_failure_shows_error_and_does_not_navigate() -> void:
	var fake := FakeHttpTransport.new()
	fake.queue_transport(HTTPRequest.RESULT_CANT_CONNECT)  # /health mất mạng
	var ctx := _build(fake)
	var controller: BootController = ctx["controller"]
	var failed: Array = []
	controller.boot_failed.connect(func(msg) -> void: failed.append(msg))
	await controller.start()
	assert_int(failed.size()).is_equal(1)
	assert_int(controller.state).is_equal(BootController.State.FAILED)
	assert_int(ctx["stub"].goto_count).is_equal(0)  # KHÔNG vào hub khi health lỗi
	# Thông báo an toàn — không lộ chi tiết nội bộ.
	assert_str(failed[0]).is_equal(BootController.SAFE_ERROR_MESSAGE)


func test_retry_after_failure_reaches_hub_without_duplicate_navigation() -> void:
	# Lượt 1: health mất mạng → FAILED, không điều hướng.
	var fake := FakeHttpTransport.new()
	fake.queue_transport(HTTPRequest.RESULT_CANT_CONNECT)
	var ctx := _build(fake)
	var controller: BootController = ctx["controller"]
	var stub = ctx["stub"]
	var failed: Array = []
	var succeeded: Array = []
	controller.boot_failed.connect(func(_msg) -> void: failed.append(true))
	controller.boot_succeeded.connect(func() -> void: succeeded.append(true))
	await controller.start()
	assert_int(failed.size()).is_equal(1)
	assert_int(stub.goto_count).is_equal(0)

	# Mạng phục hồi: đổi transport sang phản hồi thành công cho lượt retry.
	var fake_ok := FakeHttpTransport.new()
	fake_ok.queue_ok(200, '{"status":"ok"}')
	fake_ok.queue_ok(200, '{"version":{"bundle":1,"schema":1}}')
	fake_ok.queue_ok(200, JSON.stringify(_bundle(1)))
	ctx["net"].set_transport(fake_ok)

	await controller.retry()
	assert_int(succeeded.size()).is_equal(1)
	assert_int(stub.goto_count).is_equal(1)  # điều hướng đúng MỘT lần (không trùng)
	assert_str(stub.last_goto).is_equal(_MAIN_HUB_PATH)


func test_boot_scenes_exist() -> void:
	# Scene boot/app_root/main_hub tồn tại (điều hướng thật không vỡ path).
	assert_bool(ResourceLoader.exists("res://src/ui/app_root.tscn")).is_true()
	assert_bool(ResourceLoader.exists("res://src/ui/boot/boot.tscn")).is_true()
	assert_bool(ResourceLoader.exists(_MAIN_HUB_PATH)).is_true()


# SceneRouter giả: ghi lại điều hướng, không load scene thật (test cô lập boot logic).
class _StubRouter extends Node:
	var last_goto: String = ""
	var goto_count: int = 0
	var clear_count: int = 0

	func goto_scene(path: String) -> bool:
		last_goto = path
		goto_count += 1
		return true

	func clear_history() -> void:
		clear_count += 1
