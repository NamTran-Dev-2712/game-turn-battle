# Project Structure (Cấu trúc repository)

> Layout repo đầy đủ cho monorepo (client Godot + backend .NET + tài liệu + hạ tầng). Mỗi thư mục nêu **WHY**. Quy ước đặt tên chi tiết ở `../conventions/naming.md`.

---

## 1. Nguyên tắc bố cục

| Nguyên tắc | Diễn giải |
|---|---|
| Monorepo | Client + backend + docs + infra cùng repo → versioning đồng bộ, dễ cho AI agent điều hướng |
| Tách rõ 2 phía | `client/` và `server/` độc lập build/test |
| Nguồn sự thật dùng chung | `shared/` chứa hợp đồng/config schema dùng cả hai phía |
| Feature-based | Trong mỗi phía, chia theo feature không theo "loại file" |
| Generated ≠ source | Thư mục sinh tự động tách khỏi source, gitignore |

---

## 2. Cây thư mục gốc

```text
game-team/
├── client/                  # Godot 4.x project (GDScript)
├── server/                  # .NET 9 solution (Clean Architecture)
├── shared/                  # Hợp đồng & config schema dùng chung 2 phía
├── config/                  # Dữ liệu data-driven (gameplay/liveops config)
├── tools/                   # Công cụ nội bộ (config validator, codegen, importer)
├── scripts/                 # Script tự động hoá (build/dev/db)
├── deploy/                  # Docker, compose, IaC, k8s (tương lai)
├── .github/                 # GitHub Actions workflows, templates
├── docs/                    # Toàn bộ tài liệu (SSOT + blueprint)
├── assets/                  # (tuỳ chọn) nguồn asset thô trước import
├── localization/            # Nguồn bản dịch (source of truth i18n)
├── build/                   # [generated] output build (gitignored)
├── third_party/             # Thư viện/asset bên thứ ba (license rõ ràng)
├── tmp/                     # [generated] tạm thời (gitignored)
├── .gitignore
├── .gitattributes
├── .editorconfig
└── README.md
```

| Thư mục gốc | WHY tồn tại |
|---|---|
| `client/` | Cô lập project Godot; mở thẳng bằng Godot editor |
| `server/` | Cô lập solution .NET; build/test/deploy riêng |
| `shared/` | Tránh lệch hợp đồng client-server; nguồn duy nhất cho DTO/enum/config schema |
| `config/` | Data-driven content (ADR-004/005) tách khỏi code, tune không cần build |
| `tools/` | Công cụ hỗ trợ dev (validate config, sinh mã), không vào runtime |
| `scripts/` | Tự động hoá thao tác lặp (giảm lỗi con người, hợp AI) |
| `deploy/` | Hạ tầng như mã (IaC), Docker — tái lập môi trường |
| `.github/` | CI/CD, PR template, chuẩn hoá quy trình |
| `docs/` | Blueprint & SSOT — "bộ não" dự án cho người & AI |
| `assets/` | Asset thô (psd, wav gốc) tách khỏi asset đã import |
| `localization/` | i18n nguồn (khoá dịch) — chuẩn bị đa ngôn ngữ (`docs/mvp/10` UX4) |
| `build/` | Output tạm, không commit |
| `third_party/` | Cách ly mã bên ngoài + theo dõi license |
| `tmp/` | Rác tạm, gitignored |

---

## 3. `client/` — Godot project (feature-based)

```text
client/
├── project.godot
├── addons/                  # Plugin Godot (editor tools, gdUnit...)
├── src/
│   ├── core/                # Autoload services: net, config cache, event bus, scene router, save cache
│   │   ├── net/
│   │   ├── config/
│   │   ├── events/
│   │   ├── state/
│   │   └── scene/
│   ├── features/            # Mỗi feature 1 thư mục (scene+script+resource cùng chỗ)
│   │   ├── hero/
│   │   ├── summon/
│   │   ├── battle/
│   │   ├── campaign/
│   │   ├── inventory/
│   │   ├── equipment/
│   │   ├── quest/
│   │   ├── mail/
│   │   ├── shop/
│   │   └── formation/
│   ├── combat/              # Deterministic sim (thuần, không UI) — bản client của ruleset
│   ├── ui/                  # UI layer dùng chung: theme, widget tái sử dụng, layout landscape
│   ├── data/                # Resource models (.gd class + .tres) map từ config schema
│   └── shared/              # Tiện ích chung client (math fixed-point, result types)
├── assets/                  # Asset đã import dùng trong game (art/audio/fx/font)
│   ├── art/
│   ├── audio/
│   ├── vfx/
│   └── fonts/
├── localization/            # File .csv/.po dùng runtime (sinh từ /localization gốc)
├── tests/                   # Test Godot (gdUnit/GUT)
└── export_presets.cfg
```

