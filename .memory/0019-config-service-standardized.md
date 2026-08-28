# 0019 — Configuration Service standardized (Phase 21)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-08-26, Docker Desktop 28.5.1). **ĐÓNG Core Framework (P1).**
- **Bối cảnh:** Phase 05 để `ConfigBundleDto` tối thiểu + stub `/config/{version}` (501); Phase 10 khai `IConfigProvider`
  (chỉ `CurrentVersion`); Phase 13 đặt placeholder `DefaultConfigProvider` (config@v1 cố định). Phase 21 hiện thực
  Configuration Service thật: nạp `config/` → validate → build bundle bất biến → persist/cache → publish → phục vụ qua
  `IConfigProvider` (ADR-004/005). Config thật rỗng (chỉ README) tới phase 27+, nên pipeline chạy được với config rỗng-hợp-lệ.

## Quyết định (user-approved)

- **Publish khi deploy = startup `IHostedService`** (`ConfigPublishHostedService`, **user chọn** thay vì CLI/script out-of-band).
  Chạy `PublishAsync` MỘT LẦN lúc boot; **best-effort/graceful degradation** — mọi lỗi (DB chưa migrate; build-time OpenAPI gen
  boot host không có DB) → log Warning + nuốt, **không sập host** (như Redis degrade). Là hosted service ĐẦU TIÊN của repo.
- **Endpoint config = `.AllowAnonymous`** (**user chọn**): bundle là nội dung chung không nhạy cảm, client cache theo version,
  tách khỏi token. Whitelist thêm `/api/v1/config/current` + `/api/v1/config/bundle` cạnh health/auth/openapi/swagger.
- **`IConfigProvider` = `RuntimeConfigProvider`** (thay `DefaultConfigProvider`, **đã xoá**): giữ `ConfigSnapshot` **bất biến
  trong bộ nhớ** (field `volatile`, swap nguyên tử). `CurrentVersion`/`Get<T>(type,id)` (deserialize node→T, snake_case)/`GetIds`
  — **đồng bộ, không I/O** (giữ `CachingBehavior` đọc `CurrentVersion.Bundle` đồng bộ). Port mở rộng additive; `FixedConfigProvider`
  test cập nhật theo.
- **Tái dùng validator Phase 07** qua **ProjectReference** `tools/config-validator/GameTeam.ConfigValidator` (core lib thuần net9.0,
  không Console) — gọi `ConfigValidationRunner.Run` + `ConfigLoader.Load`. **KHÔNG fork validator thứ 2.** Cross-CPM-subtree ref
  build sạch (mỗi csproj resolve theo `Directory.Packages.props` gần nhất; không dùng trực tiếp JsonSchema.Net ⇒ không thêm gói server).
- **Bundle bất biến + checksum xác định:** `ConfigBundleBuilder` gộp `data{type:{id:node}}` (đủ 8 type key, sort). Checksum SHA-256
  trên canonical `{schema_version,data}` (**sort key đệ quy** ⇒ độc lập thứ tự file/JSON; **loại `generated_at`**) ⇒ **dedup**:
  config không đổi → không bump version (redeploy idempotent); đổi giá trị → version mới. Payload = envelope canonical phục vụ **nguyên văn**
  (checksum còn hiệu lực). `schema_version` = `VersionValidator.SupportedSchemaVersion` (=1).
- **Persist + atomic current:** bảng `config_bundles` (immutable, `version` PK, `config_version` unique, `schema_version`, `checksum`,
  `generated_at`, `payload`) + `config_current` (singleton pointer, **không seed** ⇒ rỗng = chưa publish). `ConfigBundleStore.SaveAndPublishAsync`
  insert bundle + flip con trỏ **trong 1 transaction** (`AppDbContext.Database.BeginTransactionAsync`) ⇒ "current" chỉ trỏ bundle hoàn chỉnh;
  warm cache **sau commit**. Migration `AddConfigBundles` (`has-pending-model-changes` sạch; up/down xanh qua `MigrationIntegrationTests`).
- **Cache Redis theo version = tái dùng `ICacheService` Phase 12** (**không** thêm domain/`RedisCacheKey` mới — doc-sync matrix nói "reuse it"):
  key `config-bundle:config@vN` (→ `{env}:cache:config-bundle:config@vN`), value = `CachedBundle` (payload string), TTL dài (immutable).
  `GetByVersionAsync` Redis→DB fallback→re-warm ⇒ v1 vẫn phục vụ sau khi v2 publish (immutable key không bị overwrite).
- **Endpoint param `bundleVersion` (KHÔNG `version`):** tên `version` trùng token `{version:apiVersion}` của version set ⇒ ApiExplorer
  không substitute (path spec kẹt `/api/v{version}/...`). Đây chính là xung đột Phase 05 cảnh báo — đổi tên param giải quyết tại chỗ,
  path sạch `/api/v1/config/bundle`. Stub literal `/config/{version}` **đã xoá** (reimplement vào version set).
