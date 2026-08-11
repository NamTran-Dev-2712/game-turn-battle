# 05 — Shared Contracts skeleton (contract-first)

> Mục đích: Định nghĩa **hợp đồng API + enum dùng chung** ở một nguồn duy nhất (`shared/contracts` + `GameTeam.Contracts`) trước khi hiện thực client/server, để hai phía làm song song không lệch.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 1 Hợp đồng & Config | P1 | S1 | nền data-driven |

# Mục tiêu

Tạo bộ contract skeleton: OpenAPI mô tả các endpoint nền (health, auth, profile, config-bundle) + shared enums (Faction/Class/Element/Role, Currency, Rarity…) đặt trong `GameTeam.Contracts` (C#) là nguồn, xuất `shared/contracts/` (OpenAPI/JSON) để codegen client (phase 08).

# Lý do

**Contract-first** (ADR-008): chốt hợp đồng versioned `/api/v1` trước giúp backend (nhóm 2) và client (nhóm 3) triển khai độc lập mà không drift. Enum dùng chung tránh mỗi phía tự định nghĩa lệch.

# Phụ thuộc

- **Trước:** 01–04.
- **Sau:** 08 (codegen), 13 (API layer hiện thực contract), 15 (client models), và mọi phase feature định nghĩa DTO mới sẽ mở rộng contract này.

# Phạm vi

- `GameTeam.Contracts`: DTO/Request/Response nền (Auth, Profile, ConfigBundle, Health, chuẩn Error envelope), enum dùng chung, quy ước versioning `/api/v{major}`.
- Sinh OpenAPI spec ra `shared/contracts/` (single source).
- Quy ước đặt tên contract theo [`../conventions/naming.md`](../conventions/naming.md) (`<Noun>Dto/Request/Response`).
- Error contract nhất quán (mã lỗi, message, traceId) — bám [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md).

# Không thuộc phạm vi

- Hiện thực endpoint (phase 13+).
- DTO của từng feature nghiệp vụ (hero/gacha/battle…) — thêm ở phase tương ứng.
- Codegen client (phase 08).

# Deliverables

- `GameTeam.Contracts` chứa DTO nền + enum dùng chung (một public type/file).
- OpenAPI spec xuất ra `shared/contracts/openapi.*`.
- Tài liệu quy ước versioning + error envelope trong `../backend/api-and-versioning.md`.
- Test biên dịch/serialize DTO nền.

# Công việc cần thực hiện

- [x] Định nghĩa enum dùng chung (Faction/Class/Element/Role, Currency, Rarity) theo glossary [`../mvp/12-glossary.md`](../mvp/12-glossary.md); giá trị enum ổn định (không đổi số thứ tự tuỳ tiện). → `server/src/GameTeam.Contracts/Enums/*.cs` (6 enum, `None=0` sentinel, số cố định). Faction chỉ có `None` vì danh sách phe (GP2) chưa chốt trong SSOT (`../mvp/10-open-questions.md`) — thêm additive khi chốt. Guard: `EnumStabilityTests` (36 test Contracts **xanh**; negative test: đổi `Class.Mage=9` → test **đỏ** đúng như kỳ vọng, đã revert).
- [x] Định nghĩa DTO nền: `AuthGuestRequest/Response`, `ProfileDto`, `ConfigBundleDto`/`ConfigVersion`, `ErrorResponse` (code/message/traceId). → `server/src/GameTeam.Contracts/{Auth,Profile,Config,Common}/*.cs` (record, một public type/file) + `ErrorEnvelope` (vỏ `{error:…}`) + `HealthResponse` + `ApiVersions`. Round-trip test **xanh**.
- [x] Chuẩn hoá quy ước `/api/v{major}/...`; ghi rõ chính sách breaking change. → `ApiVersions.V1Prefix=/api/v1`; route nền khai báo dưới `/api/v1` (Program.cs, stub 501 — chưa hiện thực); chính sách compatible/breaking + ổn định enum ghi ở `../backend/api-and-versioning.md` §4.
- [x] Cấu hình xuất OpenAPI (từ annotations/Swashbuckle hoặc source-gen) ra `shared/contracts/`. → .NET 9 first-party `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`/`MapOpenApi`) + `Microsoft.Extensions.ApiDescription.Server` (build-time) → `shared/contracts/openapi.json` (14 schema: 6 enum chuỗi + 8 DTO; 4 path). Transformer publish enum dùng chung vào components. Regenerate ổn định (diff clean).
- [x] Thêm test: DTO serialize/deserialize round-trip, enum stable. → `GameTeam.Contracts.Tests` (SerializationTests + EnumStabilityTests, 36) + `OpenApiContractTests` (validate OpenAPI bằng `Microsoft.OpenApi.Readers`, kiểm path/schema/enum-chuỗi, 20) + NetArchTest `Contracts` không ref App/Infra/Api (negative test **đỏ** khi inject dependency, đã revert). `dotnet test` **63 xanh**.
- [x] Cập nhật `../backend/api-and-versioning.md` (error envelope, versioning). → §3 error envelope `{error:{code,message,traceId}}` camelCase + tên DTO + quy tắc không rò nội bộ; §4/§4.1–4.4 major=v1, danh sách compatible/breaking, chính sách ổn định enum, contract-first/OpenAPI single-source + đường dẫn `shared/contracts/openapi.json`.

