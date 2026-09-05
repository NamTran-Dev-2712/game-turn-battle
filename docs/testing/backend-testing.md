# Backend Testing (.NET)

> Unit, integration, architecture test cho backend. Nền: `README.md`. Áp dụng cho Clean Architecture (ADR-003).

---

## 1. Công cụ (đề xuất)
| Mục | Công cụ |
|---|---|
| Test framework | xUnit |
| Assertion | FluentAssertions |
| Mock | NSubstitute / Moq |
| Integration DB | Testcontainers (PostgreSQL/Redis) hoặc DB test riêng |
| Architecture test | NetArchTest |

## 2. Theo tầng

| Tầng | Loại test | Trọng tâm |
|---|---|---|
| Domain | Unit thuần | Invariant, business rule, value object, domain service (vd pity, AFK policy) |
| Application | Unit (mock ports) | Handler đúng luồng; behaviors (validation/idempotency/transaction) |
| Infrastructure | Integration | Repository + EF + PostgreSQL; cache Redis; config provider |
| Api | Integration | Endpoint + auth + serialization (WebApplicationFactory) |

## 3. Test trọng yếu

| Test | Vì sao |
|---|---|
| **Golden combat vector** (server sim) | Khớp client & đặc tả (ADR-011) |
| Gacha rate/pity | Server-authoritative, đúng phân phối & pity |
| AFK claim | Đúng theo server time + cap; **idempotent** (không double) |
| Currency transaction | Atomic; không âm; concurrency an toàn |
| Save migration | Nâng version profile không mất dữ liệu (ADR-007) |
| Authz | Người chơi không thao tác tài nguyên người khác |
| Architecture | Domain không ref Infrastructure; hướng phụ thuộc đúng (`../architecture/dependency-graph.md`) |

## 4. Nguyên tắc
- Domain test **không** chạm DB (thuần, nhanh).
- Integration dùng container/DB thật cho hành vi SQL đúng.
- Idempotency & concurrency có test riêng (chạy song song mô phỏng double-claim).
- Deterministic: inject `IClock`, `IRandomProvider` (seeded) để test tái lập.

### 4.1 Persistence integration test (Phase 11 — đã hiện thực)
- **Testcontainers PostgreSQL** (`postgres:16-alpine`) là **hợp đồng persistence** — KHÔNG mock `DbContext` để thay
  integration test. Fixture `PostgresContainerFixture` (`IAsyncLifetime` + `PostgreSqlBuilder`, host-port ngẫu nhiên).
- Bắt buộc phủ: **CRUD** (repo/UoW ghi→đọc), **transaction rollback** (SaveChanges trong tx rồi rollback ⇒ hàng
  biến mất — chứng minh rollback thực), **domain-event dispatch** (aggregate raise → SaveChanges → handler nhận đúng
  kiểu event, event được clear), **migration up/down** (tạo+seed / revert).
- **Yêu cầu Docker runtime**: CI `ubuntu-latest` có sẵn (không cần cấu hình thêm); local chạy `scripts/dev/up`.
  KHÔNG có cơ chế skip — thiếu Docker ⇒ test đỏ (đúng ý: integration là gate thật). Entity mẫu sống trong assembly
  test (`TestDbContext : AppDbContext`) để giữ schema production sạch. Nguồn: `GameTeam.Infrastructure.Tests/Persistence/`.
- **Architecture test** (Phase 11 thêm): `Application_should_not_depend_on_efcore_or_npgsql` — EF/Npgsql CHỈ ở
  Infrastructure. Chạy cùng bộ NetArchTest trong `GameTeam.Application.Tests`.

### 4.2 Golden vector suite + CI gate (Phase 26 — đã hiện thực)
- **Hợp đồng cross-impl:** `shared/combat-vectors/*.json` là bộ vector dùng CHUNG server (.NET) và client (GDScript).
  Mỗi vector = `input → expected` (baseline). Cùng `(config_version, team_snapshot, stage, seed)` ⇒ hai hiện thực
  phải cho **cùng `event_log` + `result`** (ADR-011). Baseline SINH TỪ SIM SERVER (nguồn chân lý), **không viết tay**.
- **Nguồn baseline = tool `tools/combat-baseline`** (.NET console, ProjectReference `GameTeam.Domain` — dùng ĐÚNG một
  `BattleSimulator`, KHÔNG fork sim thứ hai): `run.sh generate` ghi khối `expected` từ sim server (chuẩn tắc 2-space,
  LF); `run.sh check` regenerate trong bộ nhớ rồi so BYTE với vector đã commit (exit 1 nếu drift). Xem
  `tools/combat-baseline/README.md`.
- **Test server:** `GameTeam.Domain.Tests/Combat/GoldenVectorTests` là `[Theory]`+`[MemberData]` **tự khám phá** mọi
  `*.json` trong thư mục vector (thêm vector = KHÔNG sửa code test), so từng sự kiện + result qua `JsonStructuralComparer`
  (so toàn chuỗi, không nới lỏng). Bộ vector hiện tại phủ: đội khác nhau, đa-unit (turn order + tie-break + đổi target),
  skill/damage, crit, **miss**, VICTORY/DEFEAT/**DRAW**, và biên sát-thương-== / < HP.
- **CI gate `golden-vector`** (`.github/workflows/ci-server.yml`, **BLOCKING**, không `continue-on-error`): chạy
  `tools/combat-baseline/run.sh check` (baseline drift guard) + `dotnet test --filter GoldenVector`. Nửa client chạy
  song song ở `ci-client.yml` (gdUnit4). Cả hai so CÙNG baseline ⇒ **server ≡ client ≡ baseline**; lệch một phía ⇒ CI đỏ.
- **Cập nhật baseline CÓ CHỦ ĐÍCH:** đổi công thức sim → chạy golden (đỏ) → xác nhận đổi là cố ý → `run.sh generate`
  → **review diff** → ghi lý do trong PR → doc-sync → review `combat-determinism`. **KHÔNG regenerate baseline âm thầm**
  để che drift/bug (agent `combat-determinism` + `reviewer` enforce).
- **Negative đã kiểm (Phase 26):** thêm `+1` vào `DamageEffectHandler.ComputeDamage` ⇒ `run.sh check` exit 1 + 9/9
  `GoldenVectorTests` đỏ; revert ⇒ exit 0 + xanh.

## 5. Liên kết
- Strategy: `README.md` · Combat: `../gameplay/combat-framework.md`, ADR-011
- Backend design: `../backend/`
