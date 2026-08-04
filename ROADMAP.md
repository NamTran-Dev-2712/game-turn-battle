# Roadmap

> Tóm tắt. **Nguồn đầy đủ:** [`docs/roadmap/`](docs/roadmap/README.md) — sổ tay vận hành ([`README.md`](docs/roadmap/README.md)) + **55 file phase chi tiết đánh số** (`01-*.md` … `55-*.md`), mỗi phase một mục tiêu, có checklist · tiêu chí hoàn thành · cách kiểm tra · rủi ro · technical-debt review. Mỗi coding session làm **đúng 1 phase**. Góc nhìn thô: phase giao hàng P0–P7 (README §12) và thứ tự kỹ thuật [`docs/architecture/implementation-order.md`](docs/architecture/implementation-order.md) (S0–S13).

## Trình tự tổng
**Bootstrap → Core Framework → Gameplay Systems → Backend Integration → LiveOps → Polish → Release**

| # | Giai đoạn | Trọng tâm | Phase |
|---|---|---|---|
| 1 | Project Bootstrap | Repo layout, CI skeleton, conventions, Docker dev | **P0 ✅ (đang chốt)** |
| 2 | Core Framework | Contracts+schema, BE skeleton+DI, autoloads, Auth+Save, Config Service | P1 |
| 3 | Gameplay Systems | Combat sim deterministic (golden vector), Hero/Formation/Battle, Summon/Inventory, Campaign/AFK | P2–P3 |
| 4 | Backend Integration | Equipment/Ascension/Shop/Quest/Mail, Ranking, tích hợp chặt | P4–P5 |
| 5 | LiveOps | Remote config, feature flags, mail hàng loạt, telemetry | P6 |
| 6 | Polish | Balance, perf mobile, security pass, regression/smoke | P7 |
| 7 | Release | Build Android trước, monitoring, soft launch | P7 |

**Nguyên tắc:** không giai đoạn nào phải viết lại giai đoạn trước (nền chốt bằng ADR trước khi build); cắt scope theo MoSCoW ([`docs/mvp/01`](docs/mvp/)) khi trễ.
