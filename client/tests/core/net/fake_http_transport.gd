# FakeHttpTransport — transport giả cho test NetworkClient (KHÔNG mạng thật ⇒ tất định, hợp CI).
# Nạp sẵn phản hồi canned; nếu nạp nhiều thì trả lần lượt rồi "dính" ở phản hồi cuối (repeat) —
# đủ cho cả canned đơn (lặp) lẫn chuỗi retry (fail→success). Đếm số lần gọi để kiểm retry/no-retry.
# `send` chờ một frame (frame-based, không wall-clock) rồi trả — giữ luồng await hợp lệ & tất định.
class_name FakeHttpTransport
extends HttpTransport

## Số lần `send` được gọi (kiểm retry GET / không-retry POST).
var call_count: int = 0

var _responses: Array = []


## Nạp một phản hồi HTTP thành công ở tầng vận chuyển (RESULT_SUCCESS) với mã + body JSON.
func queue_ok(response_code: int, body_text: String) -> void:
	_responses.append(_make(HTTPRequest.RESULT_SUCCESS, response_code, body_text))


## Nạp một lỗi tầng vận chuyển (vd RESULT_TIMEOUT, RESULT_CANT_CONNECT) — không có body.
func queue_transport(result: int) -> void:
	_responses.append(_make(result, 0, ""))


func send(_req: Dictionary) -> Dictionary:
	call_count += 1
	await Engine.get_main_loop().process_frame
	# Còn nhiều → tiến tới cái kế; còn một → giữ nguyên (lặp phản hồi cuối).
	if _responses.size() > 1:
		return _responses.pop_front()
	return _responses[0]


func _make(result: int, response_code: int, body_text: String) -> Dictionary:
	return {
		"result": result,
		"response_code": response_code,
		"headers": PackedStringArray(),
		"body": body_text.to_utf8_buffer(),
	}
