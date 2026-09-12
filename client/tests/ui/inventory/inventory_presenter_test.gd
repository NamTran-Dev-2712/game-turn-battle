# Test InventoryPresenter — Phase 32 (Inventory). Tải kho từ server (server-authoritative) → cache → hiển thị;
# lọc theo tab; fallback KHÔNG im lặng khi lỗi (giữ cache + nhãn lỗi). Client KHÔNG tự tính/thay số lượng (ADR-007).
# Stub ở ranh giới hợp lý (net/state/config/router); giữ behavior thật của presenter. (docs/testing/godot-testing.md)
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/inventory/inventory_presenter.gd")


# View gián điệp — bắt dữ liệu render gần nhất.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


# Router giả: đếm back().
class _StubRouter extends Node:
	var back_calls: int = 0

	func back() -> bool:
		back_calls += 1
		return true


# ConfigProvider giả: {type: {id: entry}} cho tên hiển thị item.
class _StubConfig extends Node:
	var data: Dictionary = {}

	func get_entry(type: StringName, id: String) -> Dictionary:
		var by_id: Dictionary = data.get(String(type), {})
		return (by_id.get(id, {}) as Dictionary).duplicate(true)


# NetworkClient giả: trả NetResult đã xếp cho get(/inventory); đếm số lần gọi.
class _StubNet extends Node:
	var result: Variant = null
	var get_calls: int = 0

	func get_json(_path: String, _parser := Callable()) -> Variant:
		get_calls += 1
		return result


# StateCache giả — apply_inventory lưu + phát state_refreshed (mô phỏng StateCache thật). Đọc trả bản đã lưu.
class _StubStateCache extends Node:
	var inventory: Dictionary = {}
	var apply_calls: int = 0
	var offline: bool = false

	func apply_inventory(items: Array, owned_heroes: Array) -> void:
		apply_calls += 1
		inventory = {"items": items.duplicate(true), "owned_heroes": owned_heroes.duplicate(true)}
		EventBus.emit(&"state_refreshed", {"source": "server"})

	func get_inventory() -> Dictionary:
		return inventory.duplicate(true)

	func is_offline() -> bool:
		return offline


func _node(n: Node) -> Node:
	add_child(n)
	auto_free(n)
	return n


func _inventory_dto(items: Array, heroes: Array) -> InventoryDto:
	var dto := InventoryDto.new()
	var item_list: Array[ItemStackDto] = []
	for triple in items:
		var s := ItemStackDto.new()
		s.item_type = str(triple[0])
		s.item_id = str(triple[1])
		s.quantity = int(triple[2])
		item_list.append(s)
	dto.items = item_list
	var hero_list: Array[OwnedHeroDto] = []
	for pair in heroes:
		var h := OwnedHeroDto.new()
		h.hero_id = str(pair[0])
		h.level = int(pair[1])
		h.stars = int(pair[2])
		hero_list.append(h)
	dto.owned_heroes = hero_list
	return dto


func _ok(value) -> NetResult:
	return NetResult.success(value, 200)


func _config() -> _StubConfig:
	var config := _StubConfig.new()
	config.data = {"item": {"item_potion": {"name": "Small Potion"}}}
	return config


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_fetch_populates_inventory_and_renders_rows() -> void:
	var net := _StubNet.new()
	net.result = _ok(_inventory_dto(
		[["item", "item_potion", 10], ["fragment", "hero_a", 5]],
		[["hero_a", 1, 1]]))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)

	assert_int(state.apply_calls).is_equal(1)  # kho tải từ server → cache (server-authoritative)
	var rows: Array = view.last.get("rows", [])
	assert_int(rows.size()).is_equal(3)  # 1 item + 1 fragment + 1 hero (tab "all")
	# Tên vật phẩm ghép từ config (data-driven) + số lượng server.
	assert_str(str(rows[0].get("label"))).is_equal("Small Potion")
	assert_int(int(rows[0].get("quantity"))).is_equal(10)
	presenter.dispose()


func test_filter_shows_only_selected_type() -> void:
	var net := _StubNet.new()
	net.result = _ok(_inventory_dto(
		[["item", "item_potion", 10], ["fragment", "hero_a", 5]],
		[["hero_a", 1, 1]]))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(InventoryView.INTENT_FILTER, {"filter": "item"})

	var rows: Array = view.last.get("rows", [])
	assert_int(rows.size()).is_equal(1)
	assert_str(str(rows[0].get("kind"))).is_equal("item")
	presenter.dispose()


func test_empty_inventory_shows_empty_state() -> void:
	var net := _StubNet.new()
	net.result = _ok(_inventory_dto([], []))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)

	assert_bool(bool(view.last.get("empty"))).is_true()
	assert_int((view.last.get("rows") as Array).size()).is_equal(0)
	presenter.dispose()


func test_fetch_failure_shows_error_and_keeps_cache() -> void:
	# Cache sẵn có (offline-view) + server lỗi ⇒ giữ cache + nhãn lỗi (Rule E — fallback KHÔNG im lặng).
	var net := _StubNet.new()
	var err := ErrorResponse.new(); err.code = "NETWORK_ERROR"; err.message = "x"
	net.result = NetResult.failure(NetResult.Kind.NETWORK_ERROR, err, 0)
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new()
	state.inventory = {"items": [{"item_type": "item", "item_id": "item_potion", "quantity": 7}], "owned_heroes": []}
	_node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)

	assert_int(state.apply_calls).is_equal(0)  # lỗi ⇒ KHÔNG áp dụng gì (không bịa)
	assert_str(str(view.last.get("error_text"))).contains("NETWORK_ERROR")
	assert_int((view.last.get("rows") as Array).size()).is_equal(1)  # vẫn hiện cache cũ
	presenter.dispose()


func test_retry_intent_refetches() -> void:
	var net := _StubNet.new()
	net.result = _ok(_inventory_dto([["item", "item_potion", 10]], []))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	var before := net.get_calls
	view.emit_intent(InventoryView.INTENT_RETRY)

	assert_int(net.get_calls).is_equal(before + 1)
	presenter.dispose()


func test_back_intent_navigates_back() -> void:
	var net := _StubNet.new()
	net.result = _ok(_inventory_dto([], []))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(InventoryView.INTENT_BACK)

	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()
