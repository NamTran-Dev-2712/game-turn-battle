# CI/CD Pipeline (GitHub Actions)

> Pipeline build/test/validate/release trên GitHub Actions. Gate merge theo `../testing/README.md` §4.

---

## 1. Workflows

| Workflow | Trigger | Nội dung |
|---|---|---|
| `ci-server.yml` | PR/push (server/**) | Build .NET, unit+integration, architecture test, golden vector (server) |
| `ci-client.yml` | PR/push (client/**) | Godot headless: build, unit, golden vector (client), smoke |
| `validate-config.yml` | PR/push (config/**, shared/config-schema/**) | Config validator (schema + referential integrity) |
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

## 4. Golden vector cross-phía
- Job chạy sim server + sim client trên **cùng test vector**; so khớp output (ADR-011). Lệch → fail (bảo vệ determinism).

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
| 6 | **Test** | `dotnet test … -c Release --no-build`; test fail ⇒ job đỏ. |
| 7 | **Coverage** | `--collect:"XPlat Code Coverage"` (coverlet.collector) → `coverage/**/coverage.cobertura.xml`. |
| 8 | **Coverage artifact** | `actions/upload-artifact@v4` tên `coverage-server`, `if-no-files-found: error` (không sinh coverage ⇒ job đỏ). |
| 9 | **Architecture gate** | Job **tách riêng** `architecture-test`: chạy `GameTeam.Application.Tests` lọc `FullyQualifiedName~ArchitectureTests` (NetArchTest). Tách khỏi `build-test` để lỗi dependency-rule hiện rõ trong CI graph. |
| 10 | **Domain→Infrastructure** | Rule `Domain_should_not_depend_on_outer_layers` (ADR-003): Domain phụ thuộc Infrastructure ⇒ `architecture-test` **đỏ**. Đã xác minh negative test (thêm leak ⇒ đỏ → revert). |
| 11 | **Path filter** | Xem danh sách `paths` ở trên (backend/shared/build-props kích hoạt; client-only thì không). |
| 12 | **`config-validate`** (hook) | **PLACEHOLDER** — job in ghi chú TODO, chưa validate thật. Bản thật: **Phase 07** (`validate-config.yml`: JSON Schema + referential integrity). |
| 13 | **`golden-vector`** (hook) | **PLACEHOLDER** — job in ghi chú TODO, chưa so khớp vector. Bản thật: **Phase 26** (sim server vs client, ADR-011 — xem §4). |

**Chưa có ở `ci-server.yml` (đích §3, để phase sau):** `publish` + Docker image, lint/format gate,
CodeQL/security (xem §1). Negative test architecture chạy thủ công/local, không phải job thường trực.

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
