# StateCache — autoload: READ-CACHE trạng thái người chơi (ADR-007). CHỈ HIỂN THỊ, KHÔNG chân lý.
# Giữ bản sao đọc của profile/currency/hero/progress để UI hiển thị + offline-view. Nguồn sự thật
# LUÔN ở server: mọi thay đổi state đi qua NetworkClient → command server → server trả snapshot mới →
# StateCache.apply_snapshot. Client KHÔNG tự cộng/trừ currency, KHÔNG tự tính reward/kết quả/progress.
# KHÔNG có API mutation chân lý: đường ghi DUY NHẤT là apply_snapshot (thay nguyên cả snapshot từ server).
# Đọc trả BẢN SAO ⇒ caller không sửa được cache. Autoload BỎ `class_name` — truy cập qua global `StateCache`.
# Chi tiết: docs/godot/state-and-signals.md §1.1, docs/adr/ADR-007-save-strategy.md.
extends Node

# BẤT BIẾN kiến trúc: cache này CHỈ để hiển thị. Đọc để tự-tài-liệu-hoá + guard review.
const IS_DISPLAY_ONLY: bool = true

# Thư mục + file cache đĩa (offline-view). Ghi đè được cho test.
const DEFAULT_CACHE_DIR: String = "user://state_cache"
const SNAPSHOT_FILE: String = "snapshot.json"

# Nhãn nguồn dữ liệu hiện tại.
const SOURCE_EMPTY: String = "empty"     ## Chưa có dữ liệu.
const SOURCE_SERVER: String = "server"   ## Vừa refresh từ server (tin cậy, online).
const SOURCE_CACHE: String = "cache"     ## Nạp từ đĩa khi boot (cũ/offline — cần nhãn "offline").

# Sự kiện phát khi state được refresh (khớp EventBus.EVENTS).
const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"

## Thư mục cache (đổi được cho test — inject temp dir).
var cache_dir: String = DEFAULT_CACHE_DIR

# Snapshot đọc — thành phần rời để truy vấn nhanh.
var _currencies: Dictionary = {}   # { code(String): amount(int) }
var _heroes: Array = []            # [ { id, ... } ] — bản ghi hero người chơi sở hữu
var _progress: Dictionary = {}     # { key(String): value }
var _profile: Dictionary = {}      # thông tin profile (playerId/displayName/level…)
var _source: String = SOURCE_EMPTY


func _ready() -> void:
	_load_snapshot_from_disk()


# ── Ghi (đường DUY NHẤT — chỉ từ server response) ─────────────────────────────────────────────────

## Thay TOÀN BỘ state cache bằng snapshot từ server response (invalidate dữ liệu cũ). Đánh dấu
## nguồn = server, lưu đĩa cho offline-view, phát `state_refreshed`. Đây KHÔNG phải mutation chân lý —
## chỉ phản chiếu sự thật server vừa trả. Hình dạng snapshot (mọi khoá tuỳ chọn):
## { "profile": {...}, "currencies": { code: int }, "heroes": [ {...} ], "progress": { key: value } }.
func apply_snapshot(snapshot: Dictionary) -> void:
	_profile = _dict_field(snapshot, "profile")
	_currencies = _dict_field(snapshot, "currencies")
	_heroes = _array_field(snapshot, "heroes")
	_progress = _dict_field(snapshot, "progress")
	_source = SOURCE_SERVER
	_persist_snapshot()
	EventBus.emit(_EVENT_STATE_REFRESHED, {"source": _source})


# ── Đọc (trả BẢN SAO — caller không sửa được cache) ──────────────────────────────────────────────

## Số dư một loại currency (0 nếu không có). Client KHÔNG tự đổi số này — chỉ đọc.
func get_currency(code: String) -> int:
	return int(_currencies.get(code, 0))


## Toàn bộ currency (bản sao). { code: amount }.
func get_currencies() -> Dictionary:
	return _currencies.duplicate(true)


## Danh sách hero người chơi sở hữu (mảng bản sao sâu).
func get_heroes() -> Array:
	return _heroes.duplicate(true)


## Một hero theo id (bản sao); {} nếu không có.
func get_hero(id: String) -> Dictionary:
	for hero in _heroes:
		if hero is Dictionary and str(hero.get("id", "")) == id:
			return (hero as Dictionary).duplicate(true)
	return {}


## Một giá trị progress theo khoá (null nếu không có).
func get_progress(key: String) -> Variant:
	var value: Variant = _progress.get(key, null)
	if value is Dictionary or value is Array:
		return value.duplicate(true)
	return value


## Toàn bộ progress (bản sao).
func get_all_progress() -> Dictionary:
	return _progress.duplicate(true)


## Profile (bản sao).
func get_profile() -> Dictionary:
	return _profile.duplicate(true)


## Nhãn nguồn dữ liệu hiện tại: "empty" | "server" | "cache".
func source() -> String:
	return _source


## True nếu dữ liệu đến từ cache đĩa (cũ/offline) — UI nên gắn nhãn "offline/cached".
func is_offline() -> bool:
	return _source == SOURCE_CACHE


## True — cache này CHỈ để hiển thị (không bao giờ là nguồn quyết định chân lý).
func is_display_only() -> bool:
	return IS_DISPLAY_ONLY


# ── Nội bộ ──────────────────────────────────────────────────────────────────────────────────────

func _dict_field(snapshot: Dictionary, key: String) -> Dictionary:
	var value: Variant = snapshot.get(key, {})
	return (value as Dictionary).duplicate(true) if value is Dictionary else {}


func _array_field(snapshot: Dictionary, key: String) -> Array:
	var value: Variant = snapshot.get(key, [])
	return (value as Array).duplicate(true) if value is Array else []


func _snapshot_path() -> String:
	return cache_dir.path_join(SNAPSHOT_FILE)


func _ensure_cache_dir() -> void:
	if not DirAccess.dir_exists_absolute(cache_dir):
		DirAccess.make_dir_recursive_absolute(cache_dir)


# Lưu snapshot xuống đĩa (offline-view). Chỉ dữ liệu hiển thị — không bí mật.
func _persist_snapshot() -> void:
	_ensure_cache_dir()
	var file := FileAccess.open(_snapshot_path(), FileAccess.WRITE)
	if file == null:
		push_warning("StateCache: không ghi được snapshot cache.")
		return
	file.store_string(JSON.stringify({
		"profile": _profile,
		"currencies": _currencies,
		"heroes": _heroes,
		"progress": _progress,
	}))
	file.close()


# Boot: nạp snapshot đĩa (nếu có) → nguồn = "cache" (offline/cũ) tới khi server refresh.
func _load_snapshot_from_disk() -> void:
	var path := _snapshot_path()
	if not FileAccess.file_exists(path):
		return
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return
	var text := file.get_as_text()
	file.close()
	var json := JSON.new()
	if json.parse(text) != OK or not (json.data is Dictionary):
		push_warning("StateCache: snapshot cache hỏng — bỏ qua.")
		return
	var snapshot: Dictionary = json.data
	_profile = _dict_field(snapshot, "profile")
	_currencies = _dict_field(snapshot, "currencies")
	_heroes = _array_field(snapshot, "heroes")
	_progress = _dict_field(snapshot, "progress")
	_source = SOURCE_CACHE
