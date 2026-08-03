# `client/src/combat/` — Deterministic combat sim (client)

> Bản **sim combat deterministic** phía client, **thuần logic, không UI**. Phải khớp bit-for-bit với sim server (golden vector — ADR-011).

| Mục | Nội dung |
|---|---|
| **Purpose** | Tái hiện trận đấu để render/preview; là "ruleset" bản client song song với server. |
| **Responsibilities** | Chạy sim theo seed + input formation; xuất chuỗi sự kiện để feature `battle` render. |
| **Allowed** | Math **integer/fixed-point**, seeded PRNG truyền vào, iteration order ổn định. |
| **Not allowed** | ❌ `float` trong sim; ❌ RNG toàn cục; ❌ dùng kết quả client làm nguồn phần thưởng (server re-sim là nguồn sự thật). |
| **Dependencies** | `src/shared` (fixed-point math). Không phụ thuộc UI/feature. |
| **Owner** | Combat/gameplay client team + đồng bộ với server combat. |
| **Future expansion** | Thêm effect qua registry data-driven (không `switch` mở rộng). |

Chi tiết: `../../../docs/gameplay/combat-framework.md`, `../../../docs/adr/ADR-011-combat-authority-and-determinism.md`, `../../../docs/testing/godot-testing.md` (golden vectors).
