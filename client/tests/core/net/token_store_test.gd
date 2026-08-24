# Test TokenStore — Phase 20.
# Chứng minh: persist token AN TOÀN (mã hoá) round-trip qua đĩa; clear xoá file; is_expired đúng;
# file thiếu ⇒ rỗng (không crash). Dùng store_path tạm ⇒ không đụng token thật (user://auth/token.dat).
# TUYỆT ĐỐI không dùng token thật — chỉ giá trị giả "fake-*". (docs/testing/godot-testing.md)
extends GdUnitTestSuite

const _TOKEN_STORE := preload("res://src/core/net/token_store.gd")

var _store_path: String


func before_test() -> void:
	_store_path = "user://test_token_%d.dat" % Time.get_ticks_usec()


func after_test() -> void:
	if FileAccess.file_exists(_store_path):
		DirAccess.remove_absolute(_store_path)


func _make() -> TokenStore:
	var store: TokenStore = _TOKEN_STORE.new()
	store.store_path = _store_path
	return store


func test_save_then_load_round_trip() -> void:
	var store := _make()
	store.save_tokens("fake-access", "fake-refresh", 3600)
	assert_bool(store.has_token()).is_true()
	assert_str(store.get_access_token()).is_equal("fake-access")
	# Instance mới (giả mở lại app) nạp lại từ đĩa.
	var reopened := _make()
	assert_bool(reopened.has_token()).is_false()  # chưa load
	reopened.load()
	assert_bool(reopened.has_token()).is_true()
	assert_str(reopened.get_access_token()).is_equal("fake-access")
	assert_str(reopened.get_refresh_token()).is_equal("fake-refresh")
	assert_bool(reopened.is_expired()).is_false()


func test_clear_wipes_memory_and_deletes_file() -> void:
	var store := _make()
	store.save_tokens("fake-access", "fake-refresh", 3600)
	assert_bool(FileAccess.file_exists(_store_path)).is_true()
	store.clear()
	assert_bool(store.has_token()).is_false()
	assert_str(store.get_access_token()).is_equal("")
	assert_bool(FileAccess.file_exists(_store_path)).is_false()


func test_is_expired_true_when_past_deadline() -> void:
	var store := _make()
	store.save_tokens("fake-access", "fake-refresh", 3600)
	assert_bool(store.is_expired()).is_false()
	# Ép hạn về quá khứ (mô phỏng token hết hạn).
	store._expires_at_unix = int(Time.get_unix_time_from_system()) - 10
	assert_bool(store.is_expired()).is_true()


func test_missing_file_loads_empty_without_crash() -> void:
	var store := _make()
	assert_bool(FileAccess.file_exists(_store_path)).is_false()
	store.load()  # không có file → rỗng, không crash
	assert_bool(store.has_token()).is_false()