- **ADR audit:** Phase 21 **thực thi** ADR-004/005 (data-driven, config strategy) + ADR-006 (đặt nền LiveOps) — **không** quyết định
  kiến trúc mới ⇒ KHÔNG sửa ADR.

## Verify

- Build Release **0 warning/0 error** (test project warning-as-error off). `dotnet test server/GameTeam.sln` **203 pass, 0 fail**:
  Domain 43, Contracts 36, Application 38, Api.Integration 45, Infrastructure 41.
- Unit `ConfigBundleBuilderTests` 6 (checksum xác định / độc lập thứ tự key / độc lập thứ tự file / đổi giá trị / envelope +
  generated_at không đổi checksum / đủ 8 type key). Integration Testcontainers `postgres:16-alpine`+`redis:7-alpine`
  `ConfigServiceIntegrationTests` 5 (publish→provider `Get`; đổi giá trị→v2 + v1 vẫn phục vụ immutable; validator-fail→không publish
  current giữ nguyên + invalid không phục vụ; redeploy trùng→không bump; immutable Redis key). `Api.IntegrationTests/ConfigEndpointTests` 4
  (deploy-publish qua hosted service→current v1 anonymous; bundle nguyên văn anonymous; thiếu version→current; 999→404 `CONFIG_BUNDLE_NOT_FOUND`).
- `has-pending-model-changes` sạch; migration up/down xanh. config-validator exit 0 trên config thật (rỗng). Codegen **no drift**
  (`ConfigBundleDto`/`ConfigVersion` giữ nguyên ⇒ chỉ `openapi.json` đổi path). **Grep guard sạch:** không `File.`/`Directory.`/`Path.`/`FileStream`
  trong Domain/Application (config đọc chỉ qua port). Arch test Application ⊥ Infra/EF vẫn xanh.
- **Sửa nợ Phase 19 (test-only):** thêm `RecordingPlayerProfileCreatedHandler` — test `Saving_new_profile_dispatches_PlayerProfileCreated`
  (Docker-only, "CI-pending" chưa chạy cục bộ) thiếu handler cho `PlayerProfileCreated` ⇒ collector rỗng. Không đụng code production.
  Cập nhật `OpenApiContractTests` (foundation paths → `/config/current` + `/config/bundle`).

## Ràng buộc cho agent sau

- **Tái dùng `IConfigProvider`/`RuntimeConfigProvider`/`ConfigBundlePublisher`/`ConfigBundleStore`/`ConfigBundleBuilder`** — **KHÔNG**
  tạo provider/publisher/store/validator/cache thứ 2, **không** đọc file config trong Domain/Application, **không** bypass port,
  **không** fork validator (dùng core lib Phase 07).
- **Bất biến + atomic (ADR-005):** version không bao giờ mutate (đổi config = version mới); con trỏ "current" flip **cuối cùng, trong
  transaction persist**; mỗi `config@vN` một key Redis riêng (không overwrite); giữ version cũ = nền rollback. **Validator-fail ⇒ không publish**
  (current giữ nguyên, invalid không phục vụ). Dedup theo checksum (config không đổi = không bump). Đổi config ⇒ **version mới, không rebuild client**.
- **Ngoài phạm vi (đặt nền, không làm):** client bundle e2e/caching = phase 22 (client `ConfigProvider` phase 16 đã có); typed POCO
  (hero/skill) = phase 27+ (provider giữ `Get<T>` generic); live swap không cần deploy = Post-MVP; feature flags/A-B = phase 49;
  admin publish/authz, signed bundle, delta download, rollback workflow = Post-MVP.
- Đồng bộ: `GameTeam.Infrastructure/Configuration/*` + `Persistence/ConfigBundleRecord`/`ConfigCurrentPointer` + configs + migration +
  `IConfigProvider` + `AddInfrastructure` + `Program.cs` (config endpoints version set) + `GameTeam.Infrastructure.csproj` (ProjectReference) +
  test (`ConfigBundleBuilderTests`/`ConfigServiceIntegrationTests`/`ConfigEndpointTests`) + regenerated `openapi.json` +
  `docs/backend/infrastructure.md §3.1` + `docs/backend/api-and-versioning.md §2/§4.5` + `docs/liveops/remote-config.md §4.1` +
  `docs/gameplay/configuration-and-data.md §5/§6` + CLAUDE.md §4.6 + `.instructions/backend.md` + `.instructions/config.md` +
  `.claude/agents/dotnet-backend.md` + doc-sync matrix row.

> Quyết định kiến trúc gốc: ADR-005 (config strategy), ADR-004 (data-driven), ADR-006 (LiveOps foundation). Liên quan:
> `.memory/0005` (validator Phase 07), `.memory/0009` (persistence), `.memory/0010` (Redis cache), `.memory/0011` (API layer).
