# ADR-011: Combat Authority & Determinism (Thẩm quyền & tính tất định của combat)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect, Chủ dự án
- Related: ADR-003, ADR-007, ADR-008, `../gameplay/combat-framework.md`, `../mvp/08`, `../mvp/14` (R1/R3)

## Context
`../mvp/14` R1/R3 là 2 câu hỏi **chặn kiến trúc**, nay đã chốt với chủ dự án. Combat full-auto (`../mvp/03`, `13` A02); là nơi mọi quyết định (team/formation/nâng cấp) được "chấm điểm"; liên quan thưởng → **nhạy cảm gian lận** (`../mvp/08` §4). Cần verify được, replay được, sweep tức thì.

## Decision
1. **Server-authoritative + re-simulation**: server là bên **quyết định kết quả & cấp thưởng**. Client mô phỏng chỉ để **hiển thị/dự đoán**.
2. **Deterministic theo seed**: bộ sim dùng **integer/fixed-point math + seeded RNG**; cùng (config version + team snapshot + stage + seed) ⇒ **cùng kết quả** ở mọi máy.
3. **Ruleset dùng chung**: một đặc tả combat, hiện thực đồng nhất ở client (GDScript) và server (.NET); **golden test vector** đảm bảo khớp.
4. **Luồng**: client gửi ý định (team, stage) → server sinh seed, re-sim, ghi kết quả+thưởng (transaction) → trả `BattleResult{seed, outcome, rewards, log}` → client replay bằng seed để xem.
5. **Sweep/quick-battle**: server tính kết quả (không cần client xem), dùng lại tính tất định.

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Client-authoritative + sanity check | Rủi ro gian lận cao — chủ dự án loại |
| Hybrid (combat client-auth, heuristic check) | Yếu hơn cho hệ liên quan thưởng; chủ dự án chọn full server-auth |
| Non-deterministic (float tự do) | Không re-sim/verify/replay được — chủ dự án loại |

## Trade-offs
- **Được:** chống gian lận mạnh, replay/verify/sweep, debug tái lập, nền cho leaderboard/PvP tương lai.
- **Mất:** **kỷ luật code combat cao** (không dùng float bừa, tránh phụ thuộc thứ tự iteration không ổn định); tải server cao hơn (re-sim); phải giữ 2 hiện thực đồng nhất.

## Consequences
- `client/src/combat` & sim server thuần, không I/O, đọc chỉ số từ config (ADR-004/005).
- **Golden test vector** chạy CI cả hai phía (`../testing/`).
- Kết quả & thưởng atomic, idempotent (ADR-007).
- Quy tắc code determinism ghi ở `../conventions/code-style.md` & `../gameplay/combat-framework.md`.
- Tối ưu re-sim (batch, cache) ở giai đoạn hardening.
