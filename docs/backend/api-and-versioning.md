# API Design & Versioning

> Hợp đồng API client↔server: thiết kế, versioning, error contract, và versioning DB/schema. Nền: ADR-008. Áp dụng tư duy `api-design-best-practice`.

---

## 1. Nguyên tắc API
| Nguyên tắc | Chi tiết |
|---|---|
| REST + JSON | Giao thức chính; JWT bearer (ADR-008) |
| Contract-first | Định nghĩa ở `shared/contracts` (OpenAPI) → codegen client |
| Versioned | Prefix `/api/v{major}/` |
| Server-authoritative | Endpoint nhận **ý định**, server quyết kết quả (ADR-011) |
| Idempotency | Header `Idempotency-Key` cho command nhạy cảm (claim/summon/battle) |
| Consistent naming | Resource danh từ số nhiều (`/heroes`, `/battles`), động từ = HTTP method |
| Pagination | Cursor/limit cho list lớn (inventory) — `../mvp/08` |

---

## 2. Nhóm endpoint (MVP — minh hoạ, không phải hợp đồng cuối)

| Nhóm | Ví dụ | Loại |
|---|---|---|
| Auth | `POST /api/v1/auth/guest`, `/auth/refresh` | command |
| Profile | `GET /api/v1/profile` | query |
| Config | `GET /api/v1/config/current`, `GET /api/v1/config/bundle?bundleVersion=N` | query (public, cache) |
| Heroes | `GET /api/v1/heroes` (owned, protected — owner từ token), `GET /api/v1/heroes/{heroId}/definition` (config, public) | query (Phase 27) |
| Formation | `PUT /api/v1/teams/{id}` | command |
| Battle | `POST /api/v1/battles` | command (re-sim) |
| Summon | `POST /api/v1/summons` | command (idempotent) |
| Campaign | `GET /api/v1/campaign`, `POST /api/v1/campaign/{stage}/sweep` | query/command |
| Economy | `POST /api/v1/afk/claim`, `POST /api/v1/shop/purchase` | command (idempotent) |
| Mail | `GET /api/v1/mail`, `POST /api/v1/mail/{id}/claim` | query/command |

> Endpoint chính thức chốt ở `shared/contracts`; bảng trên định hướng.

---

## 3. Error contract (chuẩn hoá)

Body lỗi là **envelope** bọc một đối tượng lỗi (hình dạng trên dây, camelCase — Web JSON defaults):

```json
{
  "error": {
    "code": "INSUFFICIENT_CURRENCY",
    "message": "Không đủ gem để summon.",
    "traceId": "..."
  }
}
```
- Nguồn contract (Phase 05): `GameTeam.Contracts.Common.ErrorResponse` (`code`/`message`/`traceId`) là đối
  tượng bên trong; `ErrorEnvelope` là vỏ `{ "error": … }`. Mọi response lỗi dùng `ErrorEnvelope`.
- Mã lỗi ổn định (enum), message hiển thị được, `traceId` để tra log.
- **An toàn sản xuất:** error envelope **TUYỆT ĐỐI không** rò stack trace, chi tiết exception, thông tin
  DB/query, hay chi tiết dịch vụ/hạ tầng nội bộ. Chỉ `code` + `message` hiển thị được + `traceId`.
- HTTP status hợp lý (400 validate, 401/403 auth, 409 idempotency/conflict, 422 rule nghiệp vụ, 5xx server).

### 3.1 Hiện thực xử lý lỗi ở tầng Api (Phase 13 — đã chốt)

Tất cả tập trung ở **`GameTeam.Api/Http/`** — endpoint **KHÔNG** tự map lỗi:

- **`ErrorHttpMapping`** — MỘT bảng ánh xạ `Error.Code` → HTTP status (Phase 09 dùng `Error(Code, Message)`,
  code `SCREAMING_SNAKE_CASE`, chưa có enum taxonomy đóng): explicit `VALIDATION_FAILED`→400, rồi **quy ước
  hậu tố** (KHÔNG chế code cụ thể) `*_NOT_FOUND`→404, `*_CONFLICT`→409, `UNAUTHENTICATED`/`*_UNAUTHORIZED`→401,
  `*_FORBIDDEN`→403, mặc định 400. Phase sau thêm code mới nhận đúng status mà không sửa file này.
- **`ApiResults`** — adapter mỏng: `Result`/`Result<T>` (handler MediatR trả) → HTTP. Success → 200 (rỗng cho
  `Result`, có body cho `Result<T>`); failure → `ErrorEnvelope` với status từ `ErrorHttpMapping`. `traceId` =
  `Activity.Current?.Id ?? HttpContext.TraceIdentifier` (tra log). Endpoint gọi một dòng, **không rải logic map**.
- **`GlobalExceptionHandler` (`IExceptionHandler`)** + `app.UseExceptionHandler()` — exception CHƯA bắt → **500
  `ErrorEnvelope`** (code `INTERNAL_ERROR`, message an toàn, traceId); exception đầy đủ chỉ **log server-side**
  (cùng traceId), **không** gửi client. `AddProblemDetails()` đăng ký làm fallback framework.

