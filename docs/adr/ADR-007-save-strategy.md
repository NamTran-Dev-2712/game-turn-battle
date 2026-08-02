# ADR-007: Save Strategy (Chiến lược lưu trữ tiến trình)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect, Chủ dự án
- Related: ADR-003, ADR-008, ADR-011, `../mvp/08`, `../mvp/14` (R1/R2)

## Context
`../mvp/14` R1/R2 đã chốt: **server-authoritative** & **online-required**. Mất/hỏng dữ liệu là rủi ro sống còn (`../mvp/09` BE2). Save & config đổi liên tục khi live → cần versioning/migration (`../mvp/08` TE4).

## Decision
**Server-authoritative save**: nguồn sự thật của toàn bộ profile (hero, currency, progression, inventory...) nằm ở **backend (PostgreSQL)**:
- Client **không** giữ save quyền lực; chỉ **cache đọc** để hiển thị/offline-view.
- Mọi thay đổi trạng thái đi qua **command server-side** (atomic/transaction), có **idempotency** cho claim/giao dịch (chống double-grant).
- **Schema versioning + migration** cho profile (EF Core migrations + version field).
- Sao lưu định kỳ + khôi phục (`../deployment/release-operations.md`).
- AFK/energy tính theo **server time** (ADR-011/008).

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Local save (client) là chính | Dễ gian lận, mất khi đổi máy — ngược R1/R2 |
| Offline-first + sync | Chủ dự án chọn online-required; reconcile phức tạp, rủi ro gian lận (`../mvp/13` A19) |
| Chỉ cloud blob save | Khó truy vấn/leaderboard/anti-cheat; dùng DB quan hệ tốt hơn |

## Trade-offs
- **Được:** chống gian lận, không mất dữ liệu, đa thiết bị (Post-MVP link account), truy vấn được.
- **Mất:** cần mạng để chơi; tải backend cao hơn; cần thiết kế idempotency & transaction cẩn thận.

## Consequences
- Profile schema + migration ở Infrastructure (`../backend/infrastructure.md`).
- Giao dịch tài nguyên atomic (`../gameplay/progression-and-economy.md`).
- Client `StateCache` chỉ đọc/hiển thị (`../godot/state-and-signals.md`).
- Backup/restore & versioning trong vận hành (`../deployment/`).
