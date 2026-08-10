# Tooling & Testing (Client)

> Debug tools, editor tools, plugin strategy, và testing cho client Godot. Chi tiết chiến lược test tổng ở `../testing/`.

---

## 1. Plugin strategy (ADR-010)
- Hạn chế addon bên thứ ba; ưu tiên tính năng lõi Godot.
- Addon để trong `client/addons/`, **commit** + ghi version/license.
- Ghim version Godot; CI dùng đúng version (`../deployment/ci-cd-pipeline.md`).

### 1a. Version pin (nguồn sự thật — Phase 03)

| Thành phần | Pin | Nguồn sự thật | Ghi chú |
|---|---|---|---|
| **Godot** | **4.7** (`4.7-stable`) | `client/project.godot` (`config/features` chứa `"4.7"`) **và** `ci-client.yml` `env.GODOT_VERSION` | Hai nơi phải khớp; CI có bước guard `--version`. Tải binary Linux x86_64 chính thức + verify `SHA512-SUMS.txt`, cache theo release. Không dùng `latest`. |
| **gdUnit4** | **v6.2.0** (MIT) | `client/addons/gdUnit4/plugin.cfg` (`version="6.2.0"`) + `ci-client.yml` `env.GDUNIT4_VERSION` | Addon **vendored/commit** (599 file, kèm `LICENSE`). Yêu cầu Godot ≥ 4.5. Enable ở `project.godot [editor_plugins]`; plugin tự bỏ qua UI khi headless/`--import`/CLI. |

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
- Godot headless chạy test client (`../deployment/ci-cd-pipeline.md` §4c).
- **Hiện trạng Phase 03 (nền):** `ci-client.yml` = tải+verify Godot 4.7 → `godot --headless --import --path client` (import gate) → gdUnit4 chạy `runtest.sh -a res://tests -rd reports` dưới `xvfb-run` → xuất **JUnit** `client/reports/report_<n>/results.xml` (upload artifact `gdunit4-results`). Mới ở mức 1 smoke test tất định.
- Golden vector combat chạy **cả client & server** để đảm bảo khớp — **để Phase 26** (ADR-011), chưa có ở cổng nền.

## 6. Liên kết
- Testing tổng: `../testing/README.md`, `../testing/godot-testing.md`
- Determinism: ADR-011, `../gameplay/combat-framework.md`
- Dependency mgmt: ADR-010