> **500 dùng `ErrorEnvelope`, KHÔNG ProblemDetails** — để MỌI response lỗi (validation/nghiệp vụ **và** 500)
> chung MỘT contract (§3). Đây là hợp đồng lỗi client dựa vào; không tạo error shape thứ hai.

---

## 4. API Versioning
| Chủ đề | Chính sách |
|---|---|
| Major hiện tại | **v1** — mọi route nghiệp vụ dưới tiền tố `/api/v1/...` (nguồn: `GameTeam.Contracts.Common.ApiVersions`) |
| Major | `/api/vN`; breaking change → tăng major |
| Backward-compat | Trong cùng major chỉ thêm (additive), không phá |
| Deprecation | Thông báo + thời gian ân hạn trước khi bỏ version cũ |
| Client-server lệch phiên bản | Server hỗ trợ ≥1 major cũ trong giai đoạn chuyển |

### 4.1 Thay đổi tương thích (compatible — KHÔNG tăng major)
- Thêm endpoint mới.
- Thêm **trường optional** vào response.
- Thêm **giá trị enum mới** (additive) — client phải bỏ qua giá trị lạ một cách an toàn.

### 4.2 Thay đổi phá vỡ (breaking — BẮT BUỘC tăng major)
- Bỏ/đổi tên trường; đổi kiểu dữ liệu; đổi ý nghĩa trường.
- **Đổi giá trị số enum đã có**, hoặc **tái sử dụng số enum đã bỏ**.
- Đổi ngữ nghĩa endpoint không tương thích.

### 4.3 Chính sách ổn định enum (contract-stable)
Enum dùng chung ở `GameTeam.Contracts.Enums` là hợp đồng: **không** đổi/tái dùng giá trị số đã tồn tại;
chỉ **thêm** (additive); giá trị bỏ đi thì **deprecate**, không tái dùng số. Enum serialize dạng **chuỗi**
(canonical name) trong JSON & schema OpenAPI để ổn định cho codegen. Số nền C# (kể cả enum có "khoảng trống"
như `Rarity` = `0,3,4,5`) được đưa vào spec qua **`x-enum-varnames` + `x-enum-values`**
(`ContractEnumsDocumentTransformer`) để codegen client (Phase 08) sinh enum GDScript **giữ đúng số**. Guard:
`EnumStabilityTests` + `OpenApiContractTests` (`server/tests/GameTeam.*.Tests`).

## 4.4 Contract-first & OpenAPI single-source
- **Nguồn sự thật** của contract là C# `GameTeam.Contracts` (DTO/enum, một public type/file).
- OpenAPI spec **sinh từ code** (Microsoft.AspNetCore.OpenApi + Microsoft.Extensions.ApiDescription.Server,
  build-time) ra **`shared/contracts/openapi.json`** — **KHÔNG** sửa tay file này.
- Đổi contract ⇒ rebuild (regenerate) + doc-sync + **regenerate codegen client** (Phase 08).
- CI `ci-server` có bước **OpenAPI drift guard** (`git diff --exit-code` trên `shared/contracts/openapi.json`)
  để chặn spec commit bị lệch với code.
- **Codegen client (Phase 08, đã chốt):** `shared/codegen` sinh model GDScript từ `openapi.json` vào
  **`client/src/data/generated/`** (enum + DTO nền, header `AUTO-GENERATED — DO NOT EDIT`, deterministic). Đổi
  contract ⇒ `bash shared/codegen/run.sh` → commit diff generated (KHÔNG sửa tay). GATE
  `codegen-check.yml` (`git diff --exit-code -- client/src/data/generated`) chặn generated lệch; import Godot
  headless model do `ci-client.yml`. Chi tiết: `shared/codegen/README.md`.

## 4.5 Hiện thực tầng Api (Phase 13 — đã chốt)

`GameTeam.Api` là cổng HTTP chuẩn + composition root. Convention DƯỚI ĐÂY là bắt buộc cho **mọi feature
endpoint** về sau — không tự vẽ convention khác.

- **Versioning:** `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` (8.1.1). `AddApiVersioning`
  (default v1, `AssumeDefaultVersionWhenUnspecified`, `UrlSegmentApiVersionReader`, `ReportApiVersions`) +
  `AddApiExplorer` (`GroupNameFormat="'v'VVV"`, `SubstituteApiVersionInUrl=true` ⇒ OpenAPI render path đã
  resolve `/api/v1/...`). Endpoint mới map vào **version set**: `app.NewApiVersionSet().HasApiVersion(new
  ApiVersion(1))...Build()` + `app.MapGroup("/api/v{version:apiVersion}").WithApiVersionSet(set)` + endpoint
  `.MapToApiVersion(1)`. `GameTeam.Contracts.Common.ApiVersions` vẫn là nguồn hằng số version.
  - *Lưu ý:* mọi stub Phase 05 đã được reimplement vào **version set** (auth/guest 18, profile 19, config 21) —
    **không còn** group literal `/api/v1`. Config: stub `/config/{version}` (param `version` **trùng** token
    `{version:apiVersion}` ⇒ ApiExplorer không substitute được) được **thay** ở Phase 21 bằng hai endpoint dùng query
    param đặt tên `bundleVersion` (không `version`) — xung đột giải quyết tại chỗ, path spec sạch `/api/v1/config/...`.
