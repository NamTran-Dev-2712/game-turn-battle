# ADR-008: Networking (Chiến lược mạng)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect, Chủ dự án
- Related: ADR-003, ADR-007, ADR-011, `../mvp/14` (R2), `../backend/api-and-versioning.md`

## Context
`../mvp/14` R2 chốt **online-required, server-authoritative**. Cần giao thức phù hợp idle RPG: phần lớn tương tác là **request/response** (đánh trận, summon, claim, nâng cấp); realtime chỉ cần cho một số trường hợp (Post-MVP: guild/arena/thông báo). Ràng buộc: JWT, SignalR (optional).

## Decision
- **Giao thức chính: HTTPS REST + JSON**, xác thực **JWT** (guest token → link account sau).
- **Hợp đồng versioned** (`/api/v1/...`) từ nguồn duy nhất `shared/contracts` (codegen client).
- **SignalR: optional**, chỉ dùng khi cần realtime (thông báo mail/energy, Post-MVP guild/arena). Không phụ thuộc SignalR cho core loop MVP.
- **Server time** là chuẩn cho mọi tính toán thời gian (AFK/energy/schedule).
- Idempotency key cho giao dịch nhạy cảm; retry an toàn.
- Xử lý mất mạng: client hiển thị cache + hàng đợi/thông báo, **không** tự quyết kết quả (`../mvp/10` UX3).

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Realtime socket cho mọi thứ | Thừa cho idle; tốn hạ tầng; REST đủ cho phần lớn |
| gRPC | Tốt nhưng thêm phức tạp toolchain Godot; REST/JSON đơn giản, dễ debug/AI |
| Offline-first sync | Ngược R2 (đã chốt) |

## Trade-offs
- **Được:** đơn giản, dễ cache/CDN, dễ versioning, dễ test; SignalR để dành khi thật cần.
- **Mất:** cần mạng để chơi; polling cho vài cập nhật nếu chưa bật SignalR.

## Consequences
- `client/core/net` (REST client + JWT) (`../godot/state-and-signals.md`).
- API versioning & error contract (`../backend/api-and-versioning.md`).
- SignalR hub đặt trong Api, bật theo feature flag (ADR-006).
- Combat kết quả trả kèm seed để client replay (ADR-011).
