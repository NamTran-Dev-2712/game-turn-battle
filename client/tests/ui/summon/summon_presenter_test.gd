# Test SummonPresenter — Phase 33 (Summon/Gacha). Client gửi INTENT (bannerId/count/requestId) tới server và
# HIỂN THỊ kết quả server; KHÔNG tự random / KHÔNG tự quyết reward (ADR-011). Sau summon: refresh ví + kho từ
# server vào StateCache (client chỉ đọc). Fallback KHÔNG im lặng khi lỗi. Stub ở ranh giới net/state/config/router.
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/summon/summon_presenter.gd")


# View gián điệp — bắt dữ liệu render gần nhất.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


class _StubRouter extends Node:
	var back_calls: int = 0

	func back() -> bool:
		back_calls += 1
		return true


# ConfigProvider giả: get_all("gacha") trả danh sách banner.
class _StubConfig extends Node:
	var banners: Array = []

	func get_all(_type: StringName) -> Array:
		return banners.duplicate(true)


# NetworkClient giả: post_json trả kết quả summon đã xếp + bắt body; get_json trả DTO theo path (refresh).
class _StubNet extends Node:
	var post_result: Variant = null
	var post_calls: int = 0
	var last_body: Dictionary = {}
	var wallet_result: Variant = null
	var inventory_result: Variant = null

	func post_json(_path: String, body: Dictionary, _parser := Callable()) -> Variant:
		post_calls += 1
		last_body = body.duplicate(true)
		return post_result

	func get_json(path: String, _parser := Callable()) -> Variant:
		if path == "/wallet":
			return wallet_result
		if path == "/inventory":
			return inventory_result
		return null


# StateCache giả — apply_wallet/apply_inventory đếm số lần (chứng minh refresh authoritative, không tự cộng).
class _StubStateCache extends Node:
	var wallet_calls: int = 0
	var inventory_calls: int = 0
	var offline: bool = false

	func apply_wallet(_balances: Dictionary) -> void:
		wallet_calls += 1

	func apply_inventory(_items: Array, _heroes: Array) -> void:
		inventory_calls += 1

	func is_offline() -> bool:
		return offline


func _node(n: Node) -> Node:
	add_child(n)
	auto_free(n)
	return n


func _result(banner: String, count: int, pity: int, pulls: Array) -> SummonResultDto:
	var dto := SummonResultDto.new()
	dto.banner_id = banner
	dto.count = count
	dto.pity_after = pity
	var list: Array[SummonPullDto] = []
	for tuple in pulls:
		var p := SummonPullDto.new()
		p.hero_id = str(tuple[0])
		p.rarity = int(tuple[1])
		p.is_new = bool(tuple[2])
		p.fragments = int(tuple[3])
		list.append(p)
	dto.pulls = list
	return dto


func _ok(value) -> NetResult:
	return NetResult.success(value, 200)


func _config() -> _StubConfig:
	var config := _StubConfig.new()
	config.banners = [
		{"id": "gacha_standard", "cost": {"currency": "ticket", "amount": 1}},
		{"id": "gacha_special", "cost": {"currency": "gem", "amount": 100}},
	]
	return config


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_loads_banners_and_selects_first_by_default() -> void:
	var net := _StubNet.new(); _node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)

	var banners: Array = view.last.get("banners", [])
	assert_int(banners.size()).is_equal(2)
	assert_str(str(view.last.get("selected_banner"))).is_equal("gacha_standard")
	assert_bool(bool(view.last.get("can_summon"))).is_true()
	presenter.dispose()


func test_summon_one_sends_intent_and_renders_server_result() -> void:
	var net := _StubNet.new()
	net.post_result = _ok(_result("gacha_standard", 1, 1, [["hero_ignis", 5, true, 0]]))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(SummonView.INTENT_SUMMON_ONE)

	# Client CHỈ gửi intent: body đúng gồm bannerId/count/requestId — KHÔNG chứa quyết định kết quả.
	assert_int(net.post_calls).is_equal(1)
	assert_str(str(net.last_body.get("bannerId"))).is_equal("gacha_standard")
	assert_int(int(net.last_body.get("count"))).is_equal(1)
	assert_bool(net.last_body.has("requestId")).is_true()
	assert_int(net.last_body.size()).is_equal(3)  # chỉ 3 khoá — không rò dữ liệu quyết định

	# Kết quả HIỂN THỊ lấy từ server (một pull, hero mới).
	var results: Array = view.last.get("results", [])
	assert_int(results.size()).is_equal(1)
	assert_str(str(results[0])).contains("MỚI")
	assert_int(int(view.last.get("pity_after"))).is_equal(1)
	presenter.dispose()


func test_summon_ten_sends_count_ten() -> void:
	var net := _StubNet.new()
	var pulls: Array = []
	for i in range(10):
		pulls.append(["hero_ignis", 5, i == 0, 0 if i == 0 else 50])
	net.post_result = _ok(_result("gacha_standard", 10, 0, pulls))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(SummonView.INTENT_SUMMON_TEN)

	assert_int(int(net.last_body.get("count"))).is_equal(10)
	assert_int((view.last.get("results") as Array).size()).is_equal(10)
	presenter.dispose()


func test_successful_summon_refreshes_wallet_and_inventory_from_server() -> void:
	var net := _StubNet.new()
	net.post_result = _ok(_result("gacha_standard", 1, 1, [["hero_ignis", 5, false, 50]]))
	var wallet := WalletDto.new(); wallet.balances = [] as Array[CurrencyBalanceDto]
	net.wallet_result = _ok(wallet)
	var inv := InventoryDto.new(); inv.items = [] as Array[ItemStackDto]; inv.owned_heroes = [] as Array[OwnedHeroDto]
	net.inventory_result = _ok(inv)
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(SummonView.INTENT_SUMMON_ONE)

	# Client KHÔNG tự cộng: refresh ví + kho AUTHORITATIVE từ server sau summon.
	assert_int(state.wallet_calls).is_equal(1)
	assert_int(state.inventory_calls).is_equal(1)
	presenter.dispose()


func test_summon_failure_shows_error_and_fabricates_nothing() -> void:
	var net := _StubNet.new()
	var err := ErrorResponse.new(); err.code = "CURRENCY_INSUFFICIENT_FUNDS"; err.message = "x"
	net.post_result = NetResult.failure(NetResult.Kind.HTTP_4XX, err, 409)
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(SummonView.INTENT_SUMMON_ONE)

	assert_str(str(view.last.get("error_text"))).contains("CURRENCY_INSUFFICIENT_FUNDS")
	assert_int((view.last.get("results") as Array).size()).is_equal(0)  # không bịa kết quả
	assert_int(state.wallet_calls).is_equal(0)  # thất bại ⇒ không refresh (không có gì thay đổi)
	presenter.dispose()


func test_select_banner_changes_selection_and_clears_results() -> void:
	var net := _StubNet.new()
	net.post_result = _ok(_result("gacha_standard", 1, 0, [["hero_ignis", 5, true, 0]]))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(SummonView.INTENT_SUMMON_ONE)
	view.emit_intent(SummonView.INTENT_SELECT, {"banner_id": "gacha_special"})

	assert_str(str(view.last.get("selected_banner"))).is_equal("gacha_special")
	assert_int((view.last.get("results") as Array).size()).is_equal(0)
	presenter.dispose()


func test_back_intent_navigates_back() -> void:
	var net := _StubNet.new(); _node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(SummonView.INTENT_BACK)

	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()
