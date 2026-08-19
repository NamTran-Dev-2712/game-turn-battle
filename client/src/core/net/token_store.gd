# TokenStore — kho JWT tối giản trong bộ nhớ cho NetworkClient (hạ tầng gắn token).
# Phase 15 CHỈ cung cấp seam để gắn `Authorization: Bearer <jwt>`; KHÔNG đăng nhập/refresh thật.
# Đăng nhập/lấy token, lưu bền, refresh, link account → phase 18/20 (thay/nâng cấp store này).
# TUYỆT ĐỐI không log token. Không hardcode token.
class_name TokenStore
extends RefCounted

var _access_token: String = ""


## Lưu access token hiện tại (gọi bởi lớp auth ở phase 18/20). Không persist ở phase 15.
func set_token(access_token: String) -> void:
	_access_token = access_token


## Xoá token (logout / token không hợp lệ).
func clear() -> void:
	_access_token = ""


## Access token hiện tại ("" nếu chưa có — khi đó NetworkClient bỏ qua header Authorization).
func get_access_token() -> String:
	return _access_token


## True nếu đang có token.
func has_token() -> bool:
	return _access_token != ""
