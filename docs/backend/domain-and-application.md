# Domain & Application Layers

> Thiết kế tầng Domain (lõi business) và Application (CQRS/MediatR). Không hiện thực logic gameplay ở đây — chỉ ranh giới, trách nhiệm, pattern.

---

## 1. Domain Layer

### Thành phần
| Thành phần | Vai trò | Ví dụ (domain game này) |
|---|---|---|
| Entity | Đối tượng có định danh & vòng đời | `Hero`, `PlayerProfile`, `BattleRecord` |
| Aggregate Root | Ranh giới nhất quán, cổng ghi | `PlayerProfile` (chứa inventory, currency) |
| Value Object | Bất biến, so sánh theo giá trị | `Currency`, `PowerRating`, `Seed` |
| Domain Service | Logic không thuộc 1 entity | `PityCalculator`, `AfkAccrualPolicy` |
| Domain Event | Sự việc nghiệp vụ đã xảy ra | `BattleWon`, `HeroAscended` |
| Invariant/Rule | Bất biến bảo vệ trong entity | "team đúng 6 hero" (`../mvp/03`) |

### Nguyên tắc
- **Thuần**: không EF, không HTTP, không `DateTime.Now` (inject `IClock`).
- Bảo vệ invariant trong method, tránh setter công khai bừa bãi.
- Không phụ thuộc Application/Infrastructure.
- Business rule liên quan gameplay tham chiếu `../mvp/03`, `05`, `06` — **không phát minh rule mới**.

### Foundation primitives (Phase 09 — đã đóng)

`GameTeam.Domain/Common/` chứa base type tái dùng (một public type/file, **BCL-only**, `GameTeam.Domain`
**không có package reference** — architecture test canh). Đây là nền cho mọi phase nghiệp vụ; **không** tự phát
minh primitive mới trùng chức năng.

| Primitive | Quy ước |
|---|---|
| `Result` / `Result<T>` | Dùng cho **lỗi nghiệp vụ mong đợi** (thiếu tiền, sai điều kiện…). `IsSuccess`/`IsFailure`/`Error`; `Result<T>.Value` **ném** `InvalidOperationException` nếu truy cập khi thất bại (trạng thái không hợp lệ). **KHÔNG** biến mọi exception thành Result. |
| `Error(Code, Message)` | Value bất biến (so sánh theo giá trị). `Code` **ổn định**, `SCREAMING_SNAKE_CASE`, dùng để map API (phase 13). `Error.None` = trạng thái không lỗi. **Không** rò stack trace / chi tiết DB / hạ tầng / thông tin nhạy cảm. |
| `Entity<TId>` | Equality theo **định danh** (`Id` + kiểu runtime), không theo giá trị thuộc tính. Không annotation persistence, không audit field. |
| `ValueObject` | Equality theo **giá trị** (các thành phần ở `GetEqualityComponents()`); hash nhất quán với equality; an toàn null-component. |
| `AggregateRoot<TId>` | Kế thừa `Entity<TId>`; **sở hữu** domain event. `DomainEvents` (read-only, không ép về `List` để sửa), `RaiseDomainEvent` (protected), `ClearDomainEvents`. |
| `IDomainEvent` | Marker tối giản (không timestamp → tránh wall-clock trong Domain). |
| `IClock` | Port thời gian, `DateTimeOffset UtcNow` — **ranh giới server-time**. Infrastructure hiện thực & inject (phase sau). |
| `Guard` | `NotNull` / `Positive` / `InRange` bảo vệ **bất biến** → **ném** BCL argument exception (chiến lược nhất quán). |

**Result vs Exception (ranh giới bắt buộc):**
- **Result** — lỗi nghiệp vụ *mong đợi*, là một phần hợp đồng của thao tác (handler trả về, caller xử lý).
- **Exception** — lỗi lập trình / vi phạm bất biến nội bộ / lỗi hạ tầng / điều kiện thật sự bất thường. `Guard`
  ném exception vì vi phạm invariant là lỗi lập trình, **không** phải luồng nghiệp vụ. Không tạo **hai paradigm
  validate** song song (Guard không trả `Result`).

**Domain event lifecycle & ranh giới dispatch:**
1. Aggregate **raise** event trong method nghiệp vụ (`RaiseDomainEvent`).
2. Aggregate **thu thập** event (`DomainEvents`).
3. Application/Infrastructure **dispatch** (phase 10/11) rồi gọi `ClearDomainEvents`.
> Domain **chỉ** raise & collect — **KHÔNG** dispatch, **KHÔNG** MediatR/bus/notification trong Domain.

**Server-time rule:** mọi thời gian nghiệp vụ (AFK, energy, cooldown…) lấy qua `IClock.UtcNow`; **cấm** `DateTime.Now/UtcNow`,
`DateTimeOffset.Now/UtcNow` trực tiếp trong Domain (Forbidden Pattern, `../ai/coding-rules.md` §3) — cho test tái lập & chống gian lận giờ.

---

## 2. Application Layer — CQRS + MediatR

### Command vs Query
| Loại | Đặc điểm | Ví dụ |
|---|---|---|
| Command | Đổi trạng thái, trả kết quả tối thiểu | `StartBattleCommand`, `ClaimAfkCommand`, `SummonCommand` |
| Query | Chỉ đọc, không side-effect | `GetHeroListQuery`, `GetProfileQuery` |

### Luồng qua MediatR
```mermaid
flowchart LR
    Api[Controller] --> Send[MediatR.Send]
    Send --> Pipe[Pipeline Behaviors]
    Pipe --> Handler[Command/Query Handler]
    Handler --> Domain[Domain]
    Handler --> Ports[Ports: repo/config/cache]
    Handler --> Events[Publish Domain Events]
```

### Pipeline Behaviors (cross-cutting chuẩn hoá)
| Behavior | Vai trò |
|---|---|
| ValidationBehavior | FluentValidation trước handler |
| LoggingBehavior | Log request/response có cấu trúc |
| TransactionBehavior | Bọc command trong transaction (UnitOfWork) — atomic (ADR-007) |
| CachingBehavior | Cache query đọc nhiều (Redis) |
| IdempotencyBehavior | Chống double-execute cho command nhạy cảm (claim/summon) |

### Ports (interface) — đảo phụ thuộc
`IHeroRepository`, `IPlayerProfileRepository`, `IUnitOfWork`, `IConfigProvider`, `IClock`, `ICacheStore`, `IRandomProvider` (seeded), `ICombatSimulator`... — **Infrastructure implements**.

> **Vị trí port:** khai báo port ở tầng **cần** nó nhất. `IClock` thuộc **`GameTeam.Domain`** (`Common/IClock.cs`, Phase 09)
> vì Domain cần server-time cho invariant/logic mà không được chạm wall-clock; Application/Infrastructure tái dùng cùng
> interface đó. Các repository/UnitOfWork (I/O nghiệp vụ) khai báo ở **Application**. Infrastructure hiện thực tất cả.

---

## 3. Ví dụ trách nhiệm: Start Battle (không phải code)

| Bước | Ai làm |
|---|---|
| Nhận `StartBattleCommand(teamId, stageId)` | Api → MediatR |
| Validate team hợp lệ (6 hero, sở hữu) | ValidationBehavior + Domain rule |
| Lấy snapshot hero + stage config | Handler qua `IConfigProvider`, repo |
| Sinh seed, gọi `ICombatSimulator.Simulate(...)` | Handler (server sim, deterministic — ADR-011) |
| Ghi `BattleRecord` + cấp thưởng | Domain + repo trong TransactionBehavior |
| Publish `BattleWon`/`BattleLost` | Domain Event → cập nhật quest/progression |
| Trả `BattleResult{seed, outcome, rewards, log}` | Handler → Api |

> Logic cân bằng/số liệu nằm ở **config** (ADR-004); handler chỉ điều phối cơ chế.

---

## 4. Domain Events & tách rời
- Handler phát domain event; các reaction (cập nhật quest, leaderboard, telemetry) xử lý qua **notification handler** riêng → tránh handler gọi handler (chống circular, `../architecture/dependency-graph.md`).

## 5. Liên kết
- Infrastructure (impl ports): `infrastructure.md`
- API contract: `api-and-versioning.md`
- Combat sim: `../gameplay/combat-framework.md`, ADR-011
- ADR-003, ADR-004, ADR-007
