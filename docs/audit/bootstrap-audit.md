# Bootstrap Audit — Project Bootstrap (P0)

> Kiểm toán khi kết thúc phase **dựng khung repository**. Xác nhận đã tạo gì, đối chiếu prompt ↔ SSOT, và **các khoảng trống còn lại trước khi hiện thực**. Không có gameplay/business logic nào được viết.

Ngày: khi hoàn tất P0 · Nhánh: `feature/1-feature-base-structure-project`.

---

## 1. Đã tạo (tóm tắt)

| Khu vực | Kết quả |
|---|---|
| **client/** | Godot project **di chuyển từ root** (git rename, giữ history) + cây feature-based; README mọi thư mục; `export_presets.cfg` placeholder. |
| **server/** | Solution .NET 9 **compile được**: 5 project `src` + 4 project test; CPM (`Directory.Packages.props`); `Directory.Build.props`; `Dockerfile`; DI stub mỗi tầng; `/health`. |
| **shared/** | `contracts/`, `config-schema/` (+ `config-bundle.schema.json` mẫu), `codegen/`. **Phase 05 (đã đóng):** `contracts/openapi.json` **sinh từ `GameTeam.Contracts`** (6 enum dùng chung + DTO nền + `ErrorEnvelope`; 4 path `/api/v1` stub 501 + `/health`); OpenAPI single-source (.NET 9 first-party, build-time) + CI drift guard. |
| **config/** | 10 thư mục data-driven (README trỏ `docs/mvp/*`); chưa author giá trị. |
| **tools/scripts/deploy/** | README + stub; `deploy/compose/docker-compose.yml` (Postgres+Redis+API). **Phase 04 (đã đóng):** dev env **một lệnh** — network `game-team-dev`, healthcheck xanh, `scripts/dev/up|down.{ps1,sh}` idempotent (cờ `-Api`/`--api`, `-Volumes`/`-v`), profile `api` → `/health`=200; xác minh chạy thật (PowerShell + bash) với Docker 28.5.1. |
| **.github/** | 4 workflow skeleton, issue/PR/discussion template, CODEOWNERS, dependabot, labels/project docs. |
| **Cấu hình dev** | `.editorconfig`, `.gitattributes`, `.gitignore`, `global.json` (pin SDK 9), `.env.example`, `.vscode/`, `.githooks/` + `.pre-commit-config.yaml`. |
| **Docs gốc** | 15 file (README, CONTRIBUTING, CODE_OF_CONDUCT, CHANGELOG, ROADMAP, SECURITY, SUPPORT, ARCHITECTURE, DECISIONS, AI_GUIDE, DEVELOPMENT_GUIDE, SETUP, DEPLOYMENT, TESTING, STYLE_GUIDE) — **thin, trỏ vào `docs/`**. |
| **AI scaffolding** | `.claude .instructions .prompts .agents .context .rules .templates .tasks .memory` — README mỗi thư mục. **Đã dựng execution layer (post-P0, 2026-08-03):** `CLAUDE.md` (auto-load) + `.claude/{settings.json,agents,workflows,checklists}`, thư viện `.prompts/.templates/.context`, lớp mỏng `.rules/.instructions/.memory/.tasks/.agents`, và chính sách doc-sync bắt buộc — tất cả **trỏ** `docs/`, không sao chép. |

**Kiểm chứng build/test:** `dotnet build` = 0 error; `dotnet test` = **6 passed / 0 failed** (Domain 1, Infrastructure 1, Application 3 gồm 2 architecture test NetArchTest, Api integration 1). `.cs` chỉ gồm 12 file skeleton (marker/DI/Program/test) — **không có logic nghiệp vụ**.

---

## 2. Đối chiếu Prompt ↔ SSOT (deviations)

Quy tắc đã theo: **SSOT (`docs/`) thắng** khi prompt mâu thuẫn (owner đã xác nhận).

| Prompt yêu cầu | Đã làm | Lý do |
|---|---|---|
| `/backend` | Dùng **`server/`** | SSOT `project-structure.md` §2. |
| `/github` | Dùng **`.github/`** | Chuẩn GitHub + SSOT. |
| `/docker` (root) | Dùng **`deploy/docker/`** + `deploy/compose/` | SSOT §7. |
| `/temp` | Dùng **`tmp/`** | SSOT §2. |
| `/tests` (root) | Test **theo phía** (`client/tests`, `server/tests`) | SSOT §3/§4. |
| `/design` | **Đã thêm** `design/` (mới, ngoài SSOT) | Prompt yêu cầu; ghi nhận là bổ sung, không thay SSOT. |
| AI dirs (`.claude`…) | **Đã thêm** (mới, ngoài SSOT) | Prompt yêu cầu; **bổ sung** cho `docs/ai/`, không thay thế. |
| `LICENSE (placeholder)` | **Giữ nguyên** LICENSE (MIT, © đã có) | Không ghi đè lựa chọn sẵn có của owner. |
| README `.github/ISSUE_TEMPLATE`/`DISCUSSION_TEMPLATE` | **Cố ý không thêm** | Markdown thừa trong 2 thư mục này tạo template "ma" trên GitHub. Đã tài liệu ở `.github/README.md`. |

> Không có quyết định kiến trúc nào bị đổi. Không có nghiệp vụ nào trong `docs/mvp/` bị sửa.

---

## 3. TODO đã hoãn có chủ đích (không thuộc bootstrap)

| Hạng mục | Nơi | Phase |
|---|---|---|
| `tools/config-validator` (JSON Schema + referential integrity) — hiện `validate-config.yml` chỉ check cú pháp JSON | `tools/config-validator` | Core Framework (P1) |
| `tools/codegen` (contracts → client model) | `tools/codegen` | P1 |
| CI client Godot headless (import/test/export) + gdUnit4 | `.github/workflows/ci-client.yml` | P1 |
| Golden vector combat (client == server) | `docs/testing`, sim 2 phía | Gameplay (P2) |
| DI thật: EF/Npgsql DbContext, Redis, JWT, Configuration Service, jobs | `GameTeam.Infrastructure` | P1 |
| Pipeline behaviors (Validation/Logging/Transaction/Caching/Idempotency) | `GameTeam.Application/Common/Behaviors` | P1 |
| API versioning, controllers, Swagger, error contract, health checks đầy đủ | `GameTeam.Api` | P1 |
| CodeQL/security scan | `.github/workflows` | Post-bootstrap |
| Push container registry, client export artifact, publish config bundle | `release.yml` | Release |
| Nội dung `config/*` (giá trị gameplay) | `config/` | Gameplay (P2–P3) |
| `content-importer` (bảng → JSON) | `tools/content-importer` | Post-MVP |

---

## 4. Khoảng trống / rủi ro cần chú ý

1. **Placeholder cần thay:** `@game-team/*` trong `CODEOWNERS`, URL trong template/README/CHANGELOG, kênh liên hệ trong `SECURITY.md`/`SUPPORT.md`, `godotTools.editorPath` trong `.vscode/settings.json` — điền khi khởi tạo tổ chức repo.
2. **Godot version:** `project.godot` khai báo features `4.7`; `ci-client.yml` đặt `GODOT_VERSION=4.7`. Giữ đồng bộ khi nâng cấp.
3. **Branch protection & Environments:** cần bật trên GitHub (xem `.github/project-setup.md`) — không tự động hoá được từ repo.
4. **Git LFS:** asset nặng (`assets/`, `client/assets/`) nên cân nhắc LFS trước khi commit binary lớn (mẫu đã có trong `.gitattributes`).
5. **`.env`:** chỉ dùng dev; secret thật phải ở GitHub Environments.
6. **NetArchTest:** hiện chỉ 2 luật (Domain thuần; Application ⊥ Infrastructure). Mở rộng bộ luật khi thêm tầng/feature.

---

## 5. Sẵn sàng cho phase kế
Khung repo đạt tiêu chí: modular, feature-based, compile/test xanh, tài liệu hoá mọi thư mục, CI skeleton, tách generated. **Bước tiếp: P1 — Core Framework** theo [`../roadmap/README.md`](../roadmap/README.md) và [`../architecture/implementation-order.md`](../architecture/implementation-order.md). Mọi task hiện thực bắt đầu từ [`../ai/context-strategy.md`](../ai/context-strategy.md).
