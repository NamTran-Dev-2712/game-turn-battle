# EventBus — autoload pub/sub toàn cục (ADR-002).
# Cho các feature giao tiếp LỎNG LẺO mà không import chéo nhau.
# Danh mục sự kiện = hằng `EVENTS` + các `signal` khai báo bên dưới (đóng, tự tài liệu
# hoá) ⇒ chống "God channel": chỉ tên nằm trong danh mục mới hợp lệ.
# Quy ước: mọi sự kiện mang ĐÚNG MỘT tham số `payload` (Dictionary).
# Danh mục đầy đủ + cách thêm sự kiện: docs/godot/state-and-signals.md.
extends Node

## Danh mục sự kiện nền — nguồn sự thật ĐÓNG (chỉ các tên ở đây mới hợp lệ).
## Thêm sự kiện mới: khai báo một `signal <name>(payload)` VÀ thêm tên vào đây,
## rồi tài liệu hoá trong docs/godot/state-and-signals.md.
const EVENTS: Array[StringName] = [
	&"scene_changed",
	&"network_error",
	&"unauthorized",
]

## Điều hướng scene đã hoàn tất. payload = { "to": String, "from": String }.
## Producer: SceneRouter · Consumer: feature bất kỳ cần phản ứng khi đổi scene.
signal scene_changed(payload)

## Một request mạng thất bại (HTTP 4xx/5xx, JSON/parse lỗi, timeout, mất mạng).
## payload = { "kind": int (NetResult.Kind), "code": String, "message": String,
##            "trace_id": Variant, "status": int }.
## Producer: NetworkClient · Consumer: UI/feature hiển thị lỗi, retry, thông báo.
signal network_error(payload)

## Request bị từ chối do chưa/không còn xác thực (HTTP 401). Kèm cả `network_error`.
## payload giống `network_error` (status = 401). Producer: NetworkClient ·
## Consumer: lớp auth (phase 18/20) kích hoạt đăng nhập lại / refresh token.
signal unauthorized(payload)


func _ready() -> void:
	# Bất biến: mọi tên trong danh mục phải có signal hậu thuẫn (bắt lệch sớm).
	for event in EVENTS:
		assert(has_signal(event), "EventBus: '%s' trong EVENTS nhưng thiếu signal." % event)


## Phát một sự kiện đã đăng ký kèm `payload` tới mọi subscriber.
func emit(event: StringName, payload: Variant = null) -> void:
	assert(EVENTS.has(event), "EventBus: sự kiện chưa đăng ký '%s' — khai báo signal + thêm vào EVENTS + tài liệu hoá trước khi dùng." % event)
	emit_signal(event, payload)


## Đăng ký `callback` cho `event`. An toàn khi gọi lặp (không nối trùng).
func subscribe(event: StringName, callback: Callable) -> void:
	assert(EVENTS.has(event), "EventBus: sự kiện chưa đăng ký '%s'." % event)
	if not is_connected(event, callback):
		connect(event, callback)


## Huỷ đăng ký `callback` khỏi `event` (an toàn nếu chưa nối) — tránh rò rỉ subscriber.
func unsubscribe(event: StringName, callback: Callable) -> void:
	if has_signal(event) and is_connected(event, callback):
		disconnect(event, callback)


## True nếu `event` nằm trong danh mục (đã đăng ký). Dùng để kiểm/định tuyến an toàn.
func is_known(event: StringName) -> bool:
	return EVENTS.has(event)