| Thư mục client | WHY |
|---|---|
| `src/core/` | Dịch vụ nền dạng autoload — tối giản, mỗi cái 1 việc (không God autoload), ADR-002 |
| `src/features/` | **Feature-based**: mỗi feature tự chứa scene+script+resource → dễ thêm/xoá, hợp AI context |
| `src/combat/` | Bộ sim deterministic tách khỏi UI để **đồng nhất với server** & test được (ADR-011) |
| `src/ui/` | UI tái sử dụng, theme landscape (`docs/mvp/00`) |
| `src/data/` | Resource làm "khuôn" cho config data-driven (ADR-004) |
| `addons/` | Plugin & editor tool (`docs/godot/tooling-and-testing.md`) |
| `tests/` | Test client (`docs/testing/godot-testing.md`) |

---

## 4. `server/` — .NET 9 solution (Clean Architecture)

```text
server/
├── GameTeam.sln
├── src/
│   ├── GameTeam.Domain/            # Domain layer (không phụ thuộc gì)
│   ├── GameTeam.Application/       # CQRS/MediatR, ports, validators, behaviors
│   ├── GameTeam.Infrastructure/    # EF Core, repos, Redis, JWT, config service, jobs
│   ├── GameTeam.Api/               # Presentation: controllers/minimal API, SignalR hubs
│   └── GameTeam.Contracts/         # DTO/request/response (có thể sinh vào shared)
├── tests/
│   ├── GameTeam.Domain.Tests/
│   ├── GameTeam.Application.Tests/
│   ├── GameTeam.Infrastructure.Tests/
│   └── GameTeam.Api.IntegrationTests/
├── Directory.Build.props           # Thiết lập chung (nullable, analyzers, version)
├── Directory.Packages.props        # Central Package Management (ADR-010)
└── Dockerfile
```

| Project | Tầng | WHY |
|---|---|---|
| `Domain` | Domain | Entity/aggregate/rule thuần, trung tâm sạch (ADR-003) |
| `Application` | Application | CQRS + MediatR handler, port interface, pipeline behavior |
| `Infrastructure` | Infrastructure | EF Core/PostgreSQL, Redis, JWT, Configuration Service, background jobs |
| `Api` | Presentation | Endpoint, auth, DTO mapping, SignalR (optional) |
| `Contracts` | (chia sẻ) | DTO versioned; nguồn để sinh model client (`shared/`) |
| `tests/*` | — | Test theo tầng (`docs/testing/backend-testing.md`) |

---

## 5. `shared/` — hợp đồng dùng chung

```text
shared/
├── contracts/           # Đặc tả API (OpenAPI) + enum/hằng dùng chung
├── config-schema/       # JSON Schema cho mọi file config data-driven
└── codegen/             # Output/định nghĩa sinh mã (client model từ contracts)
```

| Thư mục | WHY |
|---|---|
| `contracts/` | Một nguồn hợp đồng → client & server không lệch (ADR-008) |
| `config-schema/` | JSON Schema validate mọi config (ADR-005) — chống config sai khi live |
| `codegen/` | Sinh model/DTO cho client từ contracts → giảm lệch tay |

---

## 6. `config/` — dữ liệu data-driven

```text
config/
├── heroes/              # Định nghĩa hero (stats/faction/class/element/role/skill ref)
├── skills/              # Định nghĩa skill/effect
├── stages/              # Campaign/tower stage
├── gacha/               # Banner + rate + pity
├── shop/                # Shop items
├── rewards/             # Reward tables (AFK, quest, first-clear)
├── economy/             # Hệ số kinh tế (đường cong cost, energy...)
├── quests/              # Định nghĩa quest
├── liveops/             # Event/season/feature-flag (schedule) — Post-MVP
└── _versions/           # Metadata phiên bản config (schema versioning, ADR-005)
```

