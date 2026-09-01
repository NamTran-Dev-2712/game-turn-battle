# ConfigProvider — autoload: CỬA ĐỌC CONFIG DUY NHẤT của client (ADR-005, ADR-004).
# Nhận config bundle versioned (qua NetworkClient), cache BẤT BIẾN xuống đĩa theo `config@vN`,
# nạp lại khi boot (offline-view), phục vụ truy vấn dữ liệu cho feature theo id/type (data-driven —
# KHÔNG nhúng số gameplay). Đổi version = tạo cache mới; KHÔNG ghi đè version cũ, KHÔNG rebuild client.
# Client CHỈ đọc/cache: bundle là dữ liệu tham chiếu bất biến, không phải chân lý (chân lý ở server).
# Autoload BỎ `class_name` (trùng tên singleton) — truy cập qua global `ConfigProvider`.
# Chi tiết: docs/godot/resources-and-assets.md §1.1, docs/adr/ADR-005-configuration-strategy.md.
extends Node

# Thư mục cache config trên đĩa (ghi đè được cho test). Mỗi version một file BẤT BIẾN.
const DEFAULT_CACHE_DIR: String = "user://config_cache"
# Con trỏ version đang kích hoạt (nằm trong cache_dir).
const ACTIVE_POINTER_FILE: String = "active.json"
# Tiền tố nhãn bundle bất biến (khớp config-bundle.schema.json: ^config@v[0-9]+$).
const VERSION_PREFIX: String = "config@v"

# Đường dẫn API config THẬT (Config Service phase 21; e2e phase 22). Public (.AllowAnonymous).
#   - /config/current : version hiện hành (con trỏ "current") → ConfigBundleDto.
#   - /config/bundle?bundleVersion=N : bundle bất biến NGUYÊN VĂN theo version.
# LƯU Ý: query param tên "bundleVersion" (KHÔNG "version") — trùng token {version:apiVersion} của
# version set phía server (xem server/Program.cs). Sai tên ⇒ server hiểu là bản current.
const CONFIG_CURRENT_PATH: String = "/api/v1/config/current"
const CONFIG_BUNDLE_PATH: String = "/api/v1/config/bundle"
const CONFIG_BUNDLE_VERSION_PARAM: String = "bundleVersion"

# Sự kiện phát khi active config version đổi thành công (khớp EventBus.EVENTS).
const _EVENT_CONFIG_UPDATED: StringName = &"config_updated"

## Thư mục cache (đổi được cho test — inject temp dir).
var cache_dir: String = DEFAULT_CACHE_DIR
## NetworkClient dùng để tải bundle/version (seam — mặc định autoload; inject giả cho test).
var network_client: Node = null

# Bundle đang kích hoạt (Dictionary envelope: config_version/schema_version/data). Rỗng nếu chưa nạp.
var _active_bundle: Dictionary = {}
# Chỉ mục truy vấn nhanh: { type(StringName): { id(String): entry(Dictionary) } }.
var _index: Dictionary = {}
# Version số đang kích hoạt (N trong config@vN). 0 = chưa có config.
var _active_version: int = 0
# True nếu lần check_for_update gần nhất thấy version mới NHƯNG tải/áp bundle thất bại ⇒ đang dùng
# cache cũ (stale). Fallback KHÔNG im lặng (ADR-005/Rule E): trạng thái lộ ra + có nhật ký cảnh báo.
var _stale: bool = false
# Mã lỗi của lần cập nhật thất bại gần nhất ("" nếu không có). Phục vụ hiển thị/nhật ký, KHÔNG bịa.
var _last_error_code: String = ""


func _ready() -> void:
	if network_client == null:
		network_client = get_node_or_null(^"/root/NetworkClient")
	_ensure_cache_dir()
	_load_active_from_disk()


# ── API truy vấn (đọc — trả BẢN SAO để caller không sửa được cache) ───────────────────────────────

## Version số đang kích hoạt (N trong config@vN); 0 nếu chưa có bundle.
func current_version() -> int:
	return _active_version


## Nhãn bundle đang kích hoạt ("config@vN") hoặc "" nếu chưa có.
func config_label() -> String:
	return (VERSION_PREFIX + str(_active_version)) if _active_version > 0 else ""


## True nếu đã nạp một bundle hợp lệ.
func has_config() -> bool:
	return _active_version > 0


## True nếu đang dùng cache CŨ vì lần cập nhật gần nhất thất bại (fallback không im lặng — Rule E).
func is_stale() -> bool:
	return _stale


## Mã lỗi của lần cập nhật thất bại gần nhất ("" nếu không có). Chỉ để hiển thị/nhật ký.
func last_error_code() -> String:
	return _last_error_code


## True nếu tồn tại entry `id` trong `type`.
func has_entry(type: StringName, id: String) -> bool:
	return _index.has(type) and (_index[type] as Dictionary).has(id)


