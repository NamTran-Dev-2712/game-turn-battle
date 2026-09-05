# CI/CD Pipeline (GitHub Actions)

> Pipeline build/test/validate/release trên GitHub Actions. Gate merge theo `../testing/README.md` §4.

---

## 1. Workflows

| Workflow | Trigger | Nội dung |
|---|---|---|
| `ci-server.yml` | PR/push (server/**) | Build .NET, unit+integration, architecture test, golden vector (server) |
| `ci-client.yml` | PR/push (client/**) | Godot headless: build, unit, golden vector (client), smoke |
| `validate-config.yml` | PR/push (config/**, shared/config-schema/**) | Config validator (schema + referential integrity) |
| `codegen-check.yml` | PR/push (openapi.json, shared/codegen/**, client/src/data/generated/**) | Regenerate model client → drift check (`git diff --exit-code`) |
| `release.yml` | Tag `v*` | Build artifact, Docker image, publish config bundle, tạo release |
| `codeql/security` | schedule/PR | Quét bảo mật (Post-bootstrap) |

## 2. Luồng CI cho PR

```mermaid
flowchart LR
    PR[Pull Request] --> Lint[Lint/format]
    Lint --> BuildBE[Build + test server]
    Lint --> BuildFE[Build + test client]
    Lint --> Cfg[Validate config]
    BuildBE --> Golden[Golden vector combat]
    BuildFE --> Golden
    Golden --> Smoke[Smoke]
    Smoke --> Gate{Tất cả xanh?}
    Gate -->|Có| Merge[Cho phép merge]
    Gate -->|Không| Block[Chặn]
```

## 3. Build pipeline

| Phía | Bước |
|---|---|
| Server | restore → build (warning-as-error) → test → publish → Docker image |
| Client | setup Godot (ghim version) → import → test headless → export (release) |
| Config | validate → version bundle → (release) publish lên Config Service |

## 4. Golden vector cross-phía (Phase 26 — đã bật)
- Gate `golden-vector` chạy sim server + sim client trên **cùng bộ vector** `shared/combat-vectors/` (9 kịch bản: basic/crit/
  miss/defeat/draw/multi-unit/mixed-crit/boundary). Baseline (`expected`) **sinh từ sim server** bằng `tools/combat-baseline`
  (nguồn chân lý, ADR-011) — không viết tay.
- **Server:** job `golden-vector` trong `ci-server.yml` = `bash tools/combat-baseline/run.sh check` (baseline drift guard, so byte)
  + `dotnet test --filter GoldenVector` (tự khám phá mọi vector). **Client:** `ci-client.yml` (gdUnit4 `golden_vector_test`,
  trigger thêm `shared/combat-vectors/**`). Cả hai so CÙNG baseline ⇒ **server ≡ client ≡ baseline**; lệch → **fail** (BLOCKING,
  không `continue-on-error`). Negative đã kiểm: `+1` damage một phía ⇒ gate đỏ; revert ⇒ xanh.

## 4b. `ci-server.yml` chi tiết (Phase 02 — hiện trạng thực tế)

> Bảng §1–§3 mô tả pipeline **đích** (target). Mục này mô tả **đúng những gì `ci-server.yml`
> đang làm hôm nay** sau Phase 02. Các gate chưa có bản thật được đánh dấu **PLACEHOLDER**.

**Trigger & path filter.** Chạy trên `push` (`main`, `dev`) và mọi `pull_request` khi đụng:
`server/**`, `shared/**`, `server/Directory.Build.props`, `server/Directory.Packages.props`,
`.github/workflows/ci-server.yml`. Thay đổi chỉ ở client không kích hoạt pipeline này.
Có `concurrency` (huỷ run cũ cùng ref) và `permissions: contents: read` (least privilege).

| # | Nội dung | Chi tiết (job) |
|---|---|---|
| 1 | **SDK** | `actions/setup-dotnet@v4` với `global-json-file: global.json` → SDK **9.0.306** (nguồn sự thật duy nhất). |
| 2 | **Cache NuGet** | `actions/cache@v4`, `path: ~/.nuget/packages`, key = `nuget-<os>-hash(server/Directory.Packages.props)` (CPM, ADR-010; chưa dùng `packages.lock.json`), `restore-keys` fallback theo OS. |
| 3 | **Restore** | `dotnet restore server/GameTeam.sln`. |
| 4 | **Build Release** | `dotnet build … -c Release --no-restore`. |
| 5 | **Warnings-as-error** | Bật ở `server/Directory.Build.props`: compiler `TreatWarningsAsErrors=true` (cảnh báo compiler = lỗi, vỡ build). `CodeAnalysisTreatWarningsAsErrors=false` — cảnh báo analyzer **cố ý** không chặn build ở bootstrap (siết dần). Workflow không override chính sách này. |
| 5b | **OpenAPI drift guard** (Phase 05) | Sau Build (đã regenerate `shared/contracts/openapi.json` từ `GameTeam.Contracts`), chạy `git diff --exit-code -- shared/contracts/openapi.json`. Spec commit lệch code ⇒ job **đỏ** (buộc regenerate + commit). Đảm bảo OpenAPI = single-source, không drift. |
| 6 | **Test** | `dotnet test … -c Release --no-build`; test fail ⇒ job đỏ. |
| 7 | **Coverage** | `--collect:"XPlat Code Coverage"` (coverlet.collector) → `coverage/**/coverage.cobertura.xml`. |
| 8 | **Coverage artifact** | `actions/upload-artifact@v4` tên `coverage-server`, `if-no-files-found: error` (không sinh coverage ⇒ job đỏ). |
| 9 | **Architecture gate** | Job **tách riêng** `architecture-test`: chạy `GameTeam.Application.Tests` lọc `FullyQualifiedName~ArchitectureTests` (NetArchTest). Tách khỏi `build-test` để lỗi dependency-rule hiện rõ trong CI graph. |
| 10 | **Domain→Infrastructure** | Rule `Domain_should_not_depend_on_outer_layers` (ADR-003): Domain phụ thuộc Infrastructure ⇒ `architecture-test` **đỏ**. Đã xác minh negative test (thêm leak ⇒ đỏ → revert). |
| 11 | **Path filter** | Xem danh sách `paths` ở trên (backend/shared/build-props kích hoạt; client-only thì không). |
| 12 | **`config-validate`** (con trỏ) | **MOVED** — GATE thật đã bật ở `validate-config.yml` (**Phase 07**: `tools/config-validator` — JSON Schema + referential integrity + `schema_version`). Job ở `ci-server.yml` chỉ còn là con trỏ. |
| 13 | **`golden-vector`** (gate thật — **Phase 26**) | **ĐÃ BẬT** — job `golden-vector` chạy `bash tools/combat-baseline/run.sh check` (baseline drift guard) + `dotnet test --filter GoldenVector` (server sim == baseline). BLOCKING. Nửa client ở `ci-client.yml` (gdUnit4). Xem §4. |

**Chưa có ở `ci-server.yml` (đích §3, để phase sau):** `publish` + Docker image, lint/format gate,
CodeQL/security (xem §1). Negative test architecture chạy thủ công/local, không phải job thường trực.

## 4c. `ci-client.yml` chi tiết (Phase 03 — hiện trạng thực tế)

> Bảng §1–§3 mô tả pipeline **đích**. Mục này mô tả **đúng những gì `ci-client.yml` làm hôm nay**
> sau Phase 03: dựng cổng nền Godot headless (import + 1 smoke test). Feature/golden test client
> mở rộng ở phase sau.

**Trigger & path filter.** `push` (`main`, `dev`) và `pull_request` khi đụng `client/**`,
**`shared/combat-vectors/**`** (Phase 26 — nửa client của gate golden-vector) hoặc
`.github/workflows/ci-client.yml`. Có `concurrency` (huỷ run cũ cùng ref), `permissions: contents: read`,
`timeout-minutes: 25`.

| # | Nội dung | Chi tiết |
|---|---|---|
| 1 | **Pin Godot** | `env GODOT_VERSION="4.7"`, `GODOT_RELEASE="4.7-stable"` — **phải khớp** `client/project.godot` feature `4.7` (ADR-001, ADR-010). Không dùng `latest`. |
| 2 | **Cache binary** | `actions/cache@v4`, `path: ~/godot-bin`, key `godot-<os>-4.7-stable` → không tải lại mỗi run. |
| 3 | **Download + verify** | Tải binary Linux x86_64 chính thức từ `godotengine/godot-builds` (deterministic URL) + `SHA512-SUMS.txt` cùng release; `sha512sum -c` đúng dòng của binary → **fail cứng nếu hash sai**. |
| 4 | **Version guard** | `godot --headless --version` phải khớp `GODOT_VERSION` (lệch ⇒ job đỏ). |
| 5 | **Import gate** | `godot --headless --import --path client`; **không che exit code** → lỗi import ⇒ job đỏ. |
| 6 | **gdUnit4 addon** | Vendored (commit) tại `client/addons/gdUnit4`, pin **v6.2.0** (yêu cầu Godot ≥ 4.5), enable ở `project.godot`. Plugin tự bỏ qua UI khi headless/`--import`/CLI. |
| 7 | **gdUnit4 tests** | `runtest.sh -a res://tests -rd reports` dưới `xvfb-run` (cả cây `client/tests`: smoke + combat Phase 25 + **golden vector Phase 26** `tests/combat/golden_vector_test.gd` = nửa client của gate `golden-vector`, so client sim với baseline server). |
| 8 | **JUnit** | gdUnit4 ghi `client/reports/report_<n>/results.xml`; `actions/upload-artifact@v4` tên `gdunit4-results`, `if-no-files-found: error`. Test/exit ≠ 0 ⇒ job đỏ. |

**Golden vector client (Phase 26 — đã bật):** `tests/combat/golden_vector_test.gd` tự khám phá mọi vector và so với baseline
**server-generated**; trigger `shared/combat-vectors/**` đảm bảo đổi vector re-chạy client. **Chưa có (để phase sau):**
feature test client thật (nhóm phase 3+), export Android (**Phase 55**).

## 4d. `validate-config.yml` chi tiết (Phase 07 — GATE bắt buộc)

**Trigger & path filter.** `push`/`pull_request` khi đụng `config/**`, `shared/config-schema/**`,
`tools/config-validator/**`, `global.json`, `.github/workflows/validate-config.yml`. Có `concurrency`,
`permissions: contents: read`, `timeout-minutes: 10`.

| # | Nội dung | Chi tiết |
|---|---|---|
| 1 | **JSON parse-check** | Giữ nguyên (bootstrap, nhanh): `python3 json.load` từng file `config/**/*.json` + `shared/config-schema/**/*.json`; JSON hỏng ⇒ `::error` + job đỏ. |
| 2 | **Setup .NET + cache NuGet** | `actions/setup-dotnet@v4` pin theo `global.json` (SDK 9.0.306) + cache `~/.nuget/packages` theo `tools/config-validator/Directory.Packages.props`. |
| 3 | **GATE: config-validator** | Chạy `tools/config-validator/run.sh config shared/config-schema` (bắt buộc). Validate **schema (draft 2020-12) + referential integrity + `schema_version`**; lỗi ⇒ report `file:jsonpath:CODE` + exit ≠ 0 ⇒ job đỏ. `run.sh` phải executable (`100755`); thiếu exec bit ⇒ FAIL (không SKIP). Chi tiết + mã lỗi: `tools/config-validator/README.md`. |

> Bản thật của validator (core lib .NET tái dùng cho Config Service Phase 21) = `tools/config-validator`.
> Job placeholder `config-validate` ở `ci-server.yml` đã chuyển thành con trỏ "MOVED" tới workflow này.

## 4e. `release.yml` chi tiết (Phase 03 — hiện trạng thực tế)

**Trigger.** `push` tag `v*`. `permissions: contents: write` (cần tạo GitHub Release). Xem chi tiết
release/rollback ở `release-operations.md`.

| # | Job / bước | Chi tiết |
|---|---|---|
| 1 | **`server-image`** | `setup-dotnet` theo `global.json`; cache NuGet (key theo `Directory.Packages.props`); `restore → build -c Release → test`; `timeout-minutes: 20`. |
| 2 | **Docker build** | `docker build -f server/Dockerfile -t game-team-api:<tag> .` (build context = **repo root**, khớp Dockerfile). **Không** push registry. |
| 3 | **`create-release`** | `softprops/action-gh-release@v2` với `draft: true` + `generate_release_notes: true` (`needs: server-image`); `timeout-minutes: 10`. |

**Phase 03 cố ý KHÔNG làm (để Phase 55):** push image lên registry, ký/xuất client (Android/iOS),
publish config bundle versioned, tự động publish release (chỉ tạo **draft**). Không có secret/credential
registry ở workflow này.

## 4f. `codegen-check.yml` chi tiết (Phase 08 — GATE bắt buộc)

**Trigger & path filter.** `push`/`pull_request` khi đụng `shared/contracts/openapi.json`, `shared/codegen/**`,
`client/src/data/generated/**`, `global.json`, `.github/workflows/codegen-check.yml`. Có `concurrency`,
`permissions: contents: read`, `timeout-minutes: 10`.

| # | Nội dung | Chi tiết |
|---|---|---|
| 1 | **Setup .NET + cache NuGet** | `actions/setup-dotnet@v4` pin theo `global.json` (SDK 9.0.306) + cache `~/.nuget/packages` theo `shared/codegen/Directory.Packages.props`. |
| 2 | **Regenerate model** | `shared/codegen/run.sh` (build CLI `-c Release` → sinh GDScript vào `client/src/data/generated/`). `run.sh` phải executable (`100755`); thiếu exec bit ⇒ FAIL (không SKIP). |
| 3 | **GATE: drift check** | `git diff --exit-code -- client/src/data/generated`. Generated committed lệch output vừa sinh (stale) ⇒ job **đỏ** (buộc regenerate + commit). |

> Import Godot headless của model generated do `ci-client.yml` đảm nhiệm (`--headless --import` trên `client/**`).
> Nguồn `openapi.json` đã được `ci-server` (OpenAPI drift guard, §5b) bảo đảm khớp `GameTeam.Contracts`.
> Chi tiết + bảng kiểu/giới hạn: `shared/codegen/README.md`.

## 5. Secrets management
| Nguyên tắc | Chi tiết |
|---|---|
| Không commit secret | `.env`/key ngoài Git (`../architecture/project-structure.md`) |
| GitHub Secrets/Environments | Lưu token/connection string; phân theo môi trường |
| Ký ứng dụng | Keystore Android/chứng chỉ iOS lưu secret, không trong repo (`../mvp/10` DP3) |
| Least privilege | Token CI quyền tối thiểu |

## 6. Caching CI
- Cache NuGet, Godot import, để tăng tốc; không cache secret.

## 7. Liên kết
- Testing gate: `../testing/README.md` · Release: `release-operations.md`
- Git: `../conventions/git-conventions.md` · Config: ADR-005