# Tiêu chí hoàn thành

- `GameTeam.Contracts` build sạch (warnings-as-error), một public type/file.
- OpenAPI spec sinh ra hợp lệ (validate bằng linter OpenAPI).
- Enum có giá trị ổn định; round-trip test pass.
- Không phụ thuộc ngược: `Contracts → Domain` (không ref Application/Infrastructure).

# Cách kiểm tra

- `dotnet build -c Release` + `dotnet test` (round-trip DTO/enum).
- Validate `shared/contracts/openapi.*` bằng công cụ OpenAPI (spectral/swagger-cli).
- Kiểm dependency hướng: NetArchTest xác nhận `Contracts` không ref Infra/App.

# Rủi ro

- **Enum đổi giá trị về sau phá contract** → khoá giá trị + chính sách chỉ thêm, không đổi/xoá (deprecate).
- **OpenAPI drift với code** → sinh spec từ code (single source), CI kiểm.
- **Over-design DTO sớm** → chỉ làm DTO nền; feature DTO thêm đúng phase.

# Ghi chú

Đây là "spine" của giao tiếp client-server (ADR-008). Feature sau chỉ **mở rộng**, không sửa contract nền theo kiểu breaking. Đổi contract ⇒ chạy doc-sync + regenerate codegen.

# Technical Debt Review

- **Maintainability:** single-source contract giảm lệch; versioning rõ.
- **Scalability:** versioned API cho phép tiến hoá không phá client cũ.
- **Testing:** round-trip đảm bảo tương thích serialize.
- **Security:** error envelope không rò rỉ stack/nội bộ.
- **Nợ:** feature DTO & realtime (SignalR) để phase tương ứng/Post-MVP.

# Phase Review

**PASS (local) — đủ điều kiện đóng, chờ CI xanh trên PR.**

Bằng chứng xác minh (chạy thật, .NET SDK 9.0.306, Windows):
- `dotnet build server/GameTeam.sln -c Release` → **0 Warning, 0 Error** (warnings-as-error bật).
- `dotnet test server/GameTeam.sln -c Release` → **63 pass / 0 fail** (Domain 1, Contracts 36, Application 4, Infrastructure 1, Api.IntegrationTests 21).
- OpenAPI: `shared/contracts/openapi.json` sinh từ code lúc build; hợp lệ (0 lỗi diagnostic của `Microsoft.OpenApi.Readers`); regenerate cho kết quả **không drift**.
- Dependency direction: NetArchTest xác nhận `Contracts` chỉ → Domain (không App/Infra/Api). **Negative test** (inject Domain-ref + cấm Domain) → **đỏ** đúng kỳ vọng, đã revert & re-verify xanh.
- Enum stability: **negative test** (đổi `Class.Mage=2→9`) → **đỏ** đúng kỳ vọng, đã revert.

Còn lại (CI-verification pending): job `ci-server` (build-test + bước mới *OpenAPI drift guard* + `architecture-test`) cần xanh trên GitHub Actions của PR — chưa chứng minh local vì chỉ chạy trên runner. Tất cả lệnh tương ứng đã xanh local.

Phạm vi: KHÔNG hiện thực handler nghiệp vụ (stub 501 — Phase 13), KHÔNG DTO feature (hero/gacha/battle), KHÔNG codegen client (Phase 08). Đúng ranh giới phase.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../mvp/12-glossary.md`](../mvp/12-glossary.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`06-config-json-schema.md`](06-config-json-schema.md)
