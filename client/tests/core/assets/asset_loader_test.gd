# Test AssetLoader — Phase 27 (ADR-009). Lazy async texture load: path rỗng/không tồn tại → PLACEHOLDER
# (không chặn, không crash); path hợp lệ → texture thật + cache (lần sau trả ngay); release xoá cache.
# Tất định — chỉ chờ theo FRAME (coroutine), không wall-clock. (docs/testing/godot-testing.md)
extends GdUnitTestSuite

const _ASSET_LOADER := preload("res://src/core/assets/asset_loader.gd")
# Asset có sẵn trong project để test nạp thật (icon mặc định).
const _REAL_ASSET: String = "res://icon.svg"


func _make_loader() -> Node:
	var loader: Node = _ASSET_LOADER.new()
	add_child(loader)
	auto_free(loader)
	return loader


func test_placeholder_is_a_texture() -> void:
	var loader := _make_loader()
	assert_object(loader.placeholder()).is_not_null()
	assert_bool(loader.placeholder() is Texture2D).is_true()


func test_empty_path_returns_placeholder_without_blocking() -> void:
	var loader := _make_loader()
	var tex: Texture2D = await loader.load_texture("")
	assert_object(tex).is_same(loader.placeholder())
	assert_bool(loader.is_cached("")).is_false()


func test_missing_path_returns_placeholder() -> void:
	var loader := _make_loader()
	var tex: Texture2D = await loader.load_texture("res://does_not_exist_hero.png")
	assert_object(tex).is_same(loader.placeholder())


func test_loads_real_texture_and_caches_it() -> void:
	var loader := _make_loader()
	var tex: Texture2D = await loader.load_texture(_REAL_ASSET)

	assert_object(tex).is_not_null()
	assert_object(tex).is_not_same(loader.placeholder())  # art thật, không phải placeholder.
	assert_bool(loader.is_cached(_REAL_ASSET)).is_true()

	# Lần hai trả đúng instance đã cache (không nạp lại).
	var again: Texture2D = await loader.load_texture(_REAL_ASSET)
	assert_object(again).is_same(tex)


func test_release_removes_from_cache() -> void:
	var loader := _make_loader()
	await loader.load_texture(_REAL_ASSET)
	assert_bool(loader.is_cached(_REAL_ASSET)).is_true()
	loader.release(_REAL_ASSET)
	assert_bool(loader.is_cached(_REAL_ASSET)).is_false()
