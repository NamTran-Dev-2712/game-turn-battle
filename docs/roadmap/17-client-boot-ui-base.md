# 17 — Client boot flow + main scene + UI architecture base

> Mục đích: Dựng luồng khởi động client (boot → health check → nhận config → main scene) và **nền kiến trúc UI** (view tách logic, presenter/view-model) theo ADR-002.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 3 Client Core Framework | P1 | S3 | nền client |

# Mục tiêu

Scene khởi động: boot splash → gọi `/health` + nhận config bundle (ConfigProvider) → điều hướng (SceneRouter) tới main hub tối giản. Nền UI: base view/presenter, UI **không** gọi network trực tiếp, chỉ qua feature/EventBus.

# Lý do

Đóng nhóm 3 bằng một lát cắt "boot chạy được": chứng minh 4 autoload phối hợp (EventBus/NetworkClient/ConfigProvider/StateCache) và thiết lập khuôn UI để feature sau (nhóm 6+) gắn view nhất quán.

# Phụ thuộc

- **Trước:** 14, 15, 16.
- **Sau:** 20 (auth vào boot), 22 (config e2e), mọi feature UI.

# Phạm vi

- Boot scene: splash → health check → nhận config → route tới main hub.
- Main hub scene tối giản (placeholder các nút feature, chưa nghiệp vụ).
- UI base: lớp `BaseView`/presenter (view-model), quy ước UI chỉ nhận dữ liệu + phát intent, không gọi net.
- Xử lý lỗi boot (mất mạng/health fail) → màn báo lỗi + retry.

# Không thuộc phạm vi

- Đăng nhập thật (phase 20) — boot dùng health + config trước.
- Feature nghiệp vụ (hero/battle…).
- Art/animation hoàn chỉnh (ADR-009 tối ưu ở phase 52).

# Deliverables

- Boot scene + main hub scene tối giản chạy được.
- UI base (BaseView/presenter) + quy ước tài liệu hoá.
- Màn lỗi boot + retry.
- Test gdUnit4: boot happy-path (mock health+config) → tới hub; boot fail → màn lỗi.

# Công việc cần thực hiện

- [ ] Tạo boot scene: splash → `NetworkClient.get(/health)` → `ConfigProvider` nhận/nạp bundle → `SceneRouter.goto(main_hub)`.
- [ ] Tạo main hub scene tối giản (nút placeholder cho feature, dùng SceneRouter).
- [ ] UI base: `BaseView` + presenter/view-model; quy ước dữ liệu vào, intent ra (EventBus), **không** gọi NetworkClient trong view.
- [ ] Màn lỗi boot (health fail/mất mạng) + nút retry.
- [ ] Static typing, node PascalCase theo vai trò, tab indent.
- [ ] Test gdUnit4: happy-path boot→hub; fail→error screen; retry.
- [ ] Cập nhật [`../godot/ui-architecture.md`](../godot/ui-architecture.md) + [`../godot/scene-architecture.md`](../godot/scene-architecture.md).

# Tiêu chí hoàn thành

- Chạy client → boot gọi health + nhận config → vào main hub.
- Mất mạng/health fail → màn lỗi + retry hoạt động.
- UI base tách logic: không view nào gọi NetworkClient trực tiếp.
- Test gdUnit4 xanh headless; Godot import sạch.

# Cách kiểm tra

- Chạy server local → mở client → thấy boot → hub.
- Tắt server → client hiện màn lỗi boot + retry.
- gdUnit4: happy-path & fail-path.
- Grep: view không import `core/net`.

# Rủi ro

- **Boot chặn UI khi mạng chậm** → tải nền, splash không block; timeout → màn lỗi.
- **UI dính logic/network** → review guard + BaseView chuẩn.
- **Scene rò tài nguyên** → giải phóng khi chuyển (ADR-009, hoàn thiện phase 52).

# Ghi chú

Auth sẽ chèn vào boot ở phase 20 (guest login trước khi vào hub). Đây là "hello-world chơi được": boot + kết nối + config. Bám [`../godot/ui-architecture.md`](../godot/ui-architecture.md) + ADR-002.

# Technical Debt Review

- **Maintainability:** UI base chuẩn cho mọi feature; boot rõ ràng.
- **Scalability:** hub + router cho phép thêm feature dễ.
- **Testing:** boot path có test; nền test UI.
- **Security:** không client-authority; lỗi boot không lộ chi tiết.
- **Nợ:** auth vào boot (20); tối ưu asset/anim (52).

# Phase Review

Đóng khi boot→config→hub chạy, màn lỗi+retry hoạt động, UI base tách logic, test gdUnit4 xanh. **Kết thúc nhóm 3 — client core sẵn sàng.**

---

## Liên kết
- [`../godot/ui-architecture.md`](../godot/ui-architecture.md) · [`../godot/scene-architecture.md`](../godot/scene-architecture.md) · [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md)
- ADR: [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md) · [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`18-auth-jwt-guest.md`](18-auth-jwt-guest.md)
