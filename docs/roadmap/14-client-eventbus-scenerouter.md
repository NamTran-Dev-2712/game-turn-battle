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

- [x] Tạo `core/events/event_bus.gd`: cơ chế signal đặt tên; API `emit(event, payload)` + `subscribe`. — autoload `EventBus` (`client/src/core/events/event_bus.gd`), danh mục **đóng** = hằng `EVENTS` + `signal <name>(payload)`; API `emit`/`subscribe`/`unsubscribe`/`is_known` (assert event ∈ `EVENTS` ⇒ chống "God channel"); tự cleanup qua signal Godot. Verify: `event_bus_test.gd` 5/5 pass.
- [x] Lập **danh mục event** nền + quy ước đặt tên `snake_case` past-tense (theo [`../conventions/naming.md`](../conventions/naming.md)); ghi vào [`../godot/state-and-signals.md`](../godot/state-and-signals.md). — §3.1 mới: bảng danh mục (1 event nền `scene_changed` `{to,from}`, producer/consumer) + quy tắc naming past-tense + quy trình thêm event; mọi event dùng trong code đều documented (không "event chui").
- [x] Tạo `core/scene/scene_router.gd`: `goto_scene(path)`, back stack, transition đơn giản. — autoload `SceneRouter` (`client/src/core/scene/scene_router.gd`): `goto_scene`(push)/`back`(pop)/`stack_depth`/`clear_history`; scene-host thủ công `queue_free` scene cũ (không rò rỉ — ADR-009); transition tối giản (tráo tức thời). Verify: `scene_router_test.gd` 4/4 pass.
- [x] Đăng ký autoload trong `project.godot` (đúng tên PascalCase node). — section `[autoload]`: `EventBus`/`SceneRouter` (node PascalCase, `*` enabled), hai autoload độc lập. Verify: import tạo autoload OK + `test_autoloads_present` xanh (`/root/EventBus`, `/root/SceneRouter`).
- [x] Static typing GDScript (khai kiểu), tab indent. — typed var/param/`-> bool`/`-> void` toàn bộ; TAB indent (`.editorconfig`); bỏ `class_name` (tránh trùng singleton) ⇒ dùng global autoload static-typed. Verify: `--headless --import` 0 warning/0 error.
- [x] Test gdUnit4: emit→nhận payload; goto A→B→back về A. — `event_bus_test.gd` (emit→nhận đúng payload, unsubscribe ngắt, nối trùng 1 lần, danh mục đóng) + `scene_router_test.gd` (goto A→B→back→A, stack đúng, scene cũ freed, path lỗi→false, phát `scene_changed`). Verify: **11/11 pass, 0 orphan** headless local (Godot 4.7.1).
- [x] Cập nhật `../godot/state-and-signals.md` + `../godot/scene-architecture.md`. — state-and-signals §3.1 (EventBus catalogue + API) + scene-architecture §4.1 (SceneRouter API/lifecycle) + §4/§5 (reconcile push/replace→`goto_scene`/`back`, trạng thái autoload).

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

**Kết luận: ĐỦ ĐIỀU KIỆN ĐÓNG (local PASS 2026-08-18).** EventBus + SceneRouter chạy, danh mục event tài
liệu hoá (§3.1), test gdUnit4 xanh headless, hai autoload tối giản độc lập (không God autoload).

**Bảng audit:**

| Requirement | Implementation | Test / Verification | Status |
|---|---|---|---|
| EventBus emit/subscribe hoạt động | `event_bus.gd` `emit`/`subscribe`/`unsubscribe`/`is_known`, signal khai báo + hằng `EVENTS` | `event_bus_test.gd`: emit→nhận đúng payload; unsubscribe ngắt; nối trùng 1 lần | PASS |
| Danh mục event tài liệu hoá (không "chui") | Danh mục **đóng** (`EVENTS`), seed 1 event `scene_changed`; `state-and-signals.md` §3.1 | `test_catalogue_is_closed` (event lạ ⇒ `is_known`=false); mọi event dùng đều trong bảng | PASS |
| SceneRouter đổi scene + back stack đúng | `scene_router.gd` `goto_scene`(push)/`back`(pop), scene-host thủ công | `test_goto_and_back_navigates_scene_stack`: goto A→B→back→A + `stack_depth` đúng | PASS |
| Giải phóng scene cũ (không giữ ref — ADR-009) | `queue_free()` scene cũ khi tráo | `is_instance_valid(scene_a)`=false sau 1 frame; **0 orphan** | PASS |
| Autoload đăng ký, PascalCase, độc lập | `[autoload]` trong `project.godot`; 2 autoload SRP | import tạo autoload OK; `test_autoloads_present` (`/root/EventBus`,`/root/SceneRouter`) | PASS |
| Static typing + tab indent, không warning | typed toàn bộ, TAB, bỏ `class_name` ⇒ global autoload | `--headless --import` exit 0, **0 warning/0 error** | PASS |
| Test gdUnit4 xanh headless; Godot import sạch | `client/tests/core/*` + fixtures | gdUnit4 **11/11 pass, 0 error/0 failure/0 orphan**; import exit 0 | PASS |
| Không feature import chéo (chỉ qua EventBus) | Chưa có feature; SceneRouter phát `scene_changed` qua EventBus (không bị import) | Rà `client/src`: không `change_scene*` rải rác ngoài SceneRouter | PASS |

**Deviations có chủ đích:**
1. **Bỏ `class_name`** trên script autoload — trùng tên singleton (`EventBus`/`SceneRouter`) gây Godot "hides an
   autoload singleton". Truy cập qua global autoload (`EventBus.emit(...)`), vẫn static-typed/không warning.
2. **Scene-host thủ công** thay `get_tree().change_scene_to_file()` — chưa có `run/main_scene` (boot = Phase 17),
   và để kiểm soát vòng đời/cleanup + test tất định không quấy rối runner.
3. **Seed đúng 1 event nền** `scene_changed` (event mà code Phase 14 thực sự phát) — không seed event nghiệp vụ
   phase 15+ (tránh scope creep + "event chui").
4. **Negative test** cho phase client này = invalid-path→`false` + closed-catalogue (assert **vĩnh viễn xanh**);
   không có architecture-gate kiểu NetArchTest để break-and-revert như phase backend.

**Verify:** Godot **4.7.1-stable** (Windows, cục bộ): `godot --headless --import --path client` exit 0 (autoload
tạo OK, 0 lỗi/0 warning script mới); gdUnit4 headless (`runtest.cmd --godot_binary <godot> -a res://tests -rd
reports`) **Overall: 11 test cases | 0 errors | 0 failures | 0 orphans | PASSED** (event_bus 5 + scene_router 4 +
smoke 2), JUnit `reports/report_1/results.xml` sinh. `reports/` + `.uid` git-ignored (worktree sạch: chỉ file
trong scope). **CI-pending:** `ci-client.yml` (import + gdUnit4 dưới `xvfb` trên GitHub Actions runner) xác nhận
chính thức khi PR chạy — logic đã chứng minh đầy đủ cục bộ (§4.5 CI-only gate).

---

## Liên kết
- [`../godot/scene-architecture.md`](../godot/scene-architecture.md) · [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../conventions/naming.md`](../conventions/naming.md)
- ADR: [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`15-client-networkclient.md`](15-client-networkclient.md)
