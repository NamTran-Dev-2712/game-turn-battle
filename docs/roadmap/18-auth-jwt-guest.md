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

- [x] Domain: `Account` aggregate (id `Guid`, `AccountType` = Guest, `CreatedAt`, raise `AccountCreated`; chỗ cho provider links = bảng `account_providers` tương lai, không thêm cột nay) — `GameTeam.Domain/Accounts/`.
- [x] Application: `CreateGuestAccountCommand` + handler (`ITransactionalRequest`) + validator; port `ITokenService` (`TokenBundle`) — `GameTeam.Application/Features/Auth/`, `Abstractions/Security/`.
- [x] Infrastructure: `JwtTokenService : ITokenService` (HS256, key từ `JwtOptions`), `AccountConfiguration` (bảng `accounts` snake_case) + migration `AddAccounts` — `GameTeam.Infrastructure/Auth/`, `Persistence/`.
- [x] Api: endpoint `POST /api/v1/auth/guest` (vào version set, `.AllowAnonymous`); bật `UseAuthentication/UseAuthorization`; authorization **mặc định** qua `FallbackPolicy=RequireAuthenticatedUser`; whitelist public (health/auth/openapi/swagger). `/ping` + `/server-time` nay bảo vệ.
- [x] Cấu hình JWT issuer/audience/expiry qua `IOptions<JwtOptions>` (section `Jwt`); key từ env `Jwt__SigningKey` (fail-fast, không hardcode/commit; appsettings.json KHÔNG chứa key).
- [x] Integration test: guest login trả token hợp lệ (verify chữ ký + claims sub/type/exp/iss/aud); protected 401 khi thiếu token, 200 khi có; tampered/expired → 401 (`AuthGuestEndpointTests`, 6 test).
- [x] Cập nhật `../backend/api-and-versioning.md` (§3.1/§4.5 auth) + `../backend/infrastructure.md` (§ auth) + `../backend/cross-cutting.md` §1; ghi chú BE3 vào `../mvp/10-open-questions.md`.

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

**Trạng thái: ĐÓNG (local PASS 2026-08-21).** Guest login + JWT + authorization mặc định chạy đúng, key an toàn, integration test 401/200 xanh — đủ điều kiện Strict Phase Gate (README §5).

**Bằng chứng:**
- **Build:** `dotnet build GameTeam.sln -c Release` → **0 warning / 0 error** (warnings-as-error).
- **Test:** `dotnet test -c Release` → **167 pass / 0 fail** (Domain 38, Contracts 36, Application 31, Api.Integration 36, Infrastructure 26). Mới: `AuthGuestEndpointTests` (A guest→JWT+claims, B thiếu token→401 `UNAUTHENTICATED` envelope, C token hợp lệ→200, D tampered/expired→401), `CreateGuestAccountCommandTests`, `AccountTests`, `AccountPersistenceTests` (Testcontainers postgres:16-alpine: CRUD + `AccountCreated` dispatch), `JwtTokenServiceTests`, arch test `Application_should_not_depend_on_jwt_or_authentication_frameworks`.
- **Migration:** `AddAccounts` tạo bảng `accounts` (`id uuid` PK, `type int`, `created_at timestamptz`); `has-pending-model-changes` sạch; `Initial`/`schema_metadata` không đụng; `dotnet ef database update` up xanh.
- **Runtime thật:** API chạy (env `Jwt__SigningKey`) → `POST /api/v1/auth/guest` → JWT (sub=account id, type=guest, iss/aud/exp) + refresh + `expiresInSeconds=3600`; hàng `accounts` có `id` **khớp `sub`**, `type=1`; `GET /api/v1/server-time` với token → **200**; thiếu token → **401** `{code:UNAUTHENTICATED}`; tampered → 401; `/health`+`/openapi/v1.json` public → 200.
- **Negative test:** tắt `FallbackPolicy` ⇒ Test B đỏ (server-time trả 200) ⇒ khôi phục ⇒ xanh (chứng minh authz mặc định do code này bảo vệ).
- **Security:** không có signing key/token trong log runtime; không key thật trong source/appsettings commit; `.env`/`*.pem` đã git-ignore; secret test là placeholder rõ ràng.
- **Contract:** `openapi.json` chỉ thay đổi thứ tự path (auth/guest vào version set) — không đổi hình dạng; không drift `client/src/data/generated`.

**Deviation có chủ đích:** (1) `IOptions<JwtOptions>` đăng ký **lazy** (factory) để build-time OpenAPI gen không cần key; fail-fast chuyển sang lần resolve đầu (runtime) thay vì boot. (2) Test dùng clock ~now để mint token hợp lệ (FixedClock lịch sử của server-time làm token hết hạn khi validate theo wall-clock). (3) BE3 giữ 🟠 (guest-first hiện thực, provider login vẫn Post-MVP — không tự đóng).

**CI-pending:** kết quả trên GitHub Actions (build/test/openapi-drift/codegen-check) — chờ Actions xác nhận theo §4.5.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../backend/cross-cutting.md`](../backend/cross-cutting.md) · [`../mvp/10-open-questions.md`](../mvp/10-open-questions.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`19-profile-persistence-versioning.md`](19-profile-persistence-versioning.md)
