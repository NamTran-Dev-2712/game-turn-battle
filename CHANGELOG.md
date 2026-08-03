# Changelog

Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/); phiên bản theo [SemVer](https://semver.org/lang/vi/).

## [Unreleased]

### Added
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