## Lấy một entry config theo `type` + `id`. Trả BẢN SAO sâu; {} nếu không có.
func get_entry(type: StringName, id: String) -> Dictionary:
	if not has_entry(type, id):
		return {}
	return (_index[type][id] as Dictionary).duplicate(true)


## Lấy tất cả entry của một `type` (mảng BẢN SAO sâu); [] nếu không có.
func get_all(type: StringName) -> Array:
	if not _index.has(type):
		return []
	var out: Array = []
	for id in (_index[type] as Dictionary):
		out.append((_index[type][id] as Dictionary).duplicate(true))
	return out


## Tiện ích đọc một hero config theo id (ví dụ trong roadmap). = get_entry(&"hero", id).
func get_hero(id: String) -> Dictionary:
	return get_entry(&"hero", id)


# ── Nạp / cache bundle ────────────────────────────────────────────────────────────────────────────

## Áp một bundle (từ server HOẶC test). Validate envelope, cache BẤT BIẾN xuống đĩa (config@vN,
## ghi-một-lần), kích hoạt, và phát `config_updated` NẾU version đổi. Trả false nếu bundle sai (không ném).
func apply_bundle(bundle: Dictionary) -> bool:
	var version := _extract_version(bundle)
	if version <= 0:
		push_warning("ConfigProvider: bundle thiếu/không hợp lệ 'config_version' — bỏ qua.")
		return false
	# config@vN là BẤT BIẾN: áp lại đúng version đang kích hoạt = no-op (không ghi đè, không phát event).
	if version == _active_version:
		_clear_stale()
		return true
	# Cache đĩa BẤT BIẾN: chỉ ghi nếu file version chưa tồn tại (không bao giờ ghi đè version cũ).
	_persist_bundle_if_absent(version, bundle)
	_write_active_pointer(version)
	_activate(bundle, version)
	_clear_stale()
	EventBus.emit(_EVENT_CONFIG_UPDATED, {
		"version": version,
		"config_version": VERSION_PREFIX + str(version),
	})
	return true


## Kiểm tra & cập nhật config từ Configuration Service (e2e phase 22). Luồng:
##   GET /config/current → so version với cache → nếu mới hơn → GET /config/bundle?bundleVersion=N →
##   apply_bundle (validate + cache đĩa bất biến config@vN + phát config_updated).
## Trả STATUS Dictionary { updated, used_fallback, error_code, has_config }:
##   - updated=true                → đã tải & kích hoạt version mới.
##   - used_fallback=true          → server có version mới NHƯNG tải/áp thất bại ⇒ GIỮ cache cũ (stale),
##                                    có nhật ký cảnh báo (KHÔNG im lặng — Rule E). error_code = lý do.
##   - updated=false,fallback=false → không có version mới (đã mới nhất) hoặc không hỏi được version.
## Mất mạng/lỗi ⇒ KHÔNG bịa dữ liệu: giữ cache hiện tại. (Endpoint public — không cần token.)
func check_for_update() -> Dictionary:
	if network_client == null:
		return _update_status(false, false, "NO_NETWORK_CLIENT")

	var version_result: NetResult = await network_client.get_json(
		CONFIG_CURRENT_PATH, NetworkResponseParser.parse_config_bundle)
	if not version_result.ok:
		# Không hỏi được version hiện hành: giữ cache. Nếu ĐÃ có config thì đây là suy giảm nhẹ
		# (offline-view), không đánh dấu stale (ta chưa biết có version mới hay không).
		return _update_status(false, false, _code_of(version_result))

	var server_version := _version_from_dto(version_result.value)
	if server_version <= 0 or server_version <= _active_version:
		# Không có gì mới → xoá cờ stale (đang ở version mới nhất server biết).
		_clear_stale()
		return _update_status(false, false, "")

	# Có version mới hơn → tải bundle bất biến theo version.
	var bundle_path := "%s?%s=%d" % [CONFIG_BUNDLE_PATH, CONFIG_BUNDLE_VERSION_PARAM, server_version]
	var bundle_result: NetResult = await network_client.get_json(bundle_path)
	if not bundle_result.ok or not (bundle_result.value is Dictionary):
		return _mark_stale(server_version, _code_of(bundle_result))
	if not apply_bundle(bundle_result.value):
		return _mark_stale(server_version, "INVALID_BUNDLE")
	return _update_status(true, false, "")


# Ghi nhận fallback KHÔNG im lặng: đánh dấu stale + nhật ký cảnh báo rõ ràng (dùng cache cũ).
func _mark_stale(server_version: int, error_code: String) -> Dictionary:
	_stale = true
	_last_error_code = error_code
	push_warning("ConfigProvider: không tải được config@v%d (%s) — đang dùng cache cũ %s." % [
		server_version, error_code, config_label() if has_config() else "(không có)"])
	return _update_status(false, true, error_code)


func _clear_stale() -> void:
	_stale = false
	_last_error_code = ""


