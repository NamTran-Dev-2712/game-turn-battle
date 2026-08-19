# Test NetworkClient — Phase 15.
# Chứng minh: 200 parse đúng model generated; 4xx/5xx/401 map ErrorResponse + phát network_error;
# 401 phát thêm unauthorized; JSON/parse lỗi → lỗi rõ (không bịa thành công); timeout → báo lỗi;
# GET retry idempotent-safe; POST KHÔNG tự retry; autoload có mặt.
# Tất định — dùng FakeHttpTransport (không mạng/animation/wall-clock) (docs/testing/godot-testing.md).
extends GdUnitTestSuite

const _NETWORK_CLIENT := preload("res://src/core/net/network_client.gd")
const _SERVER_TIME_PATH := "/api/v1/server-time"

var _client: Node
var _fake: FakeHttpTransport
var _network_errors: Array = []
var _unauthorized_events: Array = []


func before_test() -> void:
	_client = _NETWORK_CLIENT.new()
	add_child(_client)  # kích hoạt _ready (base_url/token_store/transport mặc định)
	auto_free(_client)
	_fake = FakeHttpTransport.new()
	_client.set_transport(_fake)  # thay transport thật bằng giả (không mạng)
	_network_errors = []
	_unauthorized_events = []
	EventBus.subscribe(&"network_error", _on_network_error)
	EventBus.subscribe(&"unauthorized", _on_unauthorized)


func after_test() -> void:
	# Dọn kết nối (EventBus là autoload dùng chung giữa các test).
	EventBus.unsubscribe(&"network_error", _on_network_error)
	EventBus.unsubscribe(&"unauthorized", _on_unauthorized)


func _on_network_error(payload) -> void:
	_network_errors.append(payload)


func _on_unauthorized(payload) -> void:
	_unauthorized_events.append(payload)


func test_get_200_parses_generated_model() -> void:
	_fake.queue_ok(200, '{"utcNow":"2026-08-19T12:00:00+00:00"}')
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_true()
	assert_int(result.status_code).is_equal(200)
	var model := result.value as ServerTimeResponse
	assert_object(model).is_not_null()
	assert_str(model.utc_now).is_equal("2026-08-19T12:00:00+00:00")
	assert_int(_network_errors.size()).is_equal(0)


func test_4xx_maps_error_response_and_emits_network_error() -> void:
	_fake.queue_ok(400, '{"error":{"code":"VALIDATION_FAILED","message":"Yêu cầu sai.","traceId":"trace-123"}}')
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_false()
	assert_int(result.kind).is_equal(NetResult.Kind.HTTP_4XX)
	assert_str(result.error.code).is_equal("VALIDATION_FAILED")
	assert_str(result.error.message).is_equal("Yêu cầu sai.")
	assert_str(result.error.trace_id).is_equal("trace-123")
	assert_int(_network_errors.size()).is_equal(1)
	assert_int(_unauthorized_events.size()).is_equal(0)


func test_401_emits_unauthorized_and_network_error() -> void:
	_fake.queue_ok(401, '{"error":{"code":"UNAUTHENTICATED","message":"Thiếu token.","traceId":null}}')
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_false()
	assert_int(result.kind).is_equal(NetResult.Kind.UNAUTHORIZED)
	assert_str(result.error.code).is_equal("UNAUTHENTICATED")
	assert_object(result.error.trace_id).is_null()
	assert_int(_network_errors.size()).is_equal(1)
	assert_int(_unauthorized_events.size()).is_equal(1)


func test_5xx_is_normalized() -> void:
	_fake.queue_ok(500, '{"error":{"code":"INTERNAL_ERROR","message":"Lỗi máy chủ.","traceId":"t"}}')
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_false()
	assert_int(result.kind).is_equal(NetResult.Kind.HTTP_5XX)
	assert_str(result.error.code).is_equal("INTERNAL_ERROR")
	assert_int(result.status_code).is_equal(500)
	assert_int(_network_errors.size()).is_equal(1)
	# 5xx không tự retry (chỉ lỗi tầng vận chuyển mới retry).
	assert_int(_fake.call_count).is_equal(1)


func test_invalid_json_body_is_reported_not_faked() -> void:
	_fake.queue_ok(200, "khong-phai-json{")
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_false()
	assert_int(result.kind).is_equal(NetResult.Kind.INVALID_JSON)
	assert_object(result.value).is_null()
	assert_int(_network_errors.size()).is_equal(1)


func test_model_parse_failure_is_reported() -> void:
	# JSON hợp lệ nhưng thiếu field bắt buộc (utcNow) ⇒ parser trả null ⇒ PARSE_ERROR.
	_fake.queue_ok(200, '{"unexpected":"x"}')
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_false()
	assert_int(result.kind).is_equal(NetResult.Kind.PARSE_ERROR)
	assert_int(_network_errors.size()).is_equal(1)


func test_timeout_is_reported() -> void:
	_fake.queue_transport(HTTPRequest.RESULT_TIMEOUT)
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_false()
	assert_int(result.kind).is_equal(NetResult.Kind.TIMEOUT)
	assert_int(result.status_code).is_equal(0)
	assert_int(_network_errors.size()).is_equal(1)


func test_get_retries_transient_then_succeeds() -> void:
	_fake.queue_transport(HTTPRequest.RESULT_CANT_CONNECT)  # lần 1 rớt kết nối
	_fake.queue_ok(200, '{"utcNow":"2026-08-19T12:00:00+00:00"}')  # lần 2 thành công
	var result: NetResult = await _client.get_json(_SERVER_TIME_PATH, NetworkResponseParser.parse_server_time)
	assert_bool(result.ok).is_true()
	assert_int(_fake.call_count).is_equal(2)  # đã retry đúng một lần
	assert_int(_network_errors.size()).is_equal(0)  # thành công sau retry ⇒ không phát lỗi


func test_post_does_not_retry() -> void:
	_fake.queue_transport(HTTPRequest.RESULT_CANT_CONNECT)  # luôn rớt
	var result: NetResult = await _client.post_json("/api/v1/echo", {"a": 1})
	assert_bool(result.ok).is_false()
	assert_int(result.kind).is_equal(NetResult.Kind.NETWORK_ERROR)
	assert_int(_fake.call_count).is_equal(1)  # POST KHÔNG tự retry (tránh double-effect)


func test_networkclient_autoload_present() -> void:
	assert_object(get_node_or_null(^"/root/NetworkClient")).is_not_null()
