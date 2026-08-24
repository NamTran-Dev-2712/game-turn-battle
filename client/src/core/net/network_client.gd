# NetworkClient — autoload: KÊNH GIAO TIẾP SERVER DUY NHẤT của client (ADR-008, ADR-002).
# Bọc HTTPRequest (qua HttpTransport), gọi REST /api/v1 (JSON), gắn JWT, parse response vào model
# generated (phase 08), chuẩn hoá ErrorResponse thành NetResult, phát sự kiện lỗi qua EventBus,
# timeout + retry AN TOÀN (chỉ GET/idempotent). UI/feature KHÔNG gọi HTTPRequest trực tiếp —
# luôn đi qua đây. Client KHÔNG tự quyết kết quả/phần thưởng: mất mạng → báo lỗi, không bịa (ADR-011).
# Autoload BỎ `class_name` (trùng tên singleton) — truy cập qua global `NetworkClient`.
# Chi tiết: docs/godot/state-and-signals.md §4, docs/adr/ADR-008-networking.md.
extends Node

# Base URL mặc định (dev local, phase 04/13). Ghi đè bằng biến môi trường GAME_TEAM_API_BASE_URL.
const DEFAULT_BASE_URL: String = "http://localhost:8080"
# Biến môi trường ghi đè base URL (CI/nhiều môi trường). Phase 16 ConfigProvider có thể set base_url.
const ENV_BASE_URL: StringName = &"GAME_TEAM_API_BASE_URL"
# Thời gian chờ mặc định mỗi request (giây).
const DEFAULT_REQUEST_TIMEOUT_SECONDS: float = 10.0
# Số lần thử lại TỐI ĐA cho request idempotent (GET). POST không bao giờ tự retry.
const MAX_GET_RETRIES: int = 2

# Tên sự kiện phát qua EventBus (khớp danh mục EventBus.EVENTS).
const _EVENT_NETWORK_ERROR: StringName = &"network_error"
const _EVENT_UNAUTHORIZED: StringName = &"unauthorized"

## Base URL của API (gồm scheme+host+port, không có /api). Resolve khi _ready; có thể ghi đè.
var base_url: String = DEFAULT_BASE_URL
## Kho JWT (seam gắn token; đăng nhập/refresh thật ở phase 18/20).
var token_store: TokenStore = null
## Thời gian chờ mỗi request (giây) — chỉnh theo môi trường nếu cần.
var request_timeout_seconds: float = DEFAULT_REQUEST_TIMEOUT_SECONDS

# Tầng vận chuyển (seam để test không dùng mạng thật). Mặc định = GodotHttpTransport (HTTPRequest).
var _transport: HttpTransport = null


func _ready() -> void:
	base_url = _resolve_base_url()
	if token_store == null:
		token_store = TokenStore.new()
		# Nạp token đã lưu (đăng nhập lại tự động — phase 20). Thiếu/hỏng ⇒ rỗng, không crash.
		token_store.load()
	if _transport == null:
		_transport = GodotHttpTransport.new(self)


## GET JSON tới `path` (vd "/api/v1/server-time"). `parser` map Dictionary→model generated (null nếu
## không cần typed). Retry idempotent-safe (chỉ GET). Trả NetResult (thành công/lỗi đã chuẩn hoá).
func get_json(path: String, parser: Callable = Callable()) -> NetResult:
	return await _execute(HTTPClient.METHOD_GET, path, null, parser, true)


## POST JSON `body` tới `path`. KHÔNG tự retry (tránh double-effect; idempotency key = server phase 31).
func post_json(path: String, body: Dictionary, parser: Callable = Callable()) -> NetResult:
	return await _execute(HTTPClient.METHOD_POST, path, body, parser, false)


## Thay tầng vận chuyển (dùng cho composition/test — vd inject transport giả). Không dùng ở runtime thường.
func set_transport(transport: HttpTransport) -> void:
	_transport = transport


# ── Nội bộ ──────────────────────────────────────────────────────────────────────────────────

func _resolve_base_url() -> String:
	var from_env := OS.get_environment(ENV_BASE_URL)
	return from_env if from_env != "" else DEFAULT_BASE_URL


# Chạy một request có retry tuỳ chọn (chỉ bật cho GET). Trả NetResult; phát sự kiện lỗi nếu thất bại.
func _execute(method: int, path: String, body: Variant, parser: Callable, allow_retry: bool) -> NetResult:
	var request := {
		"url": base_url + path,
		"method": method,
		"headers": _build_headers(),
		"body": JSON.stringify(body) if body != null else "",
		"timeout": request_timeout_seconds,
	}
	var max_attempts := (1 + MAX_GET_RETRIES) if allow_retry else 1
	var last_failure: NetResult = null
	for attempt in range(max_attempts):
		var raw: Dictionary = await _transport.send(request)
		var transport_result := int(raw.get("result", HTTPRequest.RESULT_CANT_CONNECT))
		if transport_result != HTTPRequest.RESULT_SUCCESS:
			# Lỗi tầng vận chuyển (timeout/mất mạng). Retry nếu là GET + còn lượt + lỗi tạm thời.
			last_failure = _transport_failure(transport_result)
			if allow_retry and _is_transient(transport_result) and attempt < max_attempts - 1:
				continue
			return _fail(last_failure)
		# Kết nối OK — xét mã HTTP.
		var status := int(raw.get("response_code", 0))
		var body_bytes: PackedByteArray = raw.get("body", PackedByteArray())
		if status >= 200 and status < 300:
			return _handle_success(body_bytes, status, parser)
		return _fail(_handle_http_error(body_bytes, status))
	# An toàn (chỉ tới đây nếu max_attempts <= 0 — không xảy ra): trả lỗi cuối đã ghi nhận.
	return _fail(last_failure)


