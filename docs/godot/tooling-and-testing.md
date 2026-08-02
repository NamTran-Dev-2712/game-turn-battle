# Tooling & Testing (Client)

> Debug tools, editor tools, plugin strategy, và testing cho client Godot. Chi tiết chiến lược test tổng ở `../testing/`.

---

## 1. Plugin strategy (ADR-010)
- Hạn chế addon bên thứ ba; ưu tiên tính năng lõi Godot.
- Addon để trong `client/addons/`, **commit** + ghi version/license.
- Ghim version Godot; CI dùng đúng version (`../deployment/ci-cd-pipeline.md`).

## 2. Editor tools (nội bộ)
| Tool | Mục đích |
|---|---|
| Config preview | Xem `.tres`/config hero/skill trong editor |
| Combat sim runner | Chạy sim với seed cố định, xem log (đối chiếu server) |
| Scene template | Mẫu scene cho feature mới (nhất quán cấu trúc) |
- Editor tool viết dạng `@tool` script/plugin, tách khỏi runtime.

## 3. Debug tools (runtime)
| Tool | Mục đích |
|---|---|
| Debug overlay | FPS, RAM, network status (dev build) |
| Config version display | Hiện version config đang dùng |
| Fake latency/offline | Test xử lý mất mạng (`../mvp/10` UX3) |
| Seed inspector | Xem seed trận để tái lập bug combat |
- Debug tool chỉ bật ở dev build (feature flag/biên dịch điều kiện).

## 4. Testing client

| Loại | Công cụ | Phạm vi |
|---|---|---|
| Unit | gdUnit4 / GUT | Logic thuần: combat sim, fixed-point math, view-model |
| Golden vector | gdUnit4 | Combat sim khớp test vector chung với server (ADR-011) |
| Integration | gdUnit4 | Feature + service (mock NetworkClient) |
| Smoke | CI headless | Boot game, vào các screen chính không lỗi |

**Ưu tiên test:** combat sim (determinism), math fixed-point, mapping config → resource. UI test ở mức smoke.

## 5. Chạy test trong CI
- Godot headless chạy test client (`../deployment/ci-cd-pipeline.md`).
- Golden vector combat chạy **cả client & server** để đảm bảo khớp.

## 6. Liên kết
- Testing tổng: `../testing/README.md`, `../testing/godot-testing.md`
- Determinism: ADR-011, `../gameplay/combat-framework.md`
- Dependency mgmt: ADR-010
