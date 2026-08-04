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

- [ ] Định nghĩa enum dùng chung (Faction/Class/Element/Role, Currency, Rarity) theo glossary [`../mvp/12-glossary.md`](../mvp/12-glossary.md); giá trị enum ổn định (không đổi số thứ tự tuỳ tiện).
- [ ] Định nghĩa DTO nền: `AuthGuestRequest/Response`, `ProfileDto`, `ConfigBundleDto`/`ConfigVersion`, `ErrorResponse` (code/message/traceId).
- [ ] Chuẩn hoá quy ước `/api/v{major}/...`; ghi rõ chính sách breaking change.
- [ ] Cấu hình xuất OpenAPI (từ annotations/Swashbuckle hoặc source-gen) ra `shared/contracts/`.
- [ ] Thêm test: DTO serialize/deserialize round-trip, enum stable.
- [ ] Cập nhật `../backend/api-and-versioning.md` (error envelope, versioning).

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

Đóng khi contract nền + enum + OpenAPI spec ổn định, test round-trip xanh, hướng phụ thuộc đúng.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../mvp/12-glossary.md`](../mvp/12-glossary.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`06-config-json-schema.md`](06-config-json-schema.md)