# Dựng headers JSON + Authorization (chỉ khi có token). KHÔNG log mảng này (chứa Bearer token).
func _build_headers() -> PackedStringArray:
	var headers := PackedStringArray([
		"Content-Type: application/json",
		"Accept: application/json",
	])
	if token_store != null and token_store.has_token():
		headers.append("Authorization: Bearer " + token_store.get_access_token())
	return headers


func _is_transient(transport_result: int) -> bool:
	return transport_result == HTTPRequest.RESULT_TIMEOUT \
		or transport_result == HTTPRequest.RESULT_CANT_CONNECT \
		or transport_result == HTTPRequest.RESULT_CONNECTION_ERROR


# Parse JSON không đẩy lỗi ra console (khác JSON.parse_string): trả về giá trị hoặc null.
func _parse_json(text: String) -> Variant:
	if text == "":
		return {}
	var json := JSON.new()
	if json.parse(text) != OK:
		return null
	return json.data


# 2xx: decode JSON → parser → NetResult. Lỗi decode/parse → NetResult lỗi (không bịa thành công).
func _handle_success(body_bytes: PackedByteArray, status: int, parser: Callable) -> NetResult:
	var text := body_bytes.get_string_from_utf8()
	var decoded: Variant = _parse_json(text)
	if not (decoded is Dictionary):
		return _fail(NetResult.failure(
			NetResult.Kind.INVALID_JSON,
			_client_error("INVALID_JSON", "Phản hồi không phải JSON hợp lệ."),
			status,
		))
	if not parser.is_valid():
		# Không yêu cầu typed model — trả Dictionary thô.
		return NetResult.success(decoded, status)
	var model: Variant = parser.call(decoded)
	if model == null:
		return _fail(NetResult.failure(
			NetResult.Kind.PARSE_ERROR,
			_client_error("PARSE_ERROR", "Phản hồi không khớp model mong đợi."),
			status,
		))
	return NetResult.success(model, status)


# Non-2xx: map ErrorResponse từ body (tổng hợp fallback nếu body không đúng envelope). 401 → UNAUTHORIZED.
func _handle_http_error(body_bytes: PackedByteArray, status: int) -> NetResult:
	var text := body_bytes.get_string_from_utf8()
	var decoded: Variant = _parse_json(text)
	var error: ErrorResponse = null
	if decoded is Dictionary:
		error = NetworkResponseParser.parse_error_envelope(decoded)
	if error == null:
		error = _client_error("HTTP_%d" % status, "Yêu cầu thất bại (HTTP %d)." % status)
	var kind := NetResult.Kind.HTTP_4XX
	if status == 401:
		kind = NetResult.Kind.UNAUTHORIZED
	elif status >= 500:
		kind = NetResult.Kind.HTTP_5XX
	return NetResult.failure(kind, error, status)


# Lỗi tầng vận chuyển → NetResult (timeout vs mất mạng), status 0.
func _transport_failure(transport_result: int) -> NetResult:
	if transport_result == HTTPRequest.RESULT_TIMEOUT:
		return NetResult.failure(
			NetResult.Kind.TIMEOUT,
			_client_error("NETWORK_TIMEOUT", "Hết thời gian chờ máy chủ."),
			0,
		)
	return NetResult.failure(
		NetResult.Kind.NETWORK_ERROR,
		_client_error("NETWORK_UNREACHABLE", "Không kết nối được máy chủ."),
		0,
	)


# Tổng hợp một ErrorResponse phía client (khi không có envelope từ server).
func _client_error(code: String, message: String) -> ErrorResponse:
	var error := ErrorResponse.new()
	error.code = code
	error.message = message
	error.trace_id = null
	return error


# Phát sự kiện lỗi rồi trả về NetResult (mọi thất bại đi qua đây ⇒ một kênh lỗi nhất quán).
func _fail(result: NetResult) -> NetResult:
	var payload := {
		"kind": result.kind,
		"code": result.error.code,
		"message": result.error.message,
		"trace_id": result.error.trace_id,
		"status": result.status_code,
	}
	EventBus.emit(_EVENT_NETWORK_ERROR, payload)
	if result.status_code == 401:
		EventBus.emit(_EVENT_UNAUTHORIZED, payload)
	# Log tối giản: chỉ code + status (KHÔNG token/Authorization/body nhạy cảm).
	push_warning("NetworkClient: request lỗi code=%s status=%d." % [result.error.code, result.status_code])
	return result
