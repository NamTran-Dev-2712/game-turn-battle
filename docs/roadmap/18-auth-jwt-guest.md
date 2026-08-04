# 18 — Auth: JWT guest login (server)

> Mục đích: Hiện thực xác thực **guest bằng JWT** (tài khoản khách, liên kết sau) làm cổng bảo mật cho mọi API nghiệp vụ (ADR-008).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 4 Auth, Save & Config Service | P1 | S4 | F11 |

# Mục tiêu

Endpoint `/api/v1/auth/guest` tạo tài khoản khách + phát JWT; middleware authentication/authorization bảo vệ endpoint; nền liên kết account (Google/Apple/email) để Post-MVP mà không refactor.

# Lý do

ADR-008: online-required, JWT auth, guest token → link sau. Mọi state server-authoritative (profile/currency/gacha) cần danh tính xác thực. Đặt auth trước profile (phase 19) để state gắn đúng chủ.

# Phụ thuộc

- **Trước:** 13 (API layer), 11 (persistence), 10 (MediatR).
- **Sau:** 19 (profile gắn account), 20 (client auth), mọi endpoint nghiệp vụ (authz).

# Phạm vi

- Command `CreateGuestAccount` → tạo account (id, tạo lúc, loại=guest) + phát JWT (claims: sub, type, exp).
- Middleware JWT authentication + `[Authorize]` mặc định cho API nghiệp vụ; endpoint public rõ ràng (health/auth).
- Cấu hình JWT (issuer/audience/key từ secret), refresh/expiry cơ bản.
- Nền liên kết account (bảng account cho phép thêm provider sau).

# Không thuộc phạm vi

- Login provider thật (Google/Apple/email) — Post-MVP (ADR-006/mvp).
- Profile dữ liệu game (phase 19).
- Anti-cheat nâng cao (phase 53).

# Deliverables

- Endpoint guest login + JWT; middleware authn/authz.
- Entity Account (guest) + migration.
- Integration test: guest login → token; gọi endpoint bảo vệ có/không token.
- Cập nhật [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) (auth) + open-question BE3 nếu cần.

# Công việc cần thực hiện

- [ ] Domain: `Account` aggregate (id, type=guest, created, chỗ cho provider links).
- [ ] Application: `CreateGuestAccountCommand` + handler + validator; port `ITokenService`.
- [ ] Infrastructure: hiện thực `ITokenService` (JWT ký bằng key từ secret), repo Account, migration.
- [ ] Api: endpoint `POST /api/v1/auth/guest`; bật JWT authentication middleware; `[Authorize]` default; whitelist public (health/auth/swagger dev).
- [ ] Cấu hình JWT issuer/audience/expiry; key từ secret (không hardcode, không commit).
- [ ] Integration test: guest login trả token hợp lệ; endpoint bảo vệ 401 khi thiếu token, 200 khi có.
- [ ] Cập nhật `../backend/api-and-versioning.md`; ghi chú BE3 (account type) vào `../mvp/10` nếu quyết định thêm.

# Tiêu chí hoàn thành

- Guest login trả JWT hợp lệ (verify chữ ký, claims đúng).
- Endpoint nghiệp vụ mặc định yêu cầu auth; thiếu token → 401 chuẩn (`ErrorResponse`).
- Key JWT từ secret; không lộ trong repo/log.
- Integration test xanh.

# Cách kiểm tra

- `dotnet test` (Api.IntegrationTests): guest→token; protected endpoint 401/200.
- Local: gọi `/auth/guest` → dùng token gọi `/server-time` (bảo vệ) thành công.
- Rà secret: không có key JWT trong source/appsettings commit.

# Rủi ro

- **Rò key JWT** → lấy từ secret store/env; `.claude/settings.json` đã deny đọc `.env`/`*.pem`.
- **Token không hết hạn/không refresh** → đặt expiry + nền refresh; chống replay.
- **Guest account trùng lặp/lạm dụng** → rate limit cơ bản (mở rộng phase 53).

# Ghi chú

Guest-first giảm ma sát onboarding (A20). Liên kết account & provider thật là Post-MVP nhưng schema account chừa chỗ (ADR-006). Bám ADR-008.

# Technical Debt Review

- **Maintainability:** auth tách rõ; account mở rộng provider dễ.
- **Scalability:** JWT stateless hợp modular monolith.
- **Testing:** integration cover authn/authz.
- **Security:** key từ secret, expiry, authz mặc định — trọng tâm phase này.
- **Nợ:** provider login, refresh nâng cao, rate limit (Post-MVP/53).

# Phase Review

Đóng khi guest login + JWT + authz mặc định chạy, key an toàn, integration test 401/200 xanh.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../backend/cross-cutting.md`](../backend/cross-cutting.md) · [`../mvp/10-open-questions.md`](../mvp/10-open-questions.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`19-profile-persistence-versioning.md`](19-profile-persistence-versioning.md)
