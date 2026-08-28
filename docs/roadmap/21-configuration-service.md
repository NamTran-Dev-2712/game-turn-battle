# 21 — Configuration Service (data-driven runtime)

> Mục đích: Xây **Configuration Service** phía backend làm SSOT runtime cho config: nạp → validate → version → publish **bundle bất biến** (`config@vN`), cache Redis, phục vụ qua `IConfigProvider`.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 4 Auth, Save & Config Service | P1 | S5 | nền data-driven |

# Mục tiêu

Backend nạp config từ `config/` (đã validate bằng validator phase 07) → đóng gói thành bundle versioned bất biến → cache Redis → endpoint phục vụ bundle theo version cho client; backend đọc config qua `IConfigProvider` (không đọc file trực tiếp trong Domain/Application).

# Lý do

ADR-005: config là dữ liệu runtime versioned; đổi config **không rebuild client**. Đây là nền cho **mọi** gameplay data-driven & LiveOps (phase 27+, 49+). Là bước S5 — mắt xích cuối của Core Framework trước combat.

# Phụ thuộc

- **Trước:** 07 (validator), 06 (schema), 11–12 (DB/Redis), 10 (IConfigProvider port), 13 (API).
- **Sau:** 22 (client e2e), 27+ (feature đọc config), 49 (remote config nâng cao).

# Phạm vi

- Pipeline: đọc `config/` → validate (tái dùng logic phase 07) → build bundle immutable gắn `config_version` + `schema_version` → lưu/cache.
- Hiện thực `IConfigProvider` (Application) trong Infrastructure: truy vấn config theo id/type từ bundle hiện hành.
- Endpoint `GET /api/v1/config/bundle?version=` + `GET current version`.
- Cache Redis theo `config@vN` (immutable → cache dài); publish khi deploy (MVP).

# Không thuộc phạm vi

- Live bundle swap không cần deploy (Post-MVP — chỉ đặt nền, ADR-005/006).
- Feature flags/A-B (phase 49).
- Client caching phía client (phase 22 — đã có ConfigProvider client phase 16).

# Deliverables

- Configuration Service: nạp/validate/version/publish bundle + `IConfigProvider` hiện thực.
- Endpoint bundle + current-version.
- Cache Redis bundle theo version.
- Integration test: publish bundle → provider trả config; đổi giá trị config → version mới, client không cần rebuild.

# Công việc cần thực hiện

- [x] Pipeline nạp `config/` → chạy validate (tái dùng `ConfigValidationRunner`/`ConfigLoader` phase 07 qua ProjectReference) → fail thì không publish. *(`ConfigBundlePublisher.PublishAsync`; test `Invalid_config_does_not_publish_and_leaves_current_unchanged` xanh.)*
- [x] Đóng gói bundle immutable: gộp config theo type → `data{type:{id:node}}`, gắn `config_version` (`config@vN`) + `schema_version` + **checksum SHA-256 xác định** (canonical, độc lập thứ tự key/file, loại `generated_at`). *(`ConfigBundleBuilder`; 6 unit test checksum xanh.)*
- [x] Lưu bundle (DB `config_bundles` + con trỏ `config_current`) + cache Redis key bất biến theo version (`config-bundle:config@vN`, tái dùng `ICacheService`). *(migration `AddConfigBundles`; test `Each_version_is_cached_under_its_own_immutable_redis_key` xanh.)*
- [x] Hiện thực `IConfigProvider` (Infrastructure `RuntimeConfigProvider`): snapshot bundle hiện hành trong bộ nhớ (swap nguyên tử), truy vấn `Get<T>(type,id)`/`GetIds` cho Application/Domain (qua port; thay `DefaultConfigProvider`). *(test `Publish_makes_the_provider_serve_config_by_id` xanh.)*
- [x] Endpoint `GET /api/v1/config/bundle?bundleVersion=` (trả bundle nguyên văn; thiếu ⇒ current; không tồn tại ⇒ 404 `ErrorEnvelope`) + `GET /api/v1/config/current` (version hiện hành). Cả hai `.AllowAnonymous`. *(param đổi tên `bundleVersion` để tránh trùng token `{version:apiVersion}`; `ConfigEndpointTests` 4/4 xanh.)*
- [x] Cơ chế publish khi deploy (MVP): `ConfigPublishHostedService` (`IHostedService`) chạy MỘT LẦN lúc boot — version tăng, immutable, dedup theo checksum (config không đổi ⇒ không bump); best-effort (không sập host nếu DB chưa migrate). Giữ version cũ = nền rollback. *(test `Changing_a_value_publishes_a_new_version_and_keeps_the_old_one` + `Republishing_identical_config_does_not_bump_the_version` xanh.)*
- [x] Integration test (Testcontainers Postgres+Redis): publish → provider đọc; sửa giá trị config → version bump → bundle mới phục vụ + bundle cũ vẫn phục vụ (immutable); validator-fail chặn publish; redeploy trùng ⇒ không bump; endpoint anonymous + 404. *(9 integration + 6 unit; `dotnet test` toàn bộ 203 pass.)*
- [x] Cập nhật [`../liveops/remote-config.md`](../liveops/remote-config.md) + [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) (+ `../backend/infrastructure.md §3`, `../backend/api-and-versioning.md`).