- **Endpoint mỏng qua MediatR:** HTTP → `ISender.Send(command/query)` → Application handler → `Result` →
  `ApiResults` → HTTP. KHÔNG nhét nghiệp vụ vào endpoint. Mẫu: `GET /api/v1/ping` (`PingCommand`),
  `GET /api/v1/server-time` (`GetServerTimeQuery` + `IClock` — không gọi `DateTime.UtcNow` ở endpoint).
- **`AddApi`** chịu trách nhiệm: JSON enum-as-string, API versioning + ApiExplorer, OpenAPI first-party
  (`AddOpenApi` + `ContractEnumsDocumentTransformer`), `AddExceptionHandler<GlobalExceptionHandler>` +
  `AddProblemDetails`. **KHÔNG** nhồi Application/Infrastructure registration (mỗi tầng có `Add<Layer>()` riêng).
- **Composition root** (`Program.cs`): `AddApplication().AddInfrastructure(config).AddApi()`;
  `UseExceptionHandler()` sớm; `MapOpenApi()`; Swagger UI **dev-only**; `/health` giữ nguyên (không versioned).
  `public partial class Program` cho `WebApplicationFactory<Program>`.
  - `IConfigProvider` được **Phase 21** hiện thực thật (`RuntimeConfigProvider`, thay placeholder `DefaultConfigProvider`):
    phục vụ bundle config bất biến hiện hành; xem `infrastructure.md §3.1`.
- **Swagger:** UI (`Swashbuckle.AspNetCore.SwaggerUI`) **chỉ render** OpenAPI first-party `/openapi/v1.json`
  ở Development — **KHÔNG** dùng SwaggerGen (giữ single-source `shared/contracts/openapi.json` §4.4). Thêm/đổi
  endpoint ⇒ rebuild (regenerate openapi.json) + `bash shared/codegen/run.sh` + kiểm drift.
- **Auth (Phase 18 — đã bật, ADR-008):** `AddApi` đăng ký `AddAuthentication(JwtBearer).AddJwtBearer(...)`
  (validate **chữ ký HS256 + issuer + audience + lifetime**, tham số từ `IOptions<JwtOptions>`) +
  `AddAuthorization` với **`FallbackPolicy = RequireAuthenticatedUser`** ⇒ **mọi endpoint yêu cầu token mặc định**
  (secure-by-default). `Program.cs` bật `UseAuthentication()/UseAuthorization()` (sau routing/swagger, trước map
  endpoint). **Public whitelist tường minh** (`.AllowAnonymous`): `/health`, `POST /api/v1/auth/guest`,
  `/openapi/*`, `/swagger`, và **config** `GET /api/v1/config/current` + `GET /api/v1/config/bundle` (Phase 21 —
  bundle là nội dung chung, không nhạy cảm, client cache theo version; tách khỏi token). Endpoint nghiệp vụ mới
  **mặc định được bảo vệ** — chỉ mở bằng `.AllowAnonymous` khi thật sự công khai. Guest login: `POST /api/v1/auth/guest` (vào version set) → `CreateGuestAccountCommand` →
  JWT (`sub`=account id, `type`=guest, `exp`) + refresh token đục (nền tảng) + `expiresInSeconds`. Lỗi auth trả
  đúng **`ErrorEnvelope`** (401 `UNAUTHENTICATED` / 403 `FORBIDDEN`, qua `AuthProblem` + `ErrorHttpMapping`) —
  KHÔNG body rỗng, KHÔNG shape khác. **Key JWT** từ secret/env `Jwt__SigningKey` (fail-fast, **không** hardcode/
  commit/log; appsettings chỉ chứa issuer/audience/expiry). Provider linking (Google/Apple/email) & refresh nâng
  cao = **Post-MVP**. Phase sau **tái dùng** hạ tầng auth này, không dựng cơ chế mới.
- **Test hợp đồng:** `Api.IntegrationTests` (`WebApplicationFactory`) là hợp đồng HTTP — thêm endpoint ⇒ thêm
  integration test (status, contract, error envelope, versioned route). `ApiTestFactory` swap port
  (no-op UoW/cache, `FixedClock`) để test không cần Postgres/Redis thật.

## 5. DB & Schema Versioning
- DB: EF Core Migrations, additive-first (`infrastructure.md`).
- Profile save: version + migration (ADR-007).
- Config: `schema_version` + bundle version (ADR-005).
- Ba loại version **độc lập**: app version, API version, config version (`../conventions/naming.md`).

## 6. SignalR (optional)
- Chỉ cho realtime cần thiết (thông báo, Post-MVP guild/arena); bật theo feature flag (ADR-006).
- Không đưa core loop MVP phụ thuộc SignalR.

## 7. Liên kết
- ADR-008 (networking), ADR-005 (config), ADR-007 (save)
- Contracts: `shared/contracts` (`../architecture/project-structure.md`)
- Cross-cutting: `cross-cutting.md`
