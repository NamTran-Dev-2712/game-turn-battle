# Godot Testing (Client)

> Unit, integration, smoke, golden vector cho client. Nền: `README.md`. Chi tiết tooling ở `../godot/tooling-and-testing.md`.

---

## 1. Công cụ (đề xuất)
| Mục | Công cụ |
|---|---|
| Test framework | gdUnit4 (hoặc GUT) |
| Chạy CI | Godot headless |

## 2. Trọng tâm test client

| Loại | Trọng tâm |
|---|---|
| Unit | Combat sim (client), fixed-point math, mapping config→Resource, view-model logic |
| **Golden vector** | Sim client khớp test vector chung với server (ADR-011) |
| Integration | Feature + service (mock `NetworkClient`), scene router, config provider |
| Smoke | Boot game, vào các screen chính, không lỗi/khựng |
| Offline/latency | Xử lý mất mạng/độ trễ (`../mvp/10` UX3) |

## 3. Golden vector (điểm mấu chốt)
- Bộ dữ liệu `(configVersion, teamSnapshot, stage, seed) → expectedOutcome` **dùng chung** client & server.
- Chạy cả hai phía trong CI; lệch = **fail** → bảo đảm re-sim server khớp client hiển thị.

## 4. Nguyên tắc
- Logic thuần (sim/math) test được không cần scene → nhanh, ổn định.
- UI test ở mức smoke/interaction cơ bản (tránh test brittle theo pixel).
- Determinism: sim nhận seed/PRNG truyền vào (không RNG toàn cục).
- Mock network để test feature độc lập server.

## 5. CI
- Godot headless chạy unit + golden + smoke (`../deployment/ci-cd-pipeline.md` §4c).
- Ghim version Godot (ADR-010) — Godot **4.7**, gdUnit4 **v6.2.0** (vendored).
- **Hiện trạng Phase 03 (nền):** mới 1 smoke test tất định `client/tests/smoke/example_smoke_test.gd` (`extends GdUnitTestSuite`), chạy qua `runtest.sh` dưới `xvfb-run`, xuất **JUnit** `client/reports/report_<n>/results.xml`. Golden vector để **Phase 26**; unit/integration feature thêm dần ở các phase nhóm 3+.

## 6. Liên kết
- Strategy: `README.md` · Tooling: `../godot/tooling-and-testing.md`
- Combat: `../gameplay/combat-framework.md`, ADR-011
