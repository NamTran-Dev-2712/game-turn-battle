# AssetLoader — autoload: nạp asset NẶNG (art hero…) LAZY + BẤT ĐỒNG BỘ (ADR-009). Tách dữ liệu nhẹ
# (config/Resource, nạp sớm) khỏi art nặng (nạp theo yêu cầu, KHÔNG chặn UI). Đường dẫn art đến TỪ CONFIG
# (id → path, ADR-004) — KHÔNG hardcode rải rác ở feature. Thiếu/đường dẫn rỗng/không nạp được ⇒ trả
# PLACEHOLDER (không crash, không chặn). Cache nhẹ theo path (pooling tối giản) + `release`/`clear` để giải
# phóng khi rời scene. Tối ưu nâng cao (atlas/nén/pool lớn) hoãn tới phase 52.
# Autoload BỎ `class_name` (trùng tên singleton) — truy cập qua global `AssetLoader`.
# Chi tiết: docs/godot/resources-and-assets.md §2, docs/adr/ADR-009-asset-loading.md.
extends Node

# Kích thước texture placeholder (vuông) — chỉ là ô giữ chỗ trung tính khi chưa/không có art.
const PLACEHOLDER_SIZE: int = 96

# Cache path → Texture2D đã nạp (pooling tối giản). Giải phóng bằng release()/clear().
var _cache: Dictionary = {}
# Placeholder dựng một lần (lazy) — dùng chung, không giữ tham chiếu asset nặng.
var _placeholder: Texture2D = null


## Nạp texture tại `path` (coroutine, KHÔNG chặn caller). Trả Texture2D đã nạp, hoặc PLACEHOLDER khi path
## rỗng / không tồn tại / nạp lỗi / không phải texture. Kết quả cache theo path (lần sau trả ngay).
func load_texture(path: String) -> Texture2D:
	if path == "":
		return placeholder()
	if _cache.has(path):
		return _cache[path]
	if not ResourceLoader.exists(path):
		return placeholder()

	var err := ResourceLoader.load_threaded_request(path)
	if err != OK:
		return placeholder()

	while true:
		var status := ResourceLoader.load_threaded_get_status(path)
		if status == ResourceLoader.THREAD_LOAD_LOADED:
			var res: Resource = ResourceLoader.load_threaded_get(path)
			if res is Texture2D:
				_cache[path] = res
				return res
			return placeholder()
		if status == ResourceLoader.THREAD_LOAD_FAILED \
				or status == ResourceLoader.THREAD_LOAD_INVALID_RESOURCE:
			return placeholder()
		# Còn đang nạp — nhường một frame (KHÔNG chặn), thử lại.
		await get_tree().process_frame
	# Không tới được.
	return placeholder()


## Texture placeholder dùng chung (dựng lazy). An toàn để hiển thị trong lúc chờ / khi không có art.
func placeholder() -> Texture2D:
	if _placeholder == null:
		_placeholder = _make_placeholder()
	return _placeholder


## Giải phóng một asset đã cache (gọi khi rời scene/feature không còn dùng — ADR-009 §3).
func release(path: String) -> void:
	_cache.erase(path)


## Xoá toàn bộ cache (vd reset nặng). Không đụng placeholder.
func clear() -> void:
	_cache.clear()


## True nếu `path` đang có trong cache (hỗ trợ kiểm thử/gỡ lỗi).
func is_cached(path: String) -> bool:
	return _cache.has(path)


# ── Nội bộ ──────────────────────────────────────────────────────────────────────────────────────

func _make_placeholder() -> Texture2D:
	var image := Image.create(PLACEHOLDER_SIZE, PLACEHOLDER_SIZE, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.20, 0.20, 0.25, 1.0))
	return ImageTexture.create_from_image(image)
