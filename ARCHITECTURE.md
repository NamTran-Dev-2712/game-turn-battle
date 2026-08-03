# Kiến trúc — Điểm vào

> Tóm tắt điều hướng. **Nguồn đầy đủ:** [`docs/architecture/`](docs/architecture/) và [`docs/adr/`](docs/adr/).

## Tổng quan
- **Client** Godot 4.x (GDScript), **feature-based** + composition, autoload tối giản. → [docs/godot](docs/godot/), [docs/architecture/overview.md](docs/architecture/overview.md)
- **Backend** .NET 9 **Clean Architecture** (Domain · Application · Infrastructure · Api) + **CQRS/MediatR**. → [docs/backend](docs/backend/)
- **Shared** hợp đồng API + JSON Schema config (chống lệch client-server). → [structure §5](docs/architecture/project-structure.md)
- **Data-driven**: cân bằng gameplay ở `config/`, không hardcode.

## Quyết định nền tảng
| # | Quyết định | ADR |
|---|---|---|
| 1 | Server-authoritative + re-simulation | [ADR-011](docs/adr/ADR-011-combat-authority-and-determinism.md) |
| 2 | Online-required, server-authoritative state (AFK server-side) | [ADR-008](docs/adr/ADR-008-networking.md), [ADR-007](docs/adr/ADR-007-save-strategy.md) |
| 3 | Combat deterministic theo seed (integer/fixed-point) | [ADR-011](docs/adr/ADR-011-combat-authority-and-determinism.md) |
| 4 | Data-driven mọi cân bằng | [ADR-004](docs/adr/), [ADR-005](docs/adr/) |
| 5 | Clean Arch + CQRS (BE); feature-based + composition (client) | [ADR-003](docs/adr/), [ADR-002](docs/adr/) |

## Quy tắc phụ thuộc (bắt buộc)
Api → Application/Infrastructure(DI) → Domain; **Domain thuần**. Kiểm bằng NetArchTest ở CI. → [dependency-graph.md](docs/architecture/dependency-graph.md)

## Sơ đồ & chi tiết
Xem [docs/architecture/overview.md](docs/architecture/overview.md) (C4, sequence combat), [project-structure.md](docs/architecture/project-structure.md), [implementation-order.md](docs/architecture/implementation-order.md).
