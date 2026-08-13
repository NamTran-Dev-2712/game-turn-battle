# `.memory/` — Nhật ký quyết định phạm vi dự án

| Mục | Nội dung |
|---|---|
| **Purpose** | Ghi lại quyết định/ngữ cảnh lâu dài **của dự án** để agent/dev nhớ giữa phiên (khác bộ nhớ cá nhân của công cụ AI). |
| **Responsibilities** | Lưu "vì sao" cho quyết định không thuộc tầm ADR nhưng vẫn cần nhớ. |
| **Allowed** | File `.md` ghi chú, mỗi mục một chủ đề. |
| **Not allowed** | ❌ thay thế ADR (quyết định kiến trúc → `docs/adr/`); ❌ secret. |
| **Dependencies** | [`../docs/adr/`](../docs/adr/), [`../docs/mvp/`](../docs/mvp/). |
| **Owner** | Cả team. |
| **Future expansion** | Chuẩn hoá format; liên kết chéo. |

## Nội dung
- [`README-format.md`](README-format.md) — format một mục nhật ký quyết định.
- [`0001-ai-execution-layer.md`](0001-ai-execution-layer.md) — quyết định tách execution layer khỏi docs SSOT.
- [`0002-dev-environment-standardized.md`](0002-dev-environment-standardized.md) — dev env một lệnh (Phase 04): compose Postgres16/Redis7, network `game-team-dev`, profile `api`, `.env`, script up/down đa nền tảng.
- [`0003-shared-contracts-standardized.md`](0003-shared-contracts-standardized.md) — contract spine (Phase 05): `GameTeam.Contracts` là nguồn (enum + DTO nền), OpenAPI sinh từ code ra `shared/contracts/openapi.json` (single-source, CI drift guard), enum ổn định additive-only, `/api/v1`.
- [`0004-config-schema-standardized.md`](0004-config-schema-standardized.md) — config schema (Phase 06): 8 schema per-type + `common.schema.json` + envelope ở `shared/config-schema/` (draft 2020-12, `snake_case`, combat integer, `schema_version`, ID prefix), fixture pass/fail + `_versions/` migration; schema là cấu trúc **không** balance; referential integrity = phase 07.
- [`0005-config-validator-standardized.md`](0005-config-validator-standardized.md) — config validator (Phase 07): `tools/config-validator` (.NET 9, `JsonSchema.Net`) = core lib tái dùng + CLI mỏng + xUnit; kiểm schema + referential integrity (REF001/REF002) + `schema_version` (VER001/VER002); report `file:jsonpath:CODE`; GATE bắt buộc ở `validate-config.yml`; Config Service (phase 21) project-reference core.
- [`0006-codegen-pipeline-standardized.md`](0006-codegen-pipeline-standardized.md) — codegen client (Phase 08): `shared/codegen` (.NET 9, không gói ngoài) sinh GDScript vào `client/src/data/generated/` từ `shared/contracts/openapi.json` (enum giữ số C# qua `x-enum-values`, DTO `Resource`, header DO-NOT-EDIT, deterministic); GATE `codegen-check.yml` (regenerate → `git diff --exit-code`); Godot import sạch; parse = phase 15.
- [`0007-domain-foundation-standardized.md`](0007-domain-foundation-standardized.md) — domain foundation (Phase 09): primitive tái dùng ở `GameTeam.Domain/Common/` (BCL-only, Domain package-free) — `Result`/`Result<T>`, `Error`, `Entity<TId>`, `ValueObject`, `AggregateRoot<TId>` (raise/collect domain event, **không** dispatch), `IDomainEvent`, `IClock` (`DateTimeOffset UtcNow`, ranh giới server-time), `Guard` (**ném** BCL argument exception). Result=lỗi nghiệp vụ mong đợi, exception=lỗi lập trình/hạ tầng; NetArchTest `Domain_should_not_depend_on_framework_packages`; dispatch = phase 10/11.

> Quyết định kiến trúc **luôn** đi vào `docs/adr/`, không chỉ ở đây.
