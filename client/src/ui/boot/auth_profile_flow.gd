# AuthProfileFlow — điều phối auth + profile khi boot (phase 20). RefCounted (KHÔNG autoload).
# Vòng đời auth TẬP TRUNG ở đây (BootController gọi `run()`); NetworkClient chỉ gắn token + phát
# `unauthorized`, StateCache chỉ là read-cache. Luồng:
#   1) Đảm bảo token: có token còn hạn → dùng lại; nếu không → POST /auth/guest → lưu token an toàn.
#   2) GET /profile → StateCache.apply_snapshot (server-authoritative, ADR-007).
#      - 401 → clear token → re-login guest (giới hạn MAX_RELOGIN — CHỐNG vòng lặp) → thử lại.
#      - Mất mạng/lỗi → nếu có cache cũ ⇒ offline-view (nhãn offline); không thì báo lỗi. KHÔNG bịa dữ liệu.
# Quyết định re-login đọc `NetResult.kind` tại chỗ (điều khiển tất định); sự kiện `unauthorized` vẫn phát
# toàn cục từ NetworkClient cho hệ thống khác quan sát. Chi tiết: docs/godot/state-and-signals.md §4.
class_name AuthProfileFlow
extends RefCounted

## Endpoint guest login (POST, KHÔNG retry — tránh double-effect).
const AUTH_GUEST_PATH: String = "/api/v1/auth/guest"
## Endpoint profile (GET, cần Authorization; owner suy từ token `sub` — chống IDOR).
const PROFILE_PATH: String = "/api/v1/profile"
## Số lần re-login TỐI ĐA khi 401 (chống vòng lặp login→401→login…).
const MAX_RELOGIN: int = 1

## Kênh mạng (autoload NetworkClient mặc định; inject giả cho test).
var network_client: Node = null
## Read-cache trạng thái (autoload StateCache mặc định; inject cho test).
var state_cache: Node = null


func _init(p_network_client: Node = null, p_state_cache: Node = null) -> void:
	network_client = p_network_client if p_network_client != null else NetworkClient
	state_cache = p_state_cache if p_state_cache != null else StateCache


## Chạy auth + tải profile. Trả `{ ok: bool, offline: bool, code: String }`:
##   ok=true, offline=false  → profile server tươi đã vào StateCache.
##   ok=true, offline=true   → không lấy được server nhưng có cache cũ ⇒ hiển thị offline.
##   ok=false                → không có cả server lẫn cache ⇒ boot hiện màn lỗi.
func run() -> Dictionary:
	if network_client == null:
		return _degraded("NO_NETWORK_CLIENT")

	var token_store: TokenStore = network_client.token_store
	# 1) Đảm bảo có token còn hạn. Không có / hết hạn → guest login.
	if token_store == null or not token_store.has_token() or token_store.is_expired():
		var login := await _guest_login()
		if not login["ok"]:
			return _degraded(login["code"])

	# 2) Tải profile, re-login có giới hạn khi 401.
	var relogins := 0
	while true:
		var res: NetResult = await network_client.get_json(PROFILE_PATH, NetworkResponseParser.parse_profile)
		if res.ok:
			_apply_profile(res.value)
			return {"ok": true, "offline": false, "code": ""}
		if res.kind == NetResult.Kind.UNAUTHORIZED and relogins < MAX_RELOGIN:
			relogins += 1
			if token_store != null:
				token_store.clear()
			var relogin := await _guest_login()
			if not relogin["ok"]:
				return _degraded(relogin["code"])
			continue
		# Lỗi không phục hồi được (401 hết lượt / mất mạng / server lỗi) → offline-view nếu có cache.
		return _degraded(_code_of(res))
	# Không tới được (vòng lặp chỉ thoát bằng return) — giữ để trình biên dịch thấy mọi nhánh có trả về.
	return _degraded("UNREACHABLE")


# ── Nội bộ ────────────────────────────────────────────────────────────────────────────────────────

# Guest login: POST /auth/guest → lưu token an toàn. Body deviceId để trống (tuỳ chọn).
func _guest_login() -> Dictionary:
	var res: NetResult = await network_client.post_json(
		AUTH_GUEST_PATH, {}, NetworkResponseParser.parse_auth_guest_response)
	if not res.ok or res.value == null:
		return {"ok": false, "code": _code_of(res)}
	var auth: AuthGuestResponse = res.value
	var token_store: TokenStore = network_client.token_store
	if token_store != null:
		token_store.save_tokens(auth.access_token, auth.refresh_token, auth.expires_in_seconds)
	return {"ok": true, "code": ""}


# Nạp profile server vào StateCache (đường ghi DUY NHẤT = apply_snapshot; wire key camelCase như server).
func _apply_profile(profile: ProfileDto) -> void:
	if state_cache == null:
		return
	state_cache.apply_snapshot({
		"profile": {
			"playerId": profile.player_id,
			"displayName": profile.display_name,
			"level": profile.level,
			"schemaVersion": profile.schema_version,
		},
	})


# Suy giảm có kiểm soát: có cache cũ ⇒ offline-view (không bịa); không có ⇒ thất bại (boot màn lỗi).
func _degraded(code: String) -> Dictionary:
	if _has_cached_profile():
		return {"ok": true, "offline": true, "code": code}
	return {"ok": false, "offline": false, "code": code}


func _has_cached_profile() -> bool:
	return state_cache != null and not state_cache.get_profile().is_empty()


func _code_of(res: NetResult) -> String:
	return res.error.code if res != null and res.error != null else ""
