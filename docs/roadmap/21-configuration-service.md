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

- [ ] Pipeline nạp `config/` → chạy validate (tái dùng validator/lib phase 07) → fail thì không publish.
- [ ] Đóng gói bundle immutable: gộp config theo type, gắn `config_version` (`config@vN`) + `schema_version` + checksum.
- [ ] Lưu bundle (DB/artifact) + cache Redis key theo version.
- [ ] Hiện thực `IConfigProvider` (Infrastructure): load bundle hiện hành, truy vấn `get<T>(id)` cho Application/Domain (qua port).
- [ ] Endpoint `GET /config/bundle?version=` (trả bundle) + `GET /config/current` (version hiện hành).
- [ ] Cơ chế publish khi deploy (MVP): version tăng, immutable; ghi nền cho rollback (giữ version cũ).
- [ ] Integration test: publish → provider đọc; sửa giá trị config → version bump → bundle mới phục vụ.
- [ ] Cập nhật [`../liveops/remote-config.md`](../liveops/remote-config.md) + [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md).

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

Đóng khi Service publish bundle versioned + provider phục vụ + đổi config không rebuild + validator-fail chặn publish, integration test xanh. **Kết thúc Core Framework (P1).**

---

## Liên kết
- [`../liveops/remote-config.md`](../liveops/remote-config.md) · [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../backend/infrastructure.md`](../backend/infrastructure.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md)
- Roadmap: [`README.md`](README.md) → kế: [`22-client-config-bundle-e2e.md`](22-client-config-bundle-e2e.md)
