# HttpTransport — hợp đồng tầng vận chuyển HTTP (seam) cho NetworkClient.
# Tách vận chuyển khỏi logic (parse/lỗi/retry) ⇒ test dùng transport giả (không mạng thật),
# production dùng `GodotHttpTransport` (bọc HTTPRequest). Đây là điểm DUY NHẤT chạm HTTPRequest.
# `send` là coroutine: nhận request đã dựng sẵn, trả về kết quả thô đúng dạng tín hiệu Godot
# `request_completed`: { "result": int, "response_code": int, "headers": PackedStringArray,
# "body": PackedByteArray }. Không parse, không quyết nghiệp vụ — chỉ truyền tải.
class_name HttpTransport
extends RefCounted


## Gửi một request và chờ phản hồi. Lớp con PHẢI override.
## `req` = { "url": String, "method": int (HTTPClient.Method), "headers": PackedStringArray,
##          "body": String, "timeout": float }.
func send(_req: Dictionary) -> Dictionary:
	push_error("HttpTransport.send: lớp con phải override.")
	return {}
