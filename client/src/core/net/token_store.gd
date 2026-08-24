# TokenStore — kho JWT của client: giữ access/refresh token + hạn dùng, persist AN TOÀN xuống đĩa.
# NetworkClient gắn `Authorization: Bearer <access>` khi `has_token()`. Lớp auth (AuthProfileFlow, phase 20)
# gọi `save_tokens()` sau guest login và `clear()` khi 401/logout.
# BẢO MẬT (phase 20): token KHÔNG lưu plaintext — ghi qua `FileAccess.open_encrypted_with_pass`
#   (khoá suy ra từ salt ứng dụng + `OS.get_unique_id()`, ràng theo thiết bị). Đây KHÔNG phải keychain OS
#   (Godot thuần không có, cần native plugin — ngoài phạm vi); là mức bảo vệ tối đa trong engine.
# TUYỆT ĐỐI không log token/passphrase. Không hardcode token. Không commit token (đĩa user:// bị .gitignore).
# Chi tiết: docs/godot/state-and-signals.md §4, docs/adr/ADR-007-save-strategy.md, ADR-008-networking.md.
class_name TokenStore
extends RefCounted

## Đường dẫn file token đã mã hoá (ghi đè được cho test → temp dir).
const DEFAULT_STORE_PATH: String = "user://auth/token.dat"
# Salt tĩnh của ứng dụng (obfuscation, không phải bí mật server). Khoá thật = salt + device id.
const _APP_SALT: String = "game-team.auth.v1"

## Đường dẫn lưu (đổi được cho test — inject temp path trước khi save/load).
var store_path: String = DEFAULT_STORE_PATH

var _access_token: String = ""
var _refresh_token: String = ""
# Unix epoch (giây) khi access token hết hạn. 0 = không rõ hạn (coi như không hết hạn theo giờ).
var _expires_at_unix: int = 0


## Lưu bộ token sau guest login (phase 20). Tính hạn từ `expires_in_seconds` (giờ server-time xấp xỉ
## bằng đồng hồ hệ thống — chỉ dùng để chủ động re-login trước khi gọi; 401 vẫn là chốt cuối). Persist mã hoá.
func save_tokens(access_token: String, refresh_token: String, expires_in_seconds: int) -> void:
	_access_token = access_token
	_refresh_token = refresh_token
	_expires_at_unix = 0 if expires_in_seconds <= 0 else int(Time.get_unix_time_from_system()) + expires_in_seconds
	_persist()


## (Tương thích) Chỉ đặt access token trong bộ nhớ, KHÔNG persist/hạn. Ưu tiên `save_tokens`.
func set_token(access_token: String) -> void:
	_access_token = access_token


## Nạp token đã lưu từ đĩa (gọi khi NetworkClient._ready). Thiếu/hỏng ⇒ để rỗng, KHÔNG crash.
func load() -> void:
	if not FileAccess.file_exists(store_path):
		return
	var file := FileAccess.open_encrypted_with_pass(store_path, FileAccess.READ, _passphrase())
	if file == null:
		# Sai khoá (đổi thiết bị) / file hỏng → coi như chưa có token (sẽ login lại).
		return
	var text := file.get_as_text()
	file.close()
	var json := JSON.new()
	if json.parse(text) != OK or not (json.data is Dictionary):
		return
	var data: Dictionary = json.data
	_access_token = str(data.get("access", ""))
	_refresh_token = str(data.get("refresh", ""))
	_expires_at_unix = int(data.get("expires_at", 0))


## Xoá token (logout / 401 / token không hợp lệ): rỗng bộ nhớ + xoá file đĩa.
func clear() -> void:
	_access_token = ""
	_refresh_token = ""
	_expires_at_unix = 0
	if FileAccess.file_exists(store_path):
		DirAccess.remove_absolute(store_path)


## Access token hiện tại ("" nếu chưa có — khi đó NetworkClient bỏ qua header Authorization).
func get_access_token() -> String:
	return _access_token


## Refresh token đã lưu ("" nếu chưa có). Đổi refresh → access = phase sau (chưa có endpoint refresh).
func get_refresh_token() -> String:
	return _refresh_token


## True nếu đang có access token.
func has_token() -> bool:
	return _access_token != ""


## True nếu access token đã quá hạn theo đồng hồ hệ thống (0 = không rõ hạn ⇒ coi như còn hạn).
func is_expired() -> bool:
	return _expires_at_unix > 0 and int(Time.get_unix_time_from_system()) >= _expires_at_unix


# ── Nội bộ ────────────────────────────────────────────────────────────────────────────────────────

# Ghi token mã hoá xuống đĩa. Chỉ gọi khi có thay đổi token. KHÔNG log nội dung.
func _persist() -> void:
	_ensure_dir()
	var file := FileAccess.open_encrypted_with_pass(store_path, FileAccess.WRITE, _passphrase())
	if file == null:
		push_warning("TokenStore: không ghi được token (bỏ qua persist).")
		return
	file.store_string(JSON.stringify({
		"access": _access_token,
		"refresh": _refresh_token,
		"expires_at": _expires_at_unix,
	}))
	file.close()


func _ensure_dir() -> void:
	var dir := store_path.get_base_dir()
	if dir != "" and not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)


# Passphrase ràng theo thiết bị: salt ứng dụng + device id (fallback salt khi id rỗng). Không log.
func _passphrase() -> String:
	var device_id := OS.get_unique_id()
	return _APP_SALT + ":" + (device_id if device_id != "" else _APP_SALT)
