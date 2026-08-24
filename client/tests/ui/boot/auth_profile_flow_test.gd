# Test AuthProfileFlow — Phase 20 (server mock qua FakeHttpTransport, tất định).
# Nhánh acceptance: happy-path (login→token→profile→StateCache); token còn hạn → KHÔNG login lại;
# 401 → re-login có giới hạn (không vòng lặp vô hạn); token hết hạn → re-login chủ động; mất mạng → cache.
# Client KHÔNG tự bịa dữ liệu — mất mạng không cache ⇒ thất bại rõ ràng. Token = giá trị giả "fake-*".
# (docs/testing/godot-testing.md, docs/godot/state-and-signals.md §4)
extends GdUnitTestSuite

const _NETWORK_CLIENT := preload("res://src/core/net/network_client.gd")
const _STATE_CACHE := preload("res://src/core/state/state_cache.gd")
const _TOKEN_STORE := preload("res://src/core/net/token_store.gd")
const _AUTH_FLOW := preload("res://src/ui/boot/auth_profile_flow.gd")

const _GUEST_JSON := '{"accessToken":"fake-jwt","refreshToken":"fake-refresh","expiresInSeconds":3600}'
const _PROFILE_JSON := '{"playerId":"acc-1","displayName":"Guest","level":1,"schemaVersion":1}'
const _ERROR_401 := '{"error":{"code":"UNAUTHENTICATED","message":"nope","traceId":null}}'

var _cache_dir: String
var _unauthorized: Array = []


func before_test() -> void:
	_cache_dir = "user://test_authflow_%d" % Time.get_ticks_usec()
	_unauthorized = []
	EventBus.subscribe(&"unauthorized", _on_unauthorized)


func after_test() -> void:
	EventBus.unsubscribe(&"unauthorized", _on_unauthorized)
	_remove_dir_recursive(_cache_dir)


func _on_unauthorized(payload) -> void:
	_unauthorized.append(payload)


# ── Helpers ──────────────────────────────────────────────────────────────────────────────────────

# NetworkClient thật (fake transport) + TokenStore tạm (không đụng token thật) + StateCache tạm.
func _build(fake: FakeHttpTransport, state_cache: Node = null) -> Dictionary:
	var token_store: TokenStore = _TOKEN_STORE.new()
	token_store.store_path = "user://test_authflow_token_%d.dat" % Time.get_ticks_usec()
	var net: Node = _NETWORK_CLIENT.new()
	net.token_store = token_store  # inject trước _ready ⇒ không tạo store mặc định / không load token thật
	add_child(net)
	auto_free(net)
	net.set_transport(fake)
	var sc: Node = state_cache
	if sc == null:
		sc = _STATE_CACHE.new()
		sc.cache_dir = _cache_dir
		add_child(sc)
		auto_free(sc)
	var flow = _AUTH_FLOW.new(net, sc)
	return {"net": net, "sc": sc, "flow": flow, "ts": token_store}


func _has_auth_header(headers: PackedStringArray) -> bool:
	for header in headers:
		if header.begins_with("Authorization: Bearer "):
			return true
	return false


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

func test_happy_path_login_token_profile_into_state_cache() -> void:
	var fake := FakeHttpTransport.new()
	fake.queue_ok(200, _GUEST_JSON)    # POST /auth/guest
	fake.queue_ok(200, _PROFILE_JSON)  # GET /profile
	var ctx := _build(fake)
	var result: Dictionary = await ctx["flow"].run()

	assert_bool(result["ok"]).is_true()
	assert_bool(result["offline"]).is_false()
	# Token đã lưu.
	assert_bool(ctx["ts"].has_token()).is_true()
	assert_str(ctx["ts"].get_access_token()).is_equal("fake-jwt")
	# StateCache chứa đúng profile server, nguồn = server.
	var profile: Dictionary = ctx["sc"].get_profile()
	assert_str(str(profile.get("displayName"))).is_equal("Guest")
	assert_int(int(profile.get("level"))).is_equal(1)
	assert_str(ctx["sc"].source()).is_equal("server")
	# Thứ tự request: auth/guest rồi profile; profile CÓ Authorization header.
	assert_int(fake.requests.size()).is_equal(2)
	assert_bool(str(fake.requests[0]["url"]).ends_with("/api/v1/auth/guest")).is_true()
	assert_bool(str(fake.requests[1]["url"]).ends_with("/api/v1/profile")).is_true()
	assert_bool(_has_auth_header(fake.requests[1]["headers"])).is_true()


