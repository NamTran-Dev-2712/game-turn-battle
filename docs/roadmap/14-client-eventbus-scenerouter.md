# 14 — Client autoload: EventBus + SceneRouter

> Mục đích: Dựng hai autoload lõi **EventBus** (giao tiếp giữa feature qua signal, không cross-import) và **SceneRouter** (điều hướng scene tập trung) theo ADR-002.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 3 Client Core Framework | P1 | S3 | nền client |

# Mục tiêu

Tạo autoload `EventBus` (đăng ký/phát signal đặt tên, dạng past-tense event) và `SceneRouter` (chuyển scene, stack điều hướng) trong `client/src/core/events` & `core/scene`, đăng ký trong `project.godot`.

# Lý do

ADR-002: feature không import lẫn nhau; giao tiếp qua Event Bus/signals; điều hướng scene không rải rác. Hai autoload này là xương sống kết nối lỏng cho toàn client; phải có trước feature nào.

# Phụ thuộc

- **Trước:** 03 (client CI), 01 (conventions).
- **Sau:** 15–17 (autoload khác + boot), mọi feature client publish/subscribe event & đổi scene.

# Phạm vi

- `EventBus` autoload: API `subscribe/emit` (hoặc dùng signal Godot chuẩn), danh mục event **được tài liệu hoá** (chống "God channel").
- `SceneRouter` autoload: `goto_scene`, back stack, transition tối giản.
- Đăng ký autoload tối giản (single-responsibility, không "God autoload").
- Test gdUnit4 cho publish/subscribe & điều hướng.

# Không thuộc phạm vi

- NetworkClient/ConfigProvider/StateCache (phase 15–16).
- UI thực (phase 17).
- Feature nghiệp vụ.

# Deliverables

- `event_bus.gd`, `scene_router.gd` autoload đăng ký.
- Danh mục event nền tài liệu hoá (naming past-tense, ví dụ `battle_finished`).
- Test gdUnit4: emit→subscriber nhận; goto→scene đổi; back hoạt động.

# Công việc cần thực hiện

- [ ] Tạo `core/events/event_bus.gd`: cơ chế signal đặt tên; API `emit(event, payload)` + `subscribe`.
- [ ] Lập **danh mục event** nền + quy ước đặt tên `snake_case` past-tense (theo [`../conventions/naming.md`](../conventions/naming.md)); ghi vào [`../godot/state-and-signals.md`](../godot/state-and-signals.md).
- [ ] Tạo `core/scene/scene_router.gd`: `goto_scene(path)`, back stack, transition đơn giản.
- [ ] Đăng ký autoload trong `project.godot` (đúng tên PascalCase node).
- [ ] Static typing GDScript (khai kiểu), tab indent.
- [ ] Test gdUnit4: emit→nhận payload; goto A→B→back về A.
- [ ] Cập nhật `../godot/state-and-signals.md` + `../godot/scene-architecture.md`.

# Tiêu chí hoàn thành

- EventBus emit/subscribe hoạt động; danh mục event tài liệu hoá (không event "chui").
- SceneRouter đổi scene + back stack đúng.
- Autoload tối giản, single-responsibility (không gộp thành God autoload).
- Test gdUnit4 xanh headless; Godot import sạch.

# Cách kiểm tra

- `godot --headless --import` không lỗi.
- Chạy gdUnit4 headless: test EventBus & SceneRouter pass.
- Rà: không feature nào import feature khác (chỉ qua EventBus).

# Rủi ro

- **EventBus thành "God channel" ẩn** → bắt buộc tài liệu hoá mọi event + review naming.
- **Rò rỉ subscriber (memory leak)** → hỗ trợ unsubscribe / dùng signal Godot tự quản.
- **SceneRouter giữ tham chiếu scene cũ** → giải phóng đúng khi chuyển (liên quan ADR-009).

# Ghi chú

Bám [`../godot/scene-architecture.md`](../godot/scene-architecture.md) + [`../godot/state-and-signals.md`](../godot/state-and-signals.md) + ADR-002. Danh mục event là "hợp đồng nội bộ client".

# Technical Debt Review

- **Maintainability:** kết nối lỏng, feature độc lập, dễ test.
- **Scalability:** thêm feature không tăng coupling.
- **Testing:** autoload lõi có test sớm.
- **Security:** không áp dụng (client hiển thị).
- **Nợ:** transition/animation nâng cao để phase UI sau.

# Phase Review

Đóng khi EventBus + SceneRouter chạy, danh mục event tài liệu hoá, test gdUnit4 xanh, autoload tối giản.

---

## Liên kết
- [`../godot/scene-architecture.md`](../godot/scene-architecture.md) · [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../conventions/naming.md`](../conventions/naming.md)
- ADR: [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`15-client-networkclient.md`](15-client-networkclient.md)