func _update_status(updated: bool, used_fallback: bool, error_code: String) -> Dictionary:
	return {
		"updated": updated,
		"used_fallback": used_fallback,
		"error_code": error_code,
		"has_config": has_config(),
	}


func _code_of(res: NetResult) -> String:
	return res.error.code if res != null and res.error != null else ""


# ── Nội bộ ──────────────────────────────────────────────────────────────────────────────────────

# Đọc N từ envelope 'config_version' ("config@vN"). 0 nếu vắng/không đúng dạng.
func _extract_version(bundle: Dictionary) -> int:
	var label := str(bundle.get("config_version", ""))
	if not label.begins_with(VERSION_PREFIX):
		return 0
	var digits := label.substr(VERSION_PREFIX.length())
	if digits == "" or not digits.is_valid_int():
		return 0
	return int(digits)


# Lấy version số từ ConfigBundleDto đã parse (version.bundle). 0 nếu null.
func _version_from_dto(dto) -> int:
	if dto == null or dto.version == null:
		return 0
	return int(dto.version.bundle)


# Kích hoạt bundle trong bộ nhớ + dựng chỉ mục truy vấn.
func _activate(bundle: Dictionary, version: int) -> void:
	_active_bundle = bundle
	_active_version = version
	_index = _build_index(bundle)


# Dựng chỉ mục { type: { id: entry } } từ bundle["data"].
# Chấp nhận HAI hình dạng của `data[type]` để khớp mọi nguồn bundle:
#   - Dictionary map { id: entry }   → hình dạng Configuration Service THẬT phát (server phase 21).
#   - Array [ entry, ... ]           → hình dạng bundle rời (fixture/test cũ phase 16).
# Với map: khoá index theo `entry.id` (fallback về khoá map nếu entry thiếu `id`).
func _build_index(bundle: Dictionary) -> Dictionary:
	var index: Dictionary = {}
	var data: Variant = bundle.get("data", {})
	if not (data is Dictionary):
		return index
	for type in (data as Dictionary):
		var entries: Variant = data[type]
		var by_id: Dictionary = {}
		if entries is Dictionary:
			for key in (entries as Dictionary):
				var entry: Variant = entries[key]
				if entry is Dictionary:
					var id := str(entry.get("id", key))
					by_id[id] = entry
		elif entries is Array:
			for entry in (entries as Array):
				if entry is Dictionary and entry.has("id"):
					by_id[str(entry["id"])] = entry
		index[StringName(type)] = by_id
	return index


func _ensure_cache_dir() -> void:
	if not DirAccess.dir_exists_absolute(cache_dir):
		DirAccess.make_dir_recursive_absolute(cache_dir)


func _bundle_path(version: int) -> String:
	return cache_dir.path_join(VERSION_PREFIX + str(version) + ".json")


func _active_pointer_path() -> String:
	return cache_dir.path_join(ACTIVE_POINTER_FILE)


# Ghi bundle xuống đĩa CHỈ khi file version chưa tồn tại (bất biến — không ghi đè version cũ).
func _persist_bundle_if_absent(version: int, bundle: Dictionary) -> void:
	var path := _bundle_path(version)
	if FileAccess.file_exists(path):
		return
	_ensure_cache_dir()
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_warning("ConfigProvider: không ghi được cache bundle '%s'." % path)
		return
	file.store_string(JSON.stringify(bundle))
	file.close()


func _write_active_pointer(version: int) -> void:
	_ensure_cache_dir()
	var file := FileAccess.open(_active_pointer_path(), FileAccess.WRITE)
	if file == null:
		push_warning("ConfigProvider: không ghi được con trỏ active.")
		return
	file.store_string(JSON.stringify({"active_version": version}))
	file.close()


# Boot: đọc con trỏ active + bundle tương ứng từ đĩa (offline-view). Thiếu/hỏng → trạng thái rỗng.
func _load_active_from_disk() -> void:
	var pointer: Variant = _read_json_file(_active_pointer_path())
	if not (pointer is Dictionary) or not pointer.has("active_version"):
		return
	var version := int(pointer["active_version"])
	if version <= 0:
		return
	var bundle: Variant = _read_json_file(_bundle_path(version))
	if not (bundle is Dictionary):
		push_warning("ConfigProvider: cache bundle version %d thiếu/hỏng — bỏ qua." % version)
		return
	if _extract_version(bundle) != version:
		push_warning("ConfigProvider: cache bundle version không khớp con trỏ — bỏ qua.")
		return
	_activate(bundle, version)


# Đọc + parse một file JSON. Trả giá trị đã parse, hoặc null nếu thiếu/hỏng (không ném).
func _read_json_file(path: String) -> Variant:
	if not FileAccess.file_exists(path):
		return null
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return null
	var text := file.get_as_text()
	file.close()
	var json := JSON.new()
	if json.parse(text) != OK:
		return null
	return json.data
