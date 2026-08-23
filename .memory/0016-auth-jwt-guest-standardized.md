# 0016 — Auth: JWT guest login standardized (Phase 18)

- Date: 2026-08-21
- Scope: workspace
- Status: Active

## Decision

Nền **xác thực guest bằng JWT + authorization mặc định** là hạ tầng chuẩn cho mọi API nghiệp vụ (ADR-008). Phase sau
**tái dùng**, không dựng cơ chế auth thứ hai.

- **`Account` aggregate** (`GameTeam.Domain/Accounts/`, `AggregateRoot<Guid>`): `Id` (Guid sinh ở code), `AccountType`
  (`None=0`/`Guest=1` — enum **Domain**, không phải wire contract), `CreatedAt` (từ `IClock`), factory `CreateGuest`
  raise `AccountCreated`. Là **ranh giới định danh** cho state server-authoritative (ADR-007). **Chừa chỗ provider
  linking** = bảng `account_providers` tương lai (Post-MVP, ADR-006) — **không** thêm cột provider nay (YAGNI).
- **Application:** `CreateGuestAccountCommand` (`Features/Auth/`, `ITransactionalRequest`) + handler **mỏng** (tạo
  account → `IRepository<Account,Guid>.AddAsync` → `ITokenService.CreateTokens` → `Result<AuthGuestResponse>`) +
  validator (`DeviceId` tuỳ chọn, chỉ bound độ dài). Port **`ITokenService`** (`Abstractions/Security/`, trả
  `TokenBundle`) — Application **không** biết JWT.
- **Infrastructure:** `JwtTokenService : ITokenService` (`Auth/`, HS256, claims `sub`=account id/`type`=guest/`jti`/
  `iat`/`nbf`/`exp`/`iss`/`aud`, thời gian từ `IClock`; refresh token 256-bit ngẫu nhiên base64url = **nền tảng**).
  **`JwtOptions`** (Options pattern, section `Jwt`) — issuer/audience/`AccessTokenMinutes` ở appsettings; **`SigningKey`
  từ env `Jwt__SigningKey`**. `AddInfrastructure` đăng ký `IOptions<JwtOptions>` **lazy** (factory) + **fail-fast**
  (thiếu key/issuer/audience hoặc key < 256-bit). `AccountConfiguration` (bảng **`accounts`** snake_case) + migration
  **`AddAccounts`**; `AccountCreated` dispatch tại `SaveChanges`.
- **Api:** `POST /api/v1/auth/guest` (vào **version set**, `.AllowAnonymous`, thin → MediatR → `ApiResults`).
  `AddApi` bật `AddAuthentication(JwtBearer).AddJwtBearer(...)` (validate chữ ký/issuer/audience/lifetime từ
  `IOptions<JwtOptions>`, `ClockSkew=0`) + `AddAuthorization` **`FallbackPolicy=RequireAuthenticatedUser`** ⇒
  **secure-by-default**. `Program.cs` bật `UseAuthentication()/UseAuthorization()`. **Public whitelist tường minh**
  `.AllowAnonymous()`: `/health`, `auth/guest`, `/openapi/*`, `/swagger`. Lỗi auth → **`ErrorEnvelope`** (401
  `UNAUTHENTICATED` / 403 `FORBIDDEN`) qua `AuthProblem` + `ErrorHttpMapping` (đã sẵn map, không sửa). `/ping` +
  `/server-time` nay **được bảo vệ**.

