# Test StateCache — Phase 16.
# Chứng minh: apply_snapshot từ server → đọc đúng currency/hero/progress + phát `state_refreshed`
# + nguồn = "server"; đọc trả BẢN SAO (caller không sửa được cache); KHÔNG có API mutation chân lý
# (không setter cộng/trừ currency…); cache đĩa → boot nạp lại với nhãn "cache"/offline; autoload có mặt.
# Tất định — không network/animation/wall-clock (docs/testing/godot-testing.md).
extends GdUnitTestSuite

const _STATE_CACHE := preload("res://src/core/state/state_cache.gd")

var _cache: Node
var _cache_dir: String
var _refreshed: Array = []


func before_test() -> void:
	_refreshed = []
	EventBus.subscribe(&"state_refreshed", _on_refreshed)
	_cache_dir = "user://test_state_%d" % Time.get_ticks_usec()


func after_test() -> void:
	EventBus.unsubscribe(&"state_refreshed", _on_refreshed)
	_remove_dir_recursive(_cache_dir)


func _on_refreshed(payload) -> void:
	_refreshed.append(payload)


func _make_cache() -> Node:
	var cache: Node = _STATE_CACHE.new()
	cache.cache_dir = _cache_dir
	add_child(cache)
	auto_free(cache)
	return cache


func _snapshot() -> Dictionary:
	return {
		"profile": {"playerId": "p1", "displayName": "Nam", "level": 7},
		"currencies": {"gold": 1500, "gem": 42},
		"heroes": [
			{"id": "hero_sample", "level": 10},
			{"id": "hero_two", "level": 3},
		],
		"progress": {"stage": "stage_1_3", "chapter": 1},
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

func test_apply_snapshot_reads_state_and_emits_refresh() -> void:
	_cache = _make_cache()
	_cache.apply_snapshot(_snapshot())
	assert_int(_cache.get_currency("gold")).is_equal(1500)
	assert_int(_cache.get_currency("gem")).is_equal(42)
	assert_int(_cache.get_currency("unknown")).is_equal(0)
	assert_int(_cache.get_heroes().size()).is_equal(2)
	assert_int(int(_cache.get_hero("hero_sample")["level"])).is_equal(10)
	assert_str(str(_cache.get_progress("stage"))).is_equal("stage_1_3")
	assert_str(str(_cache.get_profile()["displayName"])).is_equal("Nam")
	# Nguồn = server + phát state_refreshed đúng một lần.
	assert_str(_cache.source()).is_equal("server")
	assert_bool(_cache.is_offline()).is_false()
	assert_int(_refreshed.size()).is_equal(1)
	assert_str(_refreshed[0]["source"]).is_equal("server")


func test_reads_return_copies_not_cache_reference() -> void:
	_cache = _make_cache()
	_cache.apply_snapshot(_snapshot())
	var heroes: Array = _cache.get_heroes()
	heroes.append({"id": "hacked"})           # sửa bản sao mảng
	heroes[0]["level"] = 999                   # sửa bản sao phần tử
	var currencies: Dictionary = _cache.get_currencies()
	currencies["gold"] = 0
	# Cache gốc không đổi.
	assert_int(_cache.get_heroes().size()).is_equal(2)
	assert_int(int(_cache.get_hero("hero_sample")["level"])).is_equal(10)
	assert_int(_cache.get_currency("gold")).is_equal(1500)


func test_no_authoritative_mutation_api_exists() -> void:
	# Guard runtime: StateCache CHỈ đọc + apply_snapshot (từ server). KHÔNG có đường ghi chân lý.
	_cache = _make_cache()
	assert_bool(_cache.is_display_only()).is_true()
	for forbidden in ["add_currency", "spend_currency", "set_currency", "add_hero",
			"set_progress", "grant_reward", "apply_reward", "mutate"]:
		assert_bool(_cache.has_method(forbidden)) \
			.override_failure_message("StateCache KHÔNG được có mutator chân lý '%s'." % forbidden) \
			.is_false()


func test_disk_cache_reloads_offline_with_cache_label() -> void:
	# Cache A refresh từ server → lưu đĩa.
	var cache_a: Node = _make_cache()
	cache_a.apply_snapshot(_snapshot())
	# Cache B (mới) cùng cache_dir → boot nạp lại với nhãn "cache" (offline/cũ), ưu tiên server khi online.
	var cache_b: Node = _make_cache()
	assert_str(cache_b.source()).is_equal("cache")
	assert_bool(cache_b.is_offline()).is_true()
	assert_int(cache_b.get_currency("gold")).is_equal(1500)
	assert_int(int(cache_b.get_hero("hero_two")["level"])).is_equal(3)
	# Refresh server sau đó lật nhãn về "server".
	cache_b.apply_snapshot(_snapshot())
	assert_str(cache_b.source()).is_equal("server")


func test_empty_when_no_cache() -> void:
	_cache = _make_cache()
	assert_str(_cache.source()).is_equal("empty")
	assert_int(_cache.get_currency("gold")).is_equal(0)
	assert_int(_cache.get_heroes().size()).is_equal(0)


func test_statecache_autoload_present() -> void:
	assert_object(get_node_or_null(^"/root/StateCache")).is_not_null()
