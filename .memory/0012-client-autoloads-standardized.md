# 0012 — Client core autoloads standardized (Phase 14)

- Date: 2026-08-18
- Scope: workspace
- Status: Active

## Decision

Hai autoload lõi client ở `client/src/core/` là **xương sống kết nối lỏng** cho toàn client (ADR-002),
phải có trước mọi feature. Convention dưới đây là **hợp đồng cho mọi feature client** về sau: giao tiếp
liên feature qua `EventBus`, điều hướng scene qua `SceneRouter` — **không import chéo feature**, **không**
gộp thành "God autoload".

- **EventBus** (`core/events/event_bus.gd`, autoload node `EventBus`): pub/sub toàn cục. API
  **`emit(event, payload)`** / **`subscribe(event, callback)`** / **`unsubscribe`** / **`is_known`**.
  Danh mục **đóng** = hằng `EVENTS: Array[StringName]` + một `signal <name>(payload)` cho mỗi event;
  `emit`/`subscribe` `assert` event ∈ `EVENTS` ⇒ event chưa đăng ký fail sớm (**chống "God channel"/"event
  chui"**). Dùng signal Godot chuẩn ⇒ tự ngắt kết nối khi subscriber node bị free (chống rò rỉ) + `unsubscribe`
  tường minh. Quy ước: mọi event mang **một** `payload` (Dictionary). Danh mục nền Phase 14 = **một** event
  `scene_changed` (`{to, from}`) — event nghiệp vụ do phase sở hữu feature thêm sau.
- **SceneRouter** (`core/scene/scene_router.gd`, autoload node `SceneRouter`): điều hướng tập trung. API
  **`goto_scene(path)`** (push) / **`back()`** (pop) / `stack_depth()` / `clear_history()` +
  `current_path`/`current_scene`. **Mô hình scene-host**: giữ scene hiện tại làm node con, tráo tại chỗ,
  **`queue_free()` scene cũ** (giải phóng đúng, không giữ tham chiếu — ADR-009). **Transition tối giản** (tráo
  tức thời; hiệu ứng nâng cao = phase UI sau). Path lỗi ⇒ `false` + `push_error` (không ném/không nuốt lỗi).
  Sau đổi scene phát `scene_changed` qua **EventBus** ⇒ feature phản ứng mà không import SceneRouter.
- **Đăng ký:** section `[autoload]` trong `client/project.godot` (`EventBus`/`SceneRouter` — node PascalCase,
  `*` = enabled). Hai autoload **độc lập**, single-responsibility.

Verified (Godot 4.7.1-stable, Windows, cục bộ): `--headless --import --path client` **exit 0**, 0 lỗi/0 warning
script mới, autoload tạo OK. gdUnit4 headless (`runtest.cmd -a res://tests`) **11/11 pass, 0 error/0 failure/0
orphan** — `event_bus_test.gd` (5: emit→nhận payload, unsubscribe ngắt, nối trùng giao 1 lần, danh mục đóng,
autoload hiện diện) + `scene_router_test.gd` (4: goto A→B→back→A + stack đúng + scene A cũ `is_instance_valid`=false
sau 1 frame, back stack rỗng→false, path lỗi→false giữ nguyên, phát `scene_changed` ×2 payload đúng) + smoke 2 giữ
xanh. `reports/` + `.uid` git-ignored (worktree sạch).

## Why

ADR-002: feature không import lẫn nhau; giao tiếp qua Event Bus/signals; điều hướng scene không rải rác; autoload
tối giản một-trách-nhiệm (không God Object — Forbidden Pattern `docs/ai/coding-rules.md` §3). Hai autoload này là
nền phải có trước feature nào (Phase 15–17 + mọi feature publish/subscribe & đổi scene). Danh mục event tài liệu
hoá = "hợp đồng nội bộ client" (Phase file `# Rủi ro`: chống EventBus thành "God channel" ẩn + chống rò rỉ subscriber
+ chống SceneRouter giữ tham chiếu scene cũ).

## Not this

- **`class_name EventBus`/`SceneRouter` trên script autoload:** trùng tên singleton autoload ⇒ Godot
  "hides an autoload singleton". Bỏ `class_name`; truy cập qua **global autoload** (`EventBus.emit(...)`) — vẫn
  static-typed, không warning; test load qua node `/root/EventBus`.
- **Bus chuỗi động (`Dictionary[String, Array[Callable]]`) không danh mục:** đúng nghĩa "God channel" (event
  tuỳ tiện). Chọn **signal khai báo + hằng `EVENTS` đóng** (fail sớm event lạ, tự tài liệu hoá, Godot tự cleanup).
- **`get_tree().change_scene_to_file()`:** thay `current_scene` của cây gốc — quấy rối test runner gdUnit4, khó
  kiểm back stack/cleanup, và **chưa có `run/main_scene`** (boot = Phase 17). Chọn **scene-host thủ công** (kiểm
  soát vòng đời, `queue_free` tường minh, tất định, kiểm thử được).
- **Seed nhiều event nền cho "đủ" (vd `battle_finished`, `player_connected`):** là event nghiệp vụ/mạng thuộc
  phase 15+ ⇒ vi phạm scope + tạo "event chui" (không ai phát). Chỉ seed **`scene_changed`** — event mà chính code
  Phase 14 (SceneRouter) thực sự phát.
- **Tiêm `event_bus` vào SceneRouter cho test:** thêm máy móc + rủi ro warning `UNSAFE_METHOD_ACCESS`. Autoload
  `EventBus` luôn tồn tại lúc chạy ⇒ tham chiếu global trực tiếp; test quan sát qua autoload thật + teardown
  `unsubscribe`.
- **Framework transition/animation:** nợ kỹ thuật để phase UI sau (Phase file `# Technical Debt Review`).
- **Gộp EventBus + SceneRouter thành `GameManager`/`CoreManager`:** God autoload — ADR-002 cấm. Hai autoload độc lập.

Liên quan: ADR-002 (kiến trúc client, event-driven, autoload tối giản), ADR-009 (vòng đời scene/asset). Dùng lại
[[0006-codegen-pipeline-standardized]] (client `src/`, convention GDScript static-typed/tab). Canonical:
`docs/godot/state-and-signals.md` §3.1 (danh mục EventBus) + `docs/godot/scene-architecture.md` §4.1 (SceneRouter).
Kế tiếp: Phase 15 (client NetworkClient + models).
