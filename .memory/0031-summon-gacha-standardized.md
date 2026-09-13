# 0031 — Summon/Gacha standardized (Phase 33)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-12, Docker Desktop + Godot 4.7.1). **Hoàn tất Collection Core (P3).**
  Triệu hồi **server-authoritative** (ADR-004/007/011): client gửi intent, **server** quyết RNG/rate/pity/hero/thưởng;
  tiêu tiền + cấp hero/mảnh **atomic + idempotent + concurrency-safe** trong MỘT transaction. Summon là **đường nhận
  hero thật** (thay seed tạm Phase 27/32).
- **Bối cảnh:** Phase 31 (ví/ledger/idempotency) + 32 (inventory) + 27 (OwnedHero) + 21 (config). Roadmap 33 yêu cầu
  tái dùng đúng các cơ chế đó — KHÔNG dựng transaction/idempotency/RNG thứ hai.

## Quyết định (thiết kế, đã justify — user-approved)

- **Compose, không tái phát minh.** `SummonCommandHandler` gọi `CurrencyWalletService.SpendAsync` (31) +
  `InventoryService.GrantAsync` (32) + `OwnedHero.Grant` (27) + `IBattleSeedSource.Next()` (seed server) +
  `Pcg32` (PRNG như combat). KHÔNG có ledger/idempotency/seed riêng cho gacha.
- **Gỡ seed TẠM "guest login cấp toàn bộ hero + mảnh" (Phase 27/32).** SSOT (`hero-system.md` §7) đánh dấu "tạm tới
  phase 33"; để summon là nhận thật + để test "hero mới → inventory" có nghĩa, gỡ vòng grant-all-hero + starter
  fragment khỏi `CreateGuestAccountCommandHandler` (giữ starter item catalog cho shop 40). Guest mới sở hữu 0 hero.
  Cập nhật test team/battle/hero/inventory dùng seed đó → grant tường minh qua **`IntegrationTestSeeding`** (decode
  JWT `sub` → grant OwnedHero/fragment/currency theo scope). *User chọn "Remove hero+fragment seed".*
- **RNG/rate/pity 100% server-side, seed KHÔNG trả client.** Khác combat (seed trả để client replay), gacha KHÔNG
  replay ở client ⇒ seed chỉ lưu `summon_records` để audit. Client `randi()` CHỈ sinh `requestId` (idempotency),
  không quyết reward.
- **Schema mở rộng additive (không bump `schema_version`).** `gacha.schema.json` +`cost{currency,amount}` (giá 1
  lượt) +`dupe_fragments[{rarity,amount}]` +`pity.target_rarity`. Optional về CẤU TRÚC; app **bắt buộc** cost +
  dupe phủ mọi rarity sinh-ra để banner quay được (kiểm ở `PrepareBanner` TRƯỚC khi tiêu tiền — không fail giữa
  transaction). Rarity hero đọc từ `hero.rarity`, KHÔNG lặp ở banner.
- **Pity persistent theo (profile, banner).** Bảng `gacha_pity` unique `(profile_id,banner_id)` + `FOR UPDATE`.
  Công thức: `count+1 ≥ threshold` ⇒ đảm bảo rarity mục tiêu; trúng mục tiêu (tự nhiên/pity) ⇒ reset 0, ngược lại
  +1. 10-pull = 10 lượt tuần tự (pity từng lượt) trong một thao tác atomic.
- **Idempotency hai lớp.** Handler-level `SummonRecord` unique `(profile_id,request_id)` (retry ⇒ kết quả đã lưu,
  không quay/tiêu/cấp lại) + backstop key `gacha:{requestId}:spend`/`:grant` vào ledger 31/32.
- **`CURRENCY_INSUFFICIENT_FUNDS` → 409.** Lần đầu lên HTTP ở summon; thêm vào `ErrorHttpMapping.KnownCodes` (code
  không có hậu tố `_CONFLICT`) — khớp intent doc Phase 31.
- **Client đọc banner từ config bundle** (`ConfigProvider.get_all("gacha")`) — KHÔNG endpoint liệt kê banner (nhất
  quán Phase 22 data-driven UI). Endpoint DUY NHẤT `POST /api/v1/summon`.

## Thành phần

- **Config/schema:** `shared/config-schema/gacha.schema.json` (+cost/dupe_fragments/pity.target_rarity) +
  `config/gacha/banner_standard.json` (pool 6 hero rarity 3/4/5, rates 790/190/20, pity 90 target 5, cost ticket 1,
  dupe 10/20/50). `ReferenceValidator.Gacha`/`ConfigType.Gacha` đã có (pool→hero).
- **Domain** `GameTeam.Domain/Gacha/`: `GachaPity : AggregateRoot<Guid>` (`CreateFor`/`Restore`/`SetCount`) +
  `SummonRecord : AggregateRoot<Guid>` (`Create`/`Restore`, JSON `pulls`) + `SummonPullLine` (value).