# Tiêu chí hoàn thành

- Publish bundle version X; `IConfigProvider` trả đúng config theo id.
- Đổi 1 giá trị config → publish version X+1 → phục vụ mới **không rebuild client** (chứng minh).
- Config sai (validator fail) → **không** publish (an toàn).
- Domain/Application **không** đọc file config trực tiếp (chỉ qua provider) — review/architecture check.

# Cách kiểm tra

- `dotnet test` (integration): publish→provider; version bump; validator-fail chặn publish.
- Local: đổi giá trị config → `up` → client (phase 22) nhận version mới không build lại.
- Rà: không có `File.Read` config trong Domain/Application.

# Rủi ro

- **Bundle không nhất quán/nửa chừng** → build atomic + checksum; chỉ chuyển "current" khi hoàn tất.
- **Cache phục vụ version cũ** → key immutable theo version; cập nhật con trỏ "current" nguyên tử.
- **Trùng logic validate với tool phase 07** → chia sẻ thư viện validate (cùng ngôn ngữ) để một nguồn sự thật.

# Ghi chú

Bundle **immutable** cho phép cache mạnh + rollback (giữ version cũ). Live swap không cần deploy là Post-MVP nhưng nền versioning/rollback đặt tại đây (ADR-005/006). Bám [`../liveops/remote-config.md`](../liveops/remote-config.md).

# Technical Debt Review

- **Maintainability:** một cổng đọc config (provider); đổi nguồn dễ.
- **Scalability:** cache version chịu tải; nền LiveOps.
- **Testing:** integration publish/serve/rollback.
- **Security:** chỉ phục vụ bundle đã validate; checksum chống hỏng.
- **Nợ:** live swap, feature flags (phase 49/Post-MVP).

# Phase Review

**ĐÓNG & verify cục bộ (2026-08-26, Docker Desktop 28.5.1).** Configuration Service publish bundle versioned bất
biến + `RuntimeConfigProvider` phục vụ qua `IConfigProvider` + đổi config → version mới phục vụ trên **cùng API build
(không rebuild client)** + validator-fail chặn publish (current giữ nguyên) + bundle cũ immutable (nền rollback).
Pipeline `config/ → ConfigLoader/ConfigValidationRunner (tái dùng phase 07) → ConfigBundleBuilder (checksum SHA-256 xác
định, dedup) → config_bundles + config_current (flip nguyên tử trong 1 transaction) → cache Redis theo version →
RuntimeConfigProvider`. Publish khi deploy qua `ConfigPublishHostedService` (best-effort, graceful degradation).
Domain/Application **không** đọc file config (grep guard sạch; đọc qua port). **Build Release 0/0; `dotnet test` 203 pass**
(Infrastructure 41 gồm 5 integration Config Service + 6 unit checksum; Api.Integration 45 gồm 4 config endpoint;
`has-pending-model-changes` sạch; migration up/down xanh); config-validator exit 0; codegen no drift. Đủ điều kiện đóng.
**Kết thúc Core Framework (P1).**

> Ngoài phạm vi (đặt nền, không làm): client bundle e2e/caching = phase 22; live swap không cần deploy = Post-MVP;
> feature flags/A-B = phase 49; typed config POCO (hero/skill) = phase 27+ (provider giữ `Get<T>` generic).
> Nợ ghi nhận: admin publish/authz, signed bundle, delta download, rollback workflow (Post-MVP).

---

## Liên kết
- [`../liveops/remote-config.md`](../liveops/remote-config.md) · [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../backend/infrastructure.md`](../backend/infrastructure.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md)
- Roadmap: [`README.md`](README.md) → kế: [`22-client-config-bundle-e2e.md`](22-client-config-bundle-e2e.md)
