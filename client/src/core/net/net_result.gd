# NetResult — kết quả chuẩn hoá của một lời gọi mạng qua NetworkClient (ADR-008).
# Thành công: `ok = true`, `value` = model generated (hoặc Dictionary thô nếu không parser).
# Thất bại: `ok = false`, `error` = ErrorResponse (map từ server HOẶC tổng hợp phía client),
# `status_code` = mã HTTP (0 nếu lỗi tầng vận chuyển: timeout/mất mạng), `kind` = loại lỗi.
# Client KHÔNG bao giờ tự bịa kết quả — thất bại luôn trả về lỗi rõ ràng (ADR-008/011).
class_name NetResult
extends RefCounted

## Phân loại kết quả để caller/UI phản ứng đúng mà không đọc chuỗi.
enum Kind {
	SUCCESS,        ## 2xx + parse thành công.
	HTTP_4XX,       ## Lỗi client (400–499, trừ 401).
	HTTP_5XX,       ## Lỗi server (500–599).
	UNAUTHORIZED,   ## 401 — token thiếu/hết hạn (nối phase 18/20).
	INVALID_JSON,   ## Body 2xx không phải JSON hợp lệ.
	PARSE_ERROR,    ## JSON hợp lệ nhưng không khớp model generated.
	TIMEOUT,        ## Hết thời gian chờ.
	NETWORK_ERROR,  ## Không kết nối được / mất mạng.
}

## True nếu request thành công (2xx + parse xong).
var ok: bool = false
## Dữ liệu thành công: model generated (phase 08) hoặc Dictionary thô. Null khi thất bại.
var value: Variant = null
## Lỗi đã chuẩn hoá (code/message/trace_id). Null khi thành công.
var error: ErrorResponse = null
## Mã HTTP (0 nếu lỗi tầng vận chuyển — timeout/mất mạng).
var status_code: int = 0
## Loại kết quả (xem `Kind`).
var kind: int = Kind.SUCCESS


## Tạo kết quả thành công kèm `value` (model đã parse) và mã HTTP.
static func success(value: Variant, status_code: int) -> NetResult:
	var result := NetResult.new()
	result.ok = true
	result.value = value
	result.status_code = status_code
	result.kind = Kind.SUCCESS
	return result


## Tạo kết quả thất bại kèm `kind`, `error` đã chuẩn hoá và mã HTTP (0 nếu lỗi vận chuyển).
static func failure(kind: int, error: ErrorResponse, status_code: int) -> NetResult:
	var result := NetResult.new()
	result.ok = false
	result.error = error
	result.status_code = status_code
	result.kind = kind
	return result
