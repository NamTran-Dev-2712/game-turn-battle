# GodotHttpTransport — hiện thực HttpTransport bằng HTTPRequest (Godot).
# Nơi DUY NHẤT trong code first-party dùng HTTPRequest (grep guard: chỉ core/net).
# Mỗi request tạo một node HTTPRequest con của `host`, đặt timeout, gửi, chờ `request_completed`,
# rồi giải phóng node ⇒ không rò rỉ, hỗ trợ nhiều request song song.
class_name GodotHttpTransport
extends HttpTransport

# Node cha để gắn HTTPRequest (NetworkClient autoload). HTTPRequest cần nằm trong scene tree.
var _host: Node


func _init(host: Node) -> void:
	_host = host


## Gửi request thật qua HTTPRequest và chờ hoàn tất. Trả về kết quả thô (dạng request_completed).
func send(req: Dictionary) -> Dictionary:
	var http := HTTPRequest.new()
	http.timeout = float(req.get("timeout", 0.0))
	_host.add_child(http)
	var err := http.request(
		String(req.get("url", "")),
		PackedStringArray(req.get("headers", PackedStringArray())),
		int(req.get("method", HTTPClient.METHOD_GET)),
		String(req.get("body", "")),
	)
	if err != OK:
		# Không gửi được (URL sai, đang bận…): trả lỗi kết nối, không ném.
		http.queue_free()
		return {
			"result": HTTPRequest.RESULT_CANT_CONNECT,
			"response_code": 0,
			"headers": PackedStringArray(),
			"body": PackedByteArray(),
		}
	var completed: Array = await http.request_completed
	http.queue_free()
	return {
		"result": int(completed[0]),
		"response_code": int(completed[1]),
		"headers": completed[2] as PackedStringArray,
		"body": completed[3] as PackedByteArray,
	}
