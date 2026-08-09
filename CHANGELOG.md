# Changelog

Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/); phiên bản theo [SemVer](https://semver.org/lang/vi/).

## [Unreleased]

### Added
- **Roadmap Phase 01 — Cấu trúc repo & thực thi conventions:** chốt cổng governance.
  - `docs/conventions/enforcement-map.md` — bảng "convention → nơi enforce" + trạng thái (active/deferred).
  - README index cho `docs/architecture/` và `docs/mvp/` (index điều hướng, không sửa SSOT).
  - `project-structure.md` §9: ghi nhận thư mục bổ sung ngoài §2 (AI execution layer, `.githooks/`, `.vscode/`, `design/`) → layout doc tự chứa 100%.

### Changed
- Thêm mục "Liên kết" cho 7 README khu vực docs (adr, audit, backend, conventions, gameplay, godot, liveops); `docs/audit/README.md` mở đầu bằng blockquote.

### Fixed
- `README.md` gốc thiếu newline cuối file (vi phạm `insert_final_newline` của `.editorconfig`).

### Verified
- `dotnet format server/GameTeam.sln --verify-no-changes` xanh; hiệu lực pre-commit/git hook đã kiểm; 671 link nội bộ docs resolve (0 gãy); cây repo khớp 100% `project-structure.md`.

- **Project Bootstrap (P0):** dựng khung repo production-ready.
  - Di chuyển project Godot vào `client/` + cây thư mục feature-based (README mỗi thư mục).
  - Solution .NET 9 skeleton compile được: 5 project `src` + 4 project test, CPM, DI stub, `/health` (build & test xanh, chưa có nghiệp vụ).
  - `shared/` (contracts, config-schema, codegen), `config/` (data-driven, theo `docs/mvp`).
  - `tools/`, `scripts/`, `deploy/` (docker-compose Postgres+Redis+API).
  - `.github/` (workflows CI/CD skeleton, issue/PR template, CODEOWNERS, dependabot).
  - Cấu hình dev: `.editorconfig`, `.gitattributes`, `.gitignore`, `global.json`, `.env.example`, `.vscode/`, git hooks.
  - Tài liệu gốc (README, CONTRIBUTING, SECURITY, …) trỏ vào `docs/`.

> Kiến trúc & SSOT (docs/) được tạo ở phase trước; xem `docs/README.md`.

[Unreleased]: https://github.com/
