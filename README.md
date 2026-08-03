# 🎮 Game Team — 2D Idle Squad RPG

> Monorepo cho game **mobile 2D Idle Squad RPG / Hero Collection** (live-service).
> **Client:** Godot 4.x (GDScript) · **Backend:** .NET 9 (Clean Architecture, CQRS, EF Core, PostgreSQL, Redis) · **Nền tảng:** Android → iOS (landscape).

[![ci-server](https://img.shields.io/badge/ci--server-skeleton-lightgrey)](.github/workflows/ci-server.yml)
[![ci-client](https://img.shields.io/badge/ci--client-skeleton-lightgrey)](.github/workflows/ci-client.yml)
[![license](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## Bản đồ repository

| Thư mục | Nội dung | Tài liệu |
|---|---|---|
| [`client/`](client/) | Project Godot 4.x (client) | [docs/godot](docs/godot/) |
| [`server/`](server/) | Solution .NET 9 (backend) | [docs/backend](docs/backend/) |
| [`shared/`](shared/) | Hợp đồng API + JSON Schema config | [structure §5](docs/architecture/project-structure.md) |
| [`config/`](config/) | Dữ liệu data-driven (gameplay/liveops) | [ADR-004/005](docs/adr/) |
| [`tools/`](tools/) · [`scripts/`](scripts/) | Công cụ nội bộ & tự động hoá | — |
| [`deploy/`](deploy/) | Docker/compose/k8s | [docs/deployment](docs/deployment/) |
| [`.github/`](.github/) | CI/CD, template, CODEOWNERS | [ci-cd](docs/deployment/ci-cd-pipeline.md) |
| [`docs/`](docs/) | **Bộ não dự án**: SSOT + blueprint kiến trúc | [docs/README](docs/README.md) |
| [`CLAUDE.md`](CLAUDE.md) · [`.claude/`](.claude/) | **AI execution layer**: điểm vào tự nạp + workflow/checklist/agents | [AI_GUIDE](AI_GUIDE.md) |

> **Nguồn sự thật (SSOT):** nghiệp vụ ở [`docs/mvp/`](docs/mvp/), kiến trúc ở [`docs/`](docs/README.md). **Không** đổi thiết kế trong code — tham chiếu tài liệu.

## Bắt đầu nhanh

```bash
# Backend
dotnet build server/GameTeam.sln
dotnet test  server/GameTeam.sln
dotnet run --project server/src/GameTeam.Api      # http://localhost:5xxx/health

# Hạ tầng local (Postgres + Redis)
cp .env.example .env
scripts/dev/up.sh            # hoặc scripts\dev\up.ps1 trên Windows

# Client: mở client/project.godot bằng Godot 4.x
```

Xem chi tiết: [SETUP.md](SETUP.md) · [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md).

## Tài liệu chính

| Tôi muốn… | Đọc |
|---|---|
| Hiểu kiến trúc tổng | [ARCHITECTURE.md](ARCHITECTURE.md) → [docs/architecture](docs/architecture/) |
| Biết vì sao thiết kế thế này | [DECISIONS.md](DECISIONS.md) → [docs/adr](docs/adr/) |
| Đóng góp code | [CONTRIBUTING.md](CONTRIBUTING.md) · [STYLE_GUIDE.md](STYLE_GUIDE.md) |
| Làm việc cùng AI agent | [CLAUDE.md](CLAUDE.md) · [AI_GUIDE.md](AI_GUIDE.md) → [docs/ai](docs/ai/) |
| Kế hoạch phát triển | [ROADMAP.md](ROADMAP.md) → [docs/roadmap](docs/roadmap/) |
| Test / Deploy | [TESTING.md](TESTING.md) · [DEPLOYMENT.md](DEPLOYMENT.md) |

## Trạng thái

**Phase:** Project Bootstrap (P0) — khung repo production-ready đã dựng; **chưa** hiện thực gameplay/nghiệp vụ. Bước tiếp: Core Framework (P1). Xem [ROADMAP.md](ROADMAP.md) và [docs/audit/bootstrap-audit.md](docs/audit/bootstrap-audit.md).

## License
[MIT](LICENSE) © 2026.