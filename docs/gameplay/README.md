# Gameplay Systems — Technical Architecture

> Thiết kế **ranh giới & trách nhiệm module** cho các hệ thống gameplay. **KHÔNG hiện thực logic gameplay**, KHÔNG đổi thiết kế/scope (SSOT = `../mvp/`). Chỉ mô tả module boundary, dữ liệu, và phân chia client/server.

## Danh mục
| File | Hệ thống | Nguồn MVP |
|---|---|---|
| [hero-system.md](hero-system.md) | Hero | `../mvp/03` §2 |
| [combat-framework.md](combat-framework.md) | Combat (deterministic, server-auth) | `../mvp/03` §4, ADR-011 |
| [skill-framework.md](skill-framework.md) | Skills (effect data-driven) | `../mvp/03` §3 |
| [inventory-and-equipment.md](inventory-and-equipment.md) | Inventory, Equipment | `../mvp/03` §10,11 |
| [quest-system.md](quest-system.md) | Quest | `../mvp/03` §12 |
| [progression-and-economy.md](progression-and-economy.md) | Progression, Economy, AFK | `../mvp/05`, `06` |
| [configuration-and-data.md](configuration-and-data.md) | Config/data schema | ADR-004/005 |

## Nguyên tắc chung (áp dụng mọi hệ thống)

| Nguyên tắc | Diễn giải |
|---|---|
| Data-driven | Chỉ số/cân bằng từ config, không hardcode (ADR-004) |
| Server-authoritative | Kết quả/thưởng/giao dịch quyết ở server (ADR-007/011) |
| Composition | Ghép component; không kế thừa sâu; không switch mở rộng |
| Event-driven | Domain event (BE) / Event Bus (client) để tách rời |
| Module boundary | Mỗi hệ thống có ranh giới rõ, giao tiếp qua interface/event |
| SRP | Không God system/manager |
| Traceability | Mọi rule tham chiếu `../mvp/*`, không phát minh |

## Phân chia client/server (tổng quát)

| Trách nhiệm | Client | Server |
|---|---|---|
| Hiển thị/animation/UX | ✅ | — |
| Nhập liệu/ý định | ✅ | — |
| Nguồn sự thật state | — | ✅ |
| Kết quả combat/thưởng | Replay hiển thị | ✅ Quyết định |
| Gacha RNG | — | ✅ |
| AFK/energy tính | Hiển thị ước lượng | ✅ Quyết định (khi claim) |
| Giao dịch tài nguyên | — | ✅ Atomic |
