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

## 3. Golden vector (điểm mấu chốt) — Phase 26 đã hiện thực
- Bộ dữ liệu `(config_version, team_snapshot, stage, seed) → expected(event_log, result)` **dùng chung** client & server,
  ở `shared/combat-vectors/*.json` (nguồn DUY NHẤT — client đọc qua `globalize_path`, KHÔNG copy/fork). Baseline sinh từ
  **sim server** (nguồn chân lý, ADR-011) bằng `tools/combat-baseline`.
- **Test client:** `client/tests/combat/golden_vector_test.gd` **tự khám phá** mọi vector qua
  `CombatVectorLoader.list_vector_files()` (thêm vector = KHÔNG sửa code test), chạy `BattleSimulator` client rồi so
  từng sự kiện + result với baseline qua `JsonDiff.first_difference`; lệch = FAIL (báo rõ tên vector).
- Chạy cả hai phía trong CI; lệch = **fail** → server ≡ client ≡ baseline (re-sim server khớp client hiển thị).
- **KHÔNG sửa vector/baseline để test xanh.** Đổi công thức sim = regenerate baseline CÓ CHỦ ĐÍCH từ server + review
  (xem `tools/combat-baseline/README.md`, agent `combat-determinism`).

## 4. Nguyên tắc
- Logic thuần (sim/math) test được không cần scene → nhanh, ổn định.
- UI test ở mức smoke/interaction cơ bản (tránh test brittle theo pixel).
- Determinism: sim nhận seed/PRNG truyền vào (không RNG toàn cục).
- Mock network để test feature độc lập server.

## 5. CI
- Godot headless chạy unit + golden + smoke (`../deployment/ci-cd-pipeline.md` §4c).
- Ghim version Godot (ADR-010) — Godot **4.7**, gdUnit4 **v6.2.0** (vendored).
- **Hiện trạng:** smoke test nền Phase 03 (`client/tests/smoke/example_smoke_test.gd`) + bộ combat client Phase 25
  (`client/tests/combat/*`: golden, determinism, outcome, fixed_point, pcg32, resolver) chạy qua `runtest.sh` dưới
  `xvfb-run`, xuất **JUnit** `client/reports/report_<n>/results.xml`. **Golden vector = Phase 26 đã bật** (§3): nửa client
  của gate `golden-vector` chạy trong job `build-test` này; trigger `shared/combat-vectors/**` được thêm vào
  `ci-client.yml` nên đổi vector re-chạy client. Unit/integration feature thêm dần ở các phase nhóm 3+.

## 6. Liên kết
- Strategy: `README.md` · Tooling: `../godot/tooling-and-testing.md`
- Combat: `../gameplay/combat-framework.md`, ADR-011
