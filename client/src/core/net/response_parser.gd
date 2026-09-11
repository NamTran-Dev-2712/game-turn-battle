# NetworkResponseParser — map JSON (Dictionary) → model generated (phase 08).
# Model generated là DO-NOT-EDIT và chỉ chứa dữ liệu (không có `from_dict`) — các model tự ghi
# "Parse JSON: Phase 15". Lớp này là chỗ chứa logic parse đó, KHÔNG sửa file generated.
# Wire là camelCase, field GDScript là snake_case (utcNow↔utc_now, traceId↔trace_id).
# Thiếu key bắt buộc / sai kiểu ⇒ trả `null` (NetworkClient dịch thành PARSE_ERROR — không bịa kết quả).
# Thêm DTO mới ở các phase sau: thêm một hàm parse tĩnh vào đây (một hàm/DTO tiêu thụ).
class_name NetworkResponseParser
extends RefCounted


## Parse `GET /api/v1/server-time` → ServerTimeResponse. `null` nếu thiếu `utcNow`.
static func parse_server_time(data: Dictionary) -> ServerTimeResponse:
	if not data.has("utcNow"):
		return null
	var model := ServerTimeResponse.new()
	model.utc_now = str(data["utcNow"])
	return model


## Parse body lỗi `{ "error": { code, message, traceId } }` → ErrorResponse (đối tượng lỗi bên trong).
## `null` nếu không đúng hình dạng envelope (thiếu `error`/`code`/`message`).
static func parse_error_envelope(data: Dictionary) -> ErrorResponse:
	if not data.has("error") or not (data["error"] is Dictionary):
		return null
	var inner: Dictionary = data["error"]
	if not inner.has("code") or not inner.has("message"):
		return null
	var model := ErrorResponse.new()
	model.code = str(inner["code"])
	model.message = str(inner["message"])
	# traceId nullable: giữ null nếu vắng/null, ngược lại ép chuỗi.
	model.trace_id = null if inner.get("traceId", null) == null else str(inner["traceId"])
	return model


## Parse `GET /health` → HealthResponse. `null` nếu thiếu `status`. (Boot dùng làm cổng kết nối — phase 17.)
static func parse_health(data: Dictionary) -> HealthResponse:
	if not data.has("status"):
		return null
	var model := HealthResponse.new()
	model.status = str(data["status"])
	return model


## Parse `POST /api/v1/auth/guest` → AuthGuestResponse (phase 20). `null` nếu thiếu `accessToken`.
## refreshToken/expiresInSeconds tuỳ chọn (mặc định ""/0). TUYỆT ĐỐI không log các giá trị này.
static func parse_auth_guest_response(data: Dictionary) -> AuthGuestResponse:
	if not data.has("accessToken"):
		return null
	var model := AuthGuestResponse.new()
	model.access_token = str(data["accessToken"])
	model.refresh_token = str(data.get("refreshToken", ""))
	model.expires_in_seconds = int(data.get("expiresInSeconds", 0))
	return model


## Parse `GET /api/v1/profile` → ProfileDto (phase 20). `null` nếu thiếu `playerId`.
static func parse_profile(data: Dictionary) -> ProfileDto:
	if not data.has("playerId"):
		return null
	var model := ProfileDto.new()
	model.player_id = str(data["playerId"])
	model.display_name = str(data.get("displayName", ""))
	model.level = int(data.get("level", 0))
	model.schema_version = int(data.get("schemaVersion", 0))
	return model


## Parse `GET /api/v1/heroes` → Array[OwnedHeroDto] (phase 27). Body bọc object `{ "heroes": [ {heroId,
## level, stars} ] }` (không mảng trần — hợp NetworkClient chỉ nhận Dictionary). `null` nếu thiếu `heroes`
## hoặc phần tử sai hình dạng (thiếu `heroId`). Mảng rỗng là hợp lệ (chưa sở hữu hero nào).
static func parse_my_heroes(data: Dictionary) -> Variant:
	if not data.has("heroes") or not (data["heroes"] is Array):
		return null
	var result: Array[OwnedHeroDto] = []
	for item in data["heroes"]:
		if not (item is Dictionary) or not item.has("heroId"):
			return null
		var dto := OwnedHeroDto.new()
		dto.hero_id = str(item["heroId"])
		dto.level = int(item.get("level", 0))
		dto.stars = int(item.get("stars", 0))
		result.append(dto)
	return result