func test_existing_valid_token_skips_guest_login() -> void:
	var fake := FakeHttpTransport.new()
	fake.queue_ok(200, _PROFILE_JSON)  # CHỈ profile được kỳ vọng
	var ctx := _build(fake)
	ctx["ts"].save_tokens("existing-jwt", "fake-refresh", 3600)  # còn hạn
	var result: Dictionary = await ctx["flow"].run()

	assert_bool(result["ok"]).is_true()
	# Đúng 1 request (profile) — KHÔNG gọi /auth/guest.
	assert_int(fake.requests.size()).is_equal(1)
	assert_bool(str(fake.requests[0]["url"]).ends_with("/api/v1/profile")).is_true()
	assert_bool(_has_auth_header(fake.requests[0]["headers"])).is_true()


func test_unauthorized_triggers_bounded_relogin_then_succeeds() -> void:
	var fake := FakeHttpTransport.new()
	fake.queue_ok(401, _ERROR_401)     # profile #1 → 401
	fake.queue_ok(200, _GUEST_JSON)    # re-login guest
	fake.queue_ok(200, _PROFILE_JSON)  # profile #2 → ok
	var ctx := _build(fake)
	ctx["ts"].save_tokens("stale-jwt", "fake-refresh", 3600)  # server sẽ từ chối
	var result: Dictionary = await ctx["flow"].run()

	assert_bool(result["ok"]).is_true()
	assert_bool(result["offline"]).is_false()
	assert_str(str(ctx["sc"].get_profile().get("displayName"))).is_equal("Guest")
	assert_str(ctx["ts"].get_access_token()).is_equal("fake-jwt")  # token mới thay token cũ
	assert_int(_unauthorized.size()).is_greater_equal(1)  # sự kiện unauthorized đã phát
	assert_int(fake.requests.size()).is_equal(3)  # profile401, guest, profile200 — có giới hạn


func test_relogin_is_bounded_no_infinite_loop_when_401_persists() -> void:
	var fake := FakeHttpTransport.new()
	fake.queue_ok(401, _ERROR_401)   # profile #1 → 401
	fake.queue_ok(200, _GUEST_JSON)  # re-login ok
	fake.queue_ok(401, _ERROR_401)   # profile #2 → 401 (lặp — server vẫn từ chối)
	var ctx := _build(fake)
	ctx["ts"].save_tokens("stale-jwt", "fake-refresh", 3600)
	var result: Dictionary = await ctx["flow"].run()

	assert_bool(result["ok"]).is_false()  # không cache ⇒ thất bại rõ ràng
	# MAX_RELOGIN=1 ⇒ dừng sau: profile401, guest, profile401 = 3 request (KHÔNG vòng lặp vô hạn).
	assert_int(fake.requests.size()).is_equal(3)


func test_expired_token_relogins_proactively() -> void:
	var fake := FakeHttpTransport.new()
	fake.queue_ok(200, _GUEST_JSON)    # guest login (vì token hết hạn)
	fake.queue_ok(200, _PROFILE_JSON)  # profile
	var ctx := _build(fake)
	ctx["ts"].save_tokens("old-jwt", "fake-refresh", 3600)
	ctx["ts"]._expires_at_unix = int(Time.get_unix_time_from_system()) - 10  # ép hết hạn
	var result: Dictionary = await ctx["flow"].run()

	assert_bool(result["ok"]).is_true()
	# Request đầu = guest (re-login chủ động), rồi profile.
	assert_int(fake.requests.size()).is_equal(2)
	assert_bool(str(fake.requests[0]["url"]).ends_with("/api/v1/auth/guest")).is_true()
	assert_str(ctx["ts"].get_access_token()).is_equal("fake-jwt")


func test_offline_with_cache_returns_offline_view_without_fabricating() -> void:
	# 1) Gieo cache đĩa qua một StateCache tạm (apply_snapshot persist).
	var seeder: Node = _STATE_CACHE.new()
	seeder.cache_dir = _cache_dir
	add_child(seeder)
	auto_free(seeder)
	seeder.apply_snapshot({"profile": {"displayName": "Cached Hero", "level": 7}})
	# 2) StateCache mới boot snapshot đó ⇒ nguồn = cache (offline-view).
	var sc: Node = _STATE_CACHE.new()
	sc.cache_dir = _cache_dir
	add_child(sc)
	auto_free(sc)
	assert_str(sc.source()).is_equal("cache")
	assert_bool(sc.is_offline()).is_true()
	# 3) Có token nhưng profile mất mạng ⇒ offline-fallback (giữ cache, KHÔNG bịa).
	var fake := FakeHttpTransport.new()
	fake.queue_transport(HTTPRequest.RESULT_CANT_CONNECT)  # profile fail
	var ctx := _build(fake, sc)
	ctx["ts"].save_tokens("fake-jwt", "fake-refresh", 3600)
	var result: Dictionary = await ctx["flow"].run()

	assert_bool(result["ok"]).is_true()
	assert_bool(result["offline"]).is_true()
	# Cache cũ được giữ nguyên, vẫn offline, không dữ liệu giả.
	assert_str(str(sc.get_profile().get("displayName"))).is_equal("Cached Hero")
	assert_bool(sc.is_offline()).is_true()
