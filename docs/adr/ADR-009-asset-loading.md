# ADR-009: Asset Loading (Chiến lược nạp asset)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: ADR-002, ADR-005, `../godot/resources-and-assets.md`, `../mvp/08`

## Context
Nhiều hero art/anim/VFX sẽ tăng dần (`../mvp/08` §3, `09` PF3). Mobile tầm trung: quan ngại thời gian load, RAM, battery. Số hero/nội dung tăng suốt live-service. Cần tránh load đồng bộ nặng gây khựng.

## Decision
Chiến lược nạp asset ở client:
- **Nạp bất đồng bộ theo yêu cầu** cho asset nặng (hero art/anim/VFX) — dùng background load, không chặn UI.
- **Object pooling** cho đối tượng combat/VFX tái sử dụng (`../mvp/09` PF1).
- **Atlas/streaming** cho sprite; nén texture phù hợp mobile.
- **Tách data khỏi asset nặng**: config (`Resource`) nhẹ, nạp sớm; art nặng nạp lazy.
- **Giải phóng chủ động** khi rời scene (quản lý memory — `../godot/resources-and-assets.md`).
- Asset **map từ config** (id → đường dẫn/atlas), không hardcode đường dẫn rải rác (ADR-004).

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Preload tất cả khi khởi động | Thời gian load & RAM bùng nổ khi nhiều hero |
| Load đồng bộ khi cần | Khựng UI/combat |
| Bundle tất cả trong 1 pack lớn | Cập nhật nội dung nặng nề |

## Trade-offs
- **Được:** khởi động nhanh, RAM ổn định, scale số hero tốt.
- **Mất:** phức tạp quản lý vòng đời asset & pooling; cần đo/profiling.

## Consequences
- `client/core` có dịch vụ nạp asset async + pool (`../godot/resources-and-assets.md`).
- Mapping asset trong config (ADR-004/005).
- Ngưỡng hiệu năng kiểm ở giai đoạn hardening (`../architecture/implementation-order.md` S13).
- (Post-MVP) cân nhắc tải content pack theo phiên bản từ server.
