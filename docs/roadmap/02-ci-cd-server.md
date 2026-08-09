# 02 — CI/CD server hardening

> Mục đích: Đưa pipeline CI cho backend `.NET 9` lên mức **gác cổng thật** (build warnings-as-error, test, architecture test), làm nền tin cậy cho mọi phase backend.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 0 Nền tảng & Chuẩn hoá | P0 | S0 | hạ tầng |

# Mục tiêu

Hoàn thiện `.github/workflows/ci-server.yml`: restore → build `-c Release` (warnings-as-error) → `dotnet test`, cộng **job architecture test** (NetArchTest) tách bạch, path-filtered cho `server/**` và `shared/**`.

# Lý do

Bootstrap đã có ci-server chạy build+test, nhưng cần chuẩn hoá cache, tách job architecture-guard rõ ràng, và cố định phiên bản SDK theo `global.json` để mọi PR backend được kiểm nhất quán trước khi code nghiệp vụ đổ vào (từ nhóm 2).

# Phụ thuộc

- **Trước:** 01 (layout & conventions).
- **Sau:** mọi phase backend (09–13, 18–21, 24, 27–55) dựa vào cổng CI này.

# Phạm vi

- `ci-server.yml`: setup-dotnet từ `global.json`; restore (CPM); build Release; test có coverage collector.
- Job/step architecture test (NetArchTest) báo đỏ khi Domain→Infrastructure leak.
- Cache NuGet; path filter `server/**`, `shared/**`, `Directory.*.props`.
- Đặt nền cho gate golden-vector & config-validate (khai báo placeholder job, bật ở phase 26/07).

# Không thuộc phạm vi

- Client/Godot CI (phase 03).
- Config validator thực thi (phase 07) — chỉ chừa hook.
- Golden vector combat (phase 26) — chỉ chừa hook.
- Registry push / deploy (phase 55).

# Deliverables

- `ci-server.yml` xanh trên PR mẫu, có job build+test+architecture.
- Báo cáo coverage cơ bản (coverlet) xuất ra artifact.
- Tài liệu ngắn trong [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md) mô tả các job & gate.

# Công việc cần thực hiện

- [x] Cố định `actions/setup-dotnet` đọc version từ `global.json` (SDK 9.0.306).
- [x] Thêm cache NuGet theo `packages.lock`/`Directory.Packages.props`.
- [x] Step `dotnet restore` → `dotnet build -c Release` (đảm bảo `TreatWarningsAsErrors` compiler bật).
- [x] Step `dotnet test -c Release --collect` (coverlet) → upload artifact coverage.
- [x] Tách/đảm bảo test architecture (NetArchTest trong `GameTeam.Application.Tests`) chạy và fail-fast khi vi phạm dependency rule (ADR-003).
- [x] Path filter cho `server/**`, `shared/**`, `Directory.Build.props`, `Directory.Packages.props`.
- [x] Khai báo placeholder job `config-validate` và `golden-vector` (skip có ghi chú TODO trỏ phase 07/26).
- [x] Cập nhật `../deployment/ci-cd-pipeline.md` mô tả pipeline.

# Tiêu chí hoàn thành

- PR mẫu chạy CI xanh; build Release không warning; 6 test hiện có pass.
- Cố ý tạo leak Domain→Infra ⇒ job architecture **fail** (đã thử nghiệm rồi revert).
- Coverage artifact tồn tại.
- SDK version khớp `global.json`; cache hoạt động (lần 2 nhanh hơn).

# Cách kiểm tra

- Mở PR nháp đụng `server/**` → xem Actions xanh.
- Local: `dotnet build -c Release` + `dotnet test` khớp CI.
- Thử nghiệm negative: thêm `using` Infra vào Domain → CI đỏ ở architecture job → revert.

# Rủi ro

- **Warnings-as-error làm vỡ build khi thêm analyzer** → giữ `CodeAnalysisTreatWarningsAsErrors=false` ở bootstrap (đã set trong `Directory.Build.props`); siết dần.
- **Cache stale** → key theo hash `Directory.Packages.props`.
- Path filter loại nhầm shared → include `shared/**`.

# Ghi chú

Bám [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md) và ADR-010 (CPM). Branch protection/environments phải bật thủ công trên GitHub (ghi ở [`../audit/bootstrap-audit.md`](../audit/bootstrap-audit.md) §4).

# Technical Debt Review

- **Maintainability:** cache & path-filter giảm thời gian CI, dễ mở rộng job.
- **Scalability:** job tách bạch cho phép thêm gate (golden/config) không rối.
- **Testing:** cổng test + architecture là xương sống chất lượng backend.
- **Security:** không in secret ra log; dùng `secrets.*` khi cần.
- **Nợ:** golden-vector & config-validate còn placeholder (đóng ở 26/07).

# Phase Review

Đóng khi CI server xanh với build/test/architecture, có coverage, negative test architecture xác nhận cổng hoạt động.

**Bằng chứng xác minh (local, Phase 02):**
- SDK: `dotnet --version` → `9.0.306` (khớp `global.json`).
- Build Release: `0 Warning(s), 0 Error(s)`.
- Test: **6/6 pass** (Domain 1, Application 3, Infrastructure 1, Api 1); coverage `coverage.cobertura.xml` sinh ra (4 file, coverlet).
- Architecture (positive): 2 test `ArchitectureTests` pass qua filter riêng.
- Architecture (**negative**): tạo type Domain phụ thuộc namespace `GameTeam.Infrastructure` ⇒ `Domain_should_not_depend_on_outer_layers` **FAIL** (exit 1) ⇒ đã **revert** ⇒ 6/6 pass lại. Cổng hoạt động.
- Còn lại (chưa chứng minh local): "PR mẫu chạy CI xanh" + cache nhanh hơn lần 2 — cần push/PR trên GitHub Actions (chờ phê duyệt push).
- Placeholder `config-validate` (→ phase 07) & `golden-vector` (→ phase 26) chỉ là hook, chưa thực thi.

---

## Liên kết
- [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md) · [`../testing/backend-testing.md`](../testing/backend-testing.md)
- ADR: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md) · [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Roadmap: [`README.md`](README.md) → kế: [`03-ci-cd-client-config-release.md`](03-ci-cd-client-config-release.md)
