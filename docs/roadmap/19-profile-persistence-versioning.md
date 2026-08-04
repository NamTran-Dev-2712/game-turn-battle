# 19 — Profile persistence & schema versioning

> Mục đích: Lưu **profile người chơi server-authoritative** (chân lý ở PostgreSQL) với schema versioning + migration (ADR-007) — nền save cho toàn bộ state game.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 4 Auth, Save & Config Service | P1 | S4 | F11 |

# Mục tiêu

Tạo aggregate `PlayerProfile` (gắn Account) với trường schema version, lưu/đọc qua repository/UoW; endpoint `GET/PUT profile` (thay đổi qua command server, atomic); migration hỗ trợ tiến hoá schema profile.

# Lý do

ADR-007: chân lý state (hero, currency, progression, inventory) ở backend; client chỉ cache đọc. Profile là "gốc" chứa/tham chiếu mọi state; phải có versioning + migration để tiến hoá không mất dữ liệu người chơi.

# Phụ thuộc

- **Trước:** 18 (account/auth), 11 (persistence), 10 (MediatR).
- **Sau:** 20 (client đọc profile), và mọi feature state (currency 31, hero 27/35, inventory 32, progress 34) mở rộng profile.

# Phạm vi

- Aggregate `PlayerProfile` (id, accountId, schema_version, timestamps, khung chứa/tham chiếu state con).
- Command đọc/khởi tạo profile khi guest login lần đầu; đọc profile của chính mình (authz theo sub).
- Schema versioning: trường version + cơ chế migration dữ liệu profile khi schema đổi.
- Idempotency nền cho thao tác khởi tạo (không tạo trùng).

# Không thuộc phạm vi

- Nội dung state cụ thể (currency/hero/inventory) — thêm ở phase feature (mở rộng profile).
- Config (phase 21).
- Client integration (phase 20).

# Deliverables

- `PlayerProfile` + migration + repository.
- Endpoint `GET /api/v1/profile` (của mình) + khởi tạo khi cần.
- Cơ chế + tài liệu schema-version migration profile.
- Integration test: tạo profile khi guest login; đọc profile; authz chặn đọc profile người khác.

# Công việc cần thực hiện

- [ ] Domain: `PlayerProfile` aggregate (accountId, schema_version, created/updated, khung state con).
- [ ] Application: `GetOrCreateProfileCommand`/`GetMyProfileQuery` + handler + validator; map DTO (contract phase 05).
- [ ] Infrastructure: cấu hình EF cho profile, migration; repository.
- [ ] Khởi tạo profile idempotent khi guest login lần đầu (không tạo trùng nếu retry).
- [ ] Authz: chỉ đọc/ghi profile của `sub` trong token.
- [ ] Schema versioning: trường `schema_version`, quy ước migration (map version cũ→mới) + test migration mẫu.
- [ ] Integration test: login→profile tạo; đọc profile; đọc profile người khác→403.
- [ ] Cập nhật [`../backend/domain-and-application.md`](../backend/domain-and-application.md) + [`../backend/infrastructure.md`](../backend/infrastructure.md).

# Tiêu chí hoàn thành

- Guest login lần đầu → profile được tạo (idempotent khi retry).
- `GET /profile` trả profile của chính mình; chặn người khác (403).
- `schema_version` tồn tại; có test migration version cũ→mới không mất dữ liệu.
- Mọi thay đổi profile qua command server (không client-authority).

# Cách kiểm tra

- `dotnet test` (integration): tạo/đọc profile, authz 403, migration.
- Local: guest login → `GET /profile` → dữ liệu đúng chủ.
- Thử tạo lại (retry) → không sinh profile trùng.

# Rủi ro

- **Migration làm hỏng dữ liệu người chơi** → migration có test + backup (vận hành phase 55); expand-then-contract.
- **Tạo profile trùng khi retry** → idempotency (khoá theo accountId).
- **Rò profile người khác** → authz theo `sub` bắt buộc + test.

# Ghi chú

Profile là root cho state; feature sau **mở rộng** profile (thêm bảng/tham chiếu), luôn tăng `schema_version` khi đổi cấu trúc + doc-sync. Bám ADR-007 + [`../backend/domain-and-application.md`](../backend/domain-and-application.md).

# Technical Debt Review

- **Maintainability:** save tập trung, versioning rõ.
- **Scalability:** profile tham chiếu state con, mở rộng không phá.
- **Testing:** integration + migration test bảo vệ dữ liệu.
- **Security:** server-authoritative + authz theo chủ sở hữu.
- **Nợ:** backup/restore vận hành (55); state con thêm dần.

# Phase Review

Đóng khi profile tạo/đọc server-authoritative + versioning + migration test + authz chủ sở hữu xanh.

---

## Liên kết
- [`../backend/domain-and-application.md`](../backend/domain-and-application.md) · [`../backend/infrastructure.md`](../backend/infrastructure.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`20-client-auth-profile-integration.md`](20-client-auth-profile-integration.md)
