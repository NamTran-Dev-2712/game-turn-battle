# Test ConfigProvider — Phase 16.
# Chứng minh: áp bundle version X → query id trả đúng data (data-driven, không hardcode); đổi X→X+1
# → cache mới + active đổi + phát `config_updated` (KHÔNG rebuild client); cache đĩa BẤT BIẾN + boot
# nạp lại (offline-view); version cũ không bị ghi đè; thiếu/hỏng cache → không crash; so version từ
# server (qua NetworkClient giả) → tải+áp bundle mới; autoload có mặt.
# Tất định — không network/animation/wall-clock thật (docs/testing/godot-testing.md).
extends GdUnitTestSuite

const _CONFIG_PROVIDER := preload("res://src/core/config/config_provider.gd")
const _NETWORK_CLIENT := preload("res://src/core/net/network_client.gd")

var _provider: Node
var _cache_dir: String
var _config_updated: Array = []


func before_test() -> void:
	_config_updated = []
	EventBus.subscribe(&"config_updated", _on_config_updated)
	_cache_dir = "user://test_config_%d" % Time.get_ticks_usec()


func after_test() -> void:
	EventBus.unsubscribe(&"config_updated", _on_config_updated)
	_remove_dir_recursive(_cache_dir)


func _on_config_updated(payload) -> void:
	_config_updated.append(payload)


# ── Helpers ──────────────────────────────────────────────────────────────────────────────────────

# Tạo một provider trỏ vào `_cache_dir`, add_child (kích hoạt _ready = boot nạp đĩa), auto_free.
func _make_provider() -> Node:
	var provider: Node = _CONFIG_PROVIDER.new()
	provider.cache_dir = _cache_dir
	add_child(provider)
	auto_free(provider)
	return provider


# Bundle mẫu: envelope config@vN + một hero với số liệu (chứng minh đọc theo dữ liệu, không hardcode).
func _bundle(version: int, hero_rarity: int, hero_hp: int) -> Dictionary:
	return {
		"config_version": "config@v%d" % version,
		"schema_version": 1,
		"data": {
			"hero": [
				{
					"schema_version": 1,
					"id": "hero_sample",
					"faction": "none",
					"class": "warrior",
					"element": "fire",
					"role": "tank",
					"rarity": hero_rarity,
					"base_stats": {"hp": hero_hp, "atk": 0, "def": 0, "spd": 0},
					"skills": ["skill_sample_basic"],
				},
			],
		},
	}


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

func test_apply_bundle_then_query_by_id_returns_data() -> void:
	_provider = _make_provider()
	assert_bool(_provider.apply_bundle(_bundle(1, 3, 100))).is_true()
	assert_int(_provider.current_version()).is_equal(1)
	assert_str(_provider.config_label()).is_equal("config@v1")
	assert_bool(_provider.has_config()).is_true()
	var hero: Dictionary = _provider.get_hero("hero_sample")
	assert_int(int(hero["rarity"])).is_equal(3)
	assert_int(int(hero["base_stats"]["hp"])).is_equal(100)
	# get_entry theo type cũng trả đúng.
	assert_bool(_provider.has_entry(&"hero", "hero_sample")).is_true()
	assert_int(_provider.get_all(&"hero").size()).is_equal(1)


func test_reads_return_copies_not_cache_reference() -> void:
	_provider = _make_provider()
	_provider.apply_bundle(_bundle(1, 3, 100))
	var hero: Dictionary = _provider.get_hero("hero_sample")
	hero["rarity"] = 999  # sửa bản sao
	assert_int(int(_provider.get_hero("hero_sample")["rarity"])).is_equal(3)  # cache không đổi


func test_version_bump_switches_active_and_emits_config_updated() -> void:
	_provider = _make_provider()
	_provider.apply_bundle(_bundle(1, 3, 100))
	_provider.apply_bundle(_bundle(2, 5, 250))
	assert_int(_provider.current_version()).is_equal(2)
	# Query CÙNG id trả data của v2 (không rebuild client).
	var hero: Dictionary = _provider.get_hero("hero_sample")
	assert_int(int(hero["rarity"])).is_equal(5)
	assert_int(int(hero["base_stats"]["hp"])).is_equal(250)
	# config_updated phát cho cả v1 (0→1) và v2 (1→2).
	assert_int(_config_updated.size()).is_equal(2)
	assert_int(int(_config_updated[1]["version"])).is_equal(2)
	assert_str(_config_updated[1]["config_version"]).is_equal("config@v2")


