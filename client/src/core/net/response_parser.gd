# NetworkResponseParser — map JSON (Dictionary) → model generated (phase 08).
# Model generated là DO-NOT-EDIT và chỉ chứa dữ liệu (không có `from_dict`) — các model tự ghi
# "Parse JSON: Phase 15". Lớp này là chỗ chứa logic parse đó, KHÔNG sửa file generated.
# Wire là camelCase, field GDScript là snake_case (utcNow↔utc_now, traceId↔trace_id).
# Thiếu key bắt buộc / sai kiểu ⇒ trả `null` (NetworkClient dịch thành PARSE_ERROR — không bịa kết quả).
# Thêm DTO mới ở các phase sau: thêm một hàm parse tĩnh vào đây (một hàm/DTO tiêu thụ).
class_name NetworkResponseParser
extends RefCounted


## Parse `GET /api/v1/server-time` → ServerTimeResponse. `null` nếu thiếu `utcNow`.
static func parse_server_time(data: Dictionary) -> ServerTimeResponse:
	if not data.has("utcNow"):
		return null
	var model := ServerTimeResponse.new()
	model.utc_now = str(data["utcNow"])
	return model


## Parse body lỗi `{ "error": { code, message, traceId } }` → ErrorResponse (đối tượng lỗi bên trong).
## `null` nếu không đúng hình dạng envelope (thiếu `error`/`code`/`message`).
static func parse_error_envelope(data: Dictionary) -> ErrorResponse:
	if not data.has("error") or not (data["error"] is Dictionary):
		return null
	var inner: Dictionary = data["error"]
	if not inner.has("code") or not inner.has("message"):
		return null
	var model := ErrorResponse.new()
	model.code = str(inner["code"])
	model.message = str(inner["message"])
	# traceId nullable: giữ null nếu vắng/null, ngược lại ép chuỗi.
	model.trace_id = null if inner.get("traceId", null) == null else str(inner["traceId"])
	return model


## Parse metadata bundle `{ "version": { "bundle": int, "schema": int } }` → ConfigBundleDto.
## Dùng cho ConfigProvider so version (phase 16). `null` nếu thiếu `version`/`bundle`.
static func parse_config_bundle(data: Dictionary) -> ConfigBundleDto:
	if not data.has("version") or not (data["version"] is Dictionary):
		return null
	var inner: Dictionary = data["version"]
	if not inner.has("bundle"):
		return null
	var version := ConfigVersion.new()
	version.bundle = int(inner["bundle"])
	version.schema = int(inner.get("schema", 0))
	var model := ConfigBundleDto.new()
	model.version = version
	return model