- **Application** `GameTeam.Application/Features/Summon/`: `SummonRoller` (thuần, static — rate/pity/RNG), `GachaConfig`
  (+nested Rate/Pity/Cost/DupeFragment) + `GachaMapping` (`ConfigType="gacha"`), `SummonCommand`(+Handler+Validator,
  `ITransactionalRequest`), `SummonErrors` (`GACHA_INVALID_COUNT`/`GACHA_BANNER_NOT_FOUND`/`GACHA_INVALID_BANNER`).
  Ports `IGachaPityRepository`(+`GetByProfileAndBannerForUpdateAsync`)/`ISummonRecordRepository`
  (+`GetByProfileAndRequestAsync`). Handler auto-reg (MediatR); `SummonRoller` static (không DI).
- **Infrastructure**: `GachaPityConfiguration`/`SummonRecordConfiguration` + `GachaPityRepository`(`FOR UPDATE`)/
  `SummonRecordRepository` + DbSets `GachaPities`/`SummonRecords` + DI + migration `AddSummon`.
- **API**: `POST /api/v1/summon` (version set, protected) → `SummonCommand`. `ErrorHttpMapping` +CURRENCY_INSUFFICIENT_FUNDS→409.
- **Contract**: `Contracts/Summon/{SummonRequest{bannerId,count,requestId}, SummonPullDto{heroId,rarity,isNew,fragments},
  SummonResultDto{bannerId,count,pityAfter,pulls[]}}` → `openapi.json` → codegen 3 file (`RealSpecTests` +3).
- **Client**: `NetworkResponseParser.parse_summon_result`; `client/src/ui/summon/{summon_view,summon_presenter,summon.tscn}`
  (banner từ ConfigProvider, gửi intent, hiển thị kết quả server, refresh ví+kho vào StateCache); route hub "Triệu hồi".
  KHÔNG event EventBus mới.
- **Guest handler**: `CreateGuestAccountCommandHandler` gỡ grant-all-hero + starter-fragment (giữ starter item).

## Chống tự-vẽ (binding)

- **Reuse:** dùng `SummonRoller`/`GachaPity`/`SummonRecord` + `CurrencyWalletService`/`InventoryService`/`OwnedHero`/
  `IBattleSeedSource`/`Pcg32` — KHÔNG simulator/ledger/idempotency/seed thứ hai; KHÔNG endpoint cấp hero/tiền công khai.
- **Server authority (ADR-011):** client CHỈ gửi intent + hiển thị; KHÔNG random/quyết reward/tính pity ở client; seed
  không trả client. Owner suy từ token (IDOR).
- **Data-driven (ADR-004):** rate/pity/cost/dupe từ config — đổi config → hành vi đổi, KHÔNG sửa code (test A/B chứng
  minh). Rarity từ hero config. Thêm banner dùng field sẵn = config-only.
- **Atomic + idempotent:** một transaction; retry cùng requestId ⇒ không double; thiếu tiền ⇒ 409 rollback toàn bộ;
  10-pull all-or-nothing; pity `FOR UPDATE` tuần tự hoá.
- Ngoài scope (nợ): banner rotation/limited/LiveOps, tune số rate, ascension tiêu fragment (39), shop redesign.

## Verify (2026-09-12)

- `dotnet test server/GameTeam.sln` — Domain **130** / Application **121** (Summon 9: rate distribution + data-driven A/B +
  pity boundary/reset + 10-pull) / Contracts **36** / Infrastructure **74** (Testcontainers pg16 `SummonPersistenceTests` 4:
  record/pity round-trip + unique + `FOR UPDATE`) / Api **98** (`SummonEndpointTests` 8: single/10-pull/dupe→fragment/
  idempotency/insufficient-409/banner-404/count-400/auth-401 + `OpenApiContractTests` summon).
- codegen **41** + drift sạch (3 file summon mới); config-validator exit 0 (18 file); `has-pending-model-changes` = "No changes"
  (migration `AddSummon` tạo `gacha_pity`+`summon_records`).
- Godot 4.7.1 `--headless --import` exit 0; gdUnit4 **150/150, 0 orphan** (`summon_presenter_test` 7).
- Security sweep client: `randi()` chỉ ở requestId (net + battle attemptId); không RNG quyết reward; `HTTPRequest` chỉ ở `core/net`.
- **CI-pending:** GitHub Actions (`ci-server`/`ci-client`/`codegen-check`/`validate-config`) — xanh đầy đủ ở local, chờ Actions.

## Liên kết

- CLAUDE.md §4.6 (Summon/Gacha block) · [[0029-currencies-transactions-standardized]] (spend/ledger/idempotency tái dùng) ·
  [[0030-inventory-standardized]] (grant mảnh/fragment) · [[0025-hero-system-standardized]] (OwnedHero + gỡ seed tạm) ·
  [[0028-battle-flow-standardized]] (mẫu record idempotency + seed server) · [[0021-combat-spec-fixedpoint-standardized]] (Pcg32) ·
  [[0011-api-layer-standardized]] (ErrorHttpMapping) · [[0006-codegen-pipeline-standardized]].
- Docs: `docs/gameplay/progression-and-economy.md` §5, `docs/gameplay/hero-system.md` §7,
  `docs/gameplay/configuration-and-data.md`, `docs/backend/infrastructure.md` §1.7, `docs/backend/api-and-versioning.md`,
  `docs/roadmap/33-summon-gacha.md`.