| Thư mục | WHY | Nguồn MVP |
|---|---|---|
| `heroes/`,`skills/` | Hero/skill data-driven | `docs/mvp/03`, `05` |
| `stages/` | Content pipeline campaign/tower | `docs/mvp/03` |
| `gacha/` | Banner/rate/pity config được | `docs/mvp/03`,`06` |
| `shop/`,`rewards/`,`economy/` | Kinh tế tune được (không hardcode) | `docs/mvp/06` |
| `liveops/` | Chừa chỗ cho LiveOps | `docs/mvp/07` |
| `_versions/` | Versioning an toàn khi cập nhật | `docs/mvp/08` TE4, ADR-005 |

> **Runtime nguồn sự thật của config là backend (Configuration Service, ADR-005).** Thư mục `config/` là nguồn author-time; pipeline nạp/validate/publish lên backend. Client nhận config đã versioned từ backend và **cache**.

---

## 7. `tools/`, `scripts/`, `deploy/`, `.github/`

```text
tools/
├── config-validator/    # Validate config theo JSON Schema (chạy CI)
├── codegen/             # Sinh DTO/model từ contracts
└── content-importer/    # Import bảng (csv/xlsx) -> config json (Post-MVP)

scripts/
├── dev/                 # chạy local (client, server, db)
├── db/                  # migration, seed
└── ci/                  # helper cho pipeline

deploy/
├── docker/              # Dockerfile phụ, compose local
├── compose/             # docker-compose (postgres+redis+api)
└── k8s/                 # (tương lai) manifest

.github/
├── workflows/           # ci-client.yml, ci-server.yml, validate-config.yml, release.yml
├── ISSUE_TEMPLATE/
└── pull_request_template.md
```

| Thư mục | WHY |
|---|---|
| `tools/config-validator` | Chặn config sai từ CI (ADR-005, `docs/testing`) |
| `tools/codegen` | Đồng bộ hợp đồng client-server tự động |
| `scripts/` | Chuẩn hoá thao tác → giảm lỗi, hợp AI (`docs/ai`) |
| `deploy/` | Tái lập môi trường (`docs/deployment`) |
| `.github/workflows` | CI/CD (`docs/deployment/ci-cd-pipeline.md`) |

---

## 8. `docs/` — cấu trúc tài liệu

```text
docs/
├── README.md            # Master index
├── mvp/                 # SSOT (Product Discovery) — KHÔNG sửa ở phase này
├── architecture/        # Tài liệu này
├── adr/                 # Architecture Decision Records
├── conventions/         # Chuẩn code/đặt tên/git
├── backend/             # Thiết kế backend
├── godot/               # Thiết kế client
├── gameplay/            # Ranh giới module gameplay
├── liveops/             # Thiết kế LiveOps
├── testing/             # Chiến lược test
├── deployment/          # CI/CD & vận hành
├── roadmap/             # Phase giao hàng
└── ai/                  # Quy tắc cộng tác AI
```

---

## 9. Thư mục generated & gitignore (nguyên tắc)

| Loại | Ví dụ | Chính sách |
|---|---|---|
| Generated | `client/.godot/`, `build/`, `tmp/`, `server/**/bin,obj`, `shared/codegen/output` | Gitignore, sinh lại được |
| Third party | `third_party/`, `client/addons/<plugin>` | Commit + ghi license |
| Editor | `client/.godot/editor`, `.vs/`, `.idea/` | Gitignore |
| Secrets | `.env`, key | **Không commit** (`docs/deployment`) |

---

## 10. Liên kết
- Đồ thị phụ thuộc: `dependency-graph.md`
- Đặt tên chi tiết: `../conventions/naming.md`
- Backend layout: `../backend/solution-structure.md`
- Client layout: `../godot/scene-architecture.md`
- Config strategy: `../adr/ADR-005-configuration-strategy.md`