## Parse `GET/POST /api/v1/team` → TeamDto (phase 29). Body bọc object `{ "slots": [ {slotIndex, heroId} ] }`
## (không mảng trần — hợp NetworkClient chỉ nhận Dictionary). `null` nếu thiếu `slots` hoặc phần tử sai hình
## dạng. Mảng rỗng là hợp lệ (chưa lưu đội — lưới trống). Định nghĩa hero ghép từ ConfigProvider ở client.
static func parse_team(data: Dictionary) -> TeamDto:
	if not data.has("slots") or not (data["slots"] is Array):
		return null
	var slots: Array[TeamSlotDto] = []
	for item in data["slots"]:
		if not (item is Dictionary) or not item.has("slotIndex") or not item.has("heroId"):
			return null
		var slot := TeamSlotDto.new()
		slot.slot_index = int(item["slotIndex"])
		slot.hero_id = str(item["heroId"])
		slots.append(slot)
	var model := TeamDto.new()
	model.id = str(data.get("id", ""))
	model.slots = slots
	return model


## Parse `POST /api/v1/battles` → BattleResultDto (phase 30). Server-authoritative: seed để replay, outcome/
## rewards do server quyết, log = event log tất định (JSON chuỗi) để vẽ + đối chiếu. `null` nếu thiếu key bắt
## buộc (`outcome`/`log`) hoặc phần tử reward sai hình dạng — client KHÔNG bịa kết quả (ADR-011).
static func parse_battle_result(data: Dictionary) -> BattleResultDto:
	if not data.has("outcome") or not data.has("log"):
		return null
	var raw: Variant = data.get("rewards", [])
	if not (raw is Array):
		return null
	var rewards: Array[RewardDto] = []
	for item in raw:
		if not (item is Dictionary) or not item.has("rewardType") or not item.has("refId"):
			return null
		var reward := RewardDto.new()
		reward.reward_type = str(item["rewardType"])
		reward.ref_id = str(item["refId"])
		reward.amount = int(item.get("amount", 0))
		rewards.append(reward)
	var model := BattleResultDto.new()
	model.seed = int(data.get("seed", 0))
	model.outcome = str(data["outcome"])
	model.rounds = int(data.get("rounds", 0))
	model.rewards = rewards
	model.log = str(data["log"])
	return model


## Parse `GET /api/v1/wallet` → WalletDto (phase 31). Body bọc object `{ "balances": [ { currency, amount } ] }`
## (không mảng trần — hợp NetworkClient chỉ nhận Dictionary). `currency` wire là CHUỖI enum
## (JsonStringEnumConverter, vd "Gold") — map về Currency enum; loại không nhận diện bị bỏ qua (không bịa).
## `null` nếu thiếu `balances` hoặc phần tử sai hình dạng (thiếu `currency`). Mảng rỗng hợp lệ (ví rỗng).
static func parse_wallet(data: Dictionary) -> WalletDto:
	if not data.has("balances") or not (data["balances"] is Array):
		return null
	var balances: Array[CurrencyBalanceDto] = []
	for item in data["balances"]:
		if not (item is Dictionary) or not item.has("currency"):
			return null
		var currency_enum := _currency_from_wire(str(item["currency"]))
		if currency_enum == Currency.NONE:
			continue  # loại tiền không nhận diện — bỏ qua an toàn (không bịa)
		var bal := CurrencyBalanceDto.new()
		bal.currency = currency_enum
		bal.amount = int(item.get("amount", 0))
		balances.append(bal)
	var model := WalletDto.new()
	model.balances = balances
	return model


## Mã chuỗi StateCache (gold/gem/ticket) cho một giá trị Currency enum; "" nếu không nhận diện.
static func currency_code(currency_enum: int) -> String:
	match currency_enum:
		Currency.GOLD: return "gold"
		Currency.GEM: return "gem"
		Currency.TICKET: return "ticket"
		_: return ""


## Map chuỗi wire (vd "Gold"/"gold") → Currency enum; Currency.NONE nếu không nhận diện.
static func _currency_from_wire(wire: String) -> int:
	match wire.to_lower():
		"gold": return Currency.GOLD
		"gem": return Currency.GEM
		"ticket": return Currency.TICKET
		_: return Currency.NONE


## Parse metadata bundle `{ "version": { "bundle": int, "schema": int } }` → ConfigBundleDto.
## Dùng cho ConfigProvider so version (phase 16). `null` nếu thiếu `version`/`bundle`.
static func parse_config_bundle(data: Dictionary) -> ConfigBundleDto:
	if not data.has("version") or not (data["version"] is Dictionary):
		return null
	var inner: Dictionary = data["version"]
	if not inner.has("bundle"):
		return null
	var version := ConfigVersion.new()
	version.bundle = int(inner["bundle"])
	version.schema = int(inner.get("schema", 0))
	var model := ConfigBundleDto.new()
	model.version = version
	return model