Verified (SDK 9.0.306, Windows + Docker Desktop 28.5.1): build Release **0/0**; `dotnet test` **167 pass** (Domain 38,
Contracts 36, Application 31, **Api.Integration 36**, Infrastructure 26). Mới: `AuthGuestEndpointTests` (guest→JWT+claims;
thiếu token→401 envelope; token hợp lệ→200; tampered/expired→401), `CreateGuestAccountCommandTests`, `AccountTests`,
`AccountPersistenceTests` (Testcontainers postgres16: CRUD + `AccountCreated` dispatch), `JwtTokenServiceTests`, arch test
`Application_should_not_depend_on_jwt_or_authentication_frameworks`. Migration `AddAccounts` up/down xanh, `has-pending`
sạch. **Runtime thật:** `/auth/guest`→JWT (sub=account id **khớp** hàng `accounts`, type=guest) + refresh + `expiresInSeconds`;
`/server-time` với token→200, thiếu→401 `UNAUTHENTICATED`, tampered→401; `/health`+`/openapi`→200. Negative: tắt
`FallbackPolicy` ⇒ Test B đỏ ⇒ revert xanh. Secret scan: không key/token trong log/source/appsettings commit.
`openapi.json` chỉ đổi thứ tự path (auth/guest vào version set) — không drift generated.

## Why

ADR-008: online-required, JWT auth, guest token → link sau. Mọi state server-authoritative cần danh tính xác thực (đặt
auth **trước** profile Phase 19 để state gắn đúng `sub`). Authorization **mặc định** (FallbackPolicy) ⇒ endpoint nghiệp vụ
tạo về sau **tự động được bảo vệ** — an toàn hơn "quên `[Authorize]` = public". JWT ở Infrastructure sau port
`ITokenService` (ADR-003/DIP) ⇒ handler thin, đổi cơ chế token không đụng Application.

## Not this

- **Đặt SigningKey trong appsettings.json / hardcode:** rò secret. Key **chỉ** từ env/secret `Jwt__SigningKey`, fail-fast
  khi thiếu; appsettings chỉ giá trị không bí mật. (Người dùng chọn `IOptions<JwtOptions>`.)
- **Đăng ký `IOptions<JwtOptions>` eager tại `AddInfrastructure`:** build-time OpenAPI gen chạy `AddInfrastructure` mà
  **chưa có key** ⇒ vỡ build + drift guard. Chọn **lazy** (factory) ⇒ validate ở lần resolve đầu (runtime).
- **Refresh-token architecture đầy đủ (rotation/persistence/`/auth/refresh`):** ngoài scope Phase 18. Trả refresh token
  **đục** thoả contract `AuthGuestResponse`; flow refresh thật = phase sau. (Người dùng chọn "foundation only".)
- **Để `/ping` public:** whitelist chỉ health/auth/openapi/swagger; `/ping`+`/server-time` **được bảo vệ** để chứng minh
  default-authz. (Người dùng chọn "also protect ping".)
- **`AccountType` trong `GameTeam.Contracts.Enums`:** không phải wire contract ⇒ để enum **Domain**, tránh sinh model
  client thừa qua codegen.
- **Login provider thật (Google/Apple/email) / account linking flow / profile game / anti-cheat / rate limit nâng cao:**
  Post-MVP / phase 19 / 53. BE3 (`docs/mvp/10`) giữ 🟠 — guest-first hiện thực, provider login chưa chốt.
- **500 → ProblemDetails cho lỗi auth:** giữ `ErrorEnvelope` (MỘT error contract, Phase 05 §3).

Liên quan: ADR-008 (networking/JWT), ADR-007 (save/identity), ADR-003 (Clean/DIP), ADR-006 (provider linking Post-MVP),
ADR-010 (CPM — thêm `Microsoft.AspNetCore.Authentication.JwtBearer` vào Api; `System.IdentityModel.Tokens.Jwt` dùng
transitive). Dùng lại [[0007-domain-foundation-standardized]] (`AggregateRoot`/`Result`/`IClock`/`Guard`),
[[0008-application-pipeline-standardized]] (`ITransactionalRequest`, `IRepository`, ports),
[[0009-persistence-standardized]] (`AppDbContext`/`EfRepository`/`UnitOfWork`/domain-event dispatch, migration),
[[0011-api-layer-standardized]] (`ErrorHttpMapping`/`ApiResults`/`ErrorEnvelope`, version set, `ApiTestFactory`). Canonical:
`docs/backend/api-and-versioning.md` §4.5 + `docs/backend/infrastructure.md` §2.5. Kế tiếp: Phase 19 (profile gắn account +
`sub`).