func test_reapplying_same_version_is_noop() -> void:
	_provider = _make_provider()
	_provider.apply_bundle(_bundle(1, 3, 100))
	assert_int(_config_updated.size()).is_equal(1)
	# Áp lại đúng version 1 (dữ liệu khác) = no-op: không phát thêm event, in-memory giữ nguyên.
	assert_bool(_provider.apply_bundle(_bundle(1, 4, 999))).is_true()
	assert_int(_config_updated.size()).is_equal(1)
	assert_int(int(_provider.get_hero("hero_sample")["rarity"])).is_equal(3)


func test_disk_cache_persists_and_reloads_on_boot() -> void:
	# Provider A ghi cache v1 xuống đĩa.
	var provider_a: Node = _make_provider()
	provider_a.apply_bundle(_bundle(1, 3, 100))
	# Provider B (mới) cùng cache_dir → boot _ready nạp lại từ đĩa (offline-view).
	var provider_b: Node = _make_provider()
	assert_bool(provider_b.has_config()).is_true()
	assert_int(provider_b.current_version()).is_equal(1)
	assert_int(int(provider_b.get_hero("hero_sample")["rarity"])).is_equal(3)


func test_old_version_file_is_immutable_not_overwritten() -> void:
	_provider = _make_provider()
	_provider.apply_bundle(_bundle(1, 3, 100))  # config@v1 = rarity 3
	_provider.apply_bundle(_bundle(2, 5, 250))  # config@v2 = rarity 5
	# Áp lại v1 với dữ liệu KHÁC (rarity 4): file config@v1 đã tồn tại ⇒ ghi-một-lần, không ghi đè.
	_provider.apply_bundle(_bundle(1, 4, 111))
	var path := _cache_dir.path_join("config@v1.json")
	var file := FileAccess.open(path, FileAccess.READ)
	assert_object(file).is_not_null()
	var json := JSON.new()
	assert_int(json.parse(file.get_as_text())).is_equal(OK)
	file.close()
	# Nội dung đĩa v1 vẫn là rarity 3 gốc (bất biến).
	assert_int(int(json.data["data"]["hero"][0]["rarity"])).is_equal(3)


func test_missing_cache_yields_empty_state() -> void:
	# cache_dir trống (chưa ghi gì) → boot không có config, không crash.
	_provider = _make_provider()
	assert_bool(_provider.has_config()).is_false()
	assert_int(_provider.current_version()).is_equal(0)
	assert_dict(_provider.get_hero("hero_sample")).is_empty()


func test_corrupt_cache_is_handled_gracefully() -> void:
	# Con trỏ hợp lệ nhưng file bundle hỏng ⇒ boot bỏ qua, không crash.
	DirAccess.make_dir_recursive_absolute(_cache_dir)
	var pointer := FileAccess.open(_cache_dir.path_join("active.json"), FileAccess.WRITE)
	pointer.store_string(JSON.stringify({"active_version": 1}))
	pointer.close()
	var bundle := FileAccess.open(_cache_dir.path_join("config@v1.json"), FileAccess.WRITE)
	bundle.store_string("khong-phai-json{")
	bundle.close()
	_provider = _make_provider()
	assert_bool(_provider.has_config()).is_false()


func test_invalid_bundle_is_rejected() -> void:
	_provider = _make_provider()
	# Thiếu config_version hợp lệ ⇒ từ chối, không crash, không phát event.
	assert_bool(_provider.apply_bundle({"data": {}})).is_false()
	assert_bool(_provider.apply_bundle({"config_version": "v1", "data": {}})).is_false()
	assert_int(_config_updated.size()).is_equal(0)


func test_check_for_update_pulls_new_version_from_server() -> void:
	_provider = _make_provider()
	_provider.apply_bundle(_bundle(1, 3, 100))
	# NetworkClient thật + transport giả: lần 1 trả version metadata (v2), lần 2 trả bundle v2.
	var net: Node = _NETWORK_CLIENT.new()
	add_child(net)
	auto_free(net)
	var fake := FakeHttpTransport.new()
	fake.queue_ok(200, JSON.stringify({"version": {"bundle": 2, "schema": 1}}))
	fake.queue_ok(200, JSON.stringify(_bundle(2, 5, 250)))
	net.set_transport(fake)
	_provider.network_client = net
	var updated: bool = await _provider.check_for_update()
	assert_bool(updated).is_true()
	assert_int(_provider.current_version()).is_equal(2)
	assert_int(int(_provider.get_hero("hero_sample")["rarity"])).is_equal(5)


func test_configprovider_autoload_present() -> void:
	assert_object(get_node_or_null(^"/root/ConfigProvider")).is_not_null()
