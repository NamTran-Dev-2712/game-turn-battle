# 0020 — Client config bundle end-to-end standardized (Phase 22)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-08-28, Godot 4.7.1-stable Windows). **ĐÓNG P1 — nền data-driven
  end-to-end sẵn sàng cho gameplay.**
- **Bối cảnh:** Phase 16 dựng `ConfigProvider` client với GIẢ ĐỊNH `data` là mảng theo type + endpoint placeholder
  `/api/v1/config/version` + `?version=`. Phase 21 dựng Configuration Service THẬT phát `data` **map theo id**
  (`data.{type}.{id}=entry`) qua `GET /api/v1/config/current` + `GET /api/v1/config/bundle?bundleVersion=N`. Phase 22 nối
  hai đầu: sửa client khớp hợp đồng server thật + màn mẫu chứng minh vòng (ADR-004/005/009).

## Vấn đề phát hiện khi inspect (không phải feature greenfield)

- **Lệch hình dạng `data` (nghiêm trọng):** `ConfigProvider._build_index` chỉ xử lý mảng `data.{type}=[…]`; server phát
  **map** `data.{type}.{id}=entry` ⇒ với bundle server thật client index RỖNG. Bằng chứng:
  `ConfigEndpointTests.cs` khẳng định `doc["data"]["hero"]["hero_sample"]["base_stats"]["hp"]`.
- **Sai wire endpoint:** `check_for_update()` gọi `/api/v1/config/version` + `?version=`; endpoint thật (Phase 21) là
  `/config/current` + `?bundleVersion=` (param `bundleVersion` — KHÔNG `version`, trùng token `{version:apiVersion}`).
- **Fallback im lặng:** `check_for_update()` trả `false` trơ; Rule E yêu cầu fallback KHÔNG im lặng (log + trạng thái + retry).

## Quyết định (user-approved)

- **Màn mẫu = màn hero riêng** (`src/ui/hero_list/`, `HeroListView`+`HeroListPresenter`), điều hướng từ nút "Anh hùng" của
  hub qua `SceneRouter` (thay vì nhét section vào hub). Đọc `ConfigProvider.get_all(&"hero")` — data-driven, KHÔNG hardcode,
  KHÔNG phải Hero System (feature thật = phase 27).
- **Fallback KHÔNG thêm event EventBus** — danh mục ĐÓNG như Phase 17/20. `check_for_update()` trả **status dict**
  `{updated, used_fallback, error_code, has_config}`; `ConfigProvider` lộ `is_stale()`/`last_error_code()`; boot `push_warning`;
  màn feature hiện banner stale + nút Thử lại. Tái dùng `config_updated` sẵn có.
- **Seed config thật tối thiểu** (`config/heroes/hero_sample.json` + `config/skills/skill_sample_basic.json`, stats **số 0 —
  KHÔNG balance**, y như seed của `ConfigEndpointTests`) để server thật phát `data.hero` khác rỗng cho demo e2e. Qua
  config-validator (hero→skill referential integrity) exit 0.

## Thay đổi chính

- `config_provider.gd`: `_build_index` chấp nhận **cả map (server) lẫn mảng (fixture cũ)**, index theo `entry.id`; endpoint thật
  `CONFIG_CURRENT_PATH`/`?bundleVersion=`; `check_for_update()` trả status dict + đánh dấu stale (`_stale`/`_last_error_code`) +
  `push_warning` khi fallback; thêm `is_stale()`/`last_error_code()`; `apply_bundle` xoá cờ stale khi thành công.
- `boot_controller.gd`: bắt status config; `used_fallback` ⇒ `push_warning` "dùng cache cũ" (config vẫn best-effort, KHÔNG chặn
  boot — giữ offline-view Phase 20).
- `main_hub_presenter.gd`: intent `heroes` → `SceneRouter.goto_scene(hero_list.tscn)` (inject `_scene_router`).
- Mới: `hero_list_view.gd` (BaseView, network-free, banner stale + Retry/Back, dựng danh sách động từ data), `hero_list_presenter.gd`
  (đọc ConfigProvider, retry→`check_for_update`→refresh, refresh khi `config_updated`), `hero_list.tscn`.
  Presenter dùng literal intent `&"retry"`/`&"back"` (KHÔNG tham chiếu lớp `HeroListView`) ⇒ tránh phụ thuộc vòng class_name.

## Verify (cục bộ)

- config-validator `bash tools/config-validator/run.sh config shared/config-schema` → **exit 0** (2 file hợp lệ, hero→skill OK).
- Godot `--headless --import` → exit 0, 0 error/0 warning; `HeroListView`/`HeroListPresenter` đăng ký.
- gdUnit4 headless (`--ignoreHeadlessMode`) toàn bộ **76/76 pass, 0 error/0 failure/0 orphan** (config_provider +7 test mới:
  map-shape, endpoint thật, fallback/stale, no-cache; hero_list_presenter 5 test: nhận→query→hiển thị, version bump, lỗi→fallback,
  no-cache→retry, back).
- Grep guard: `HTTPRequest`/`core/net`/`NetworkClient` KHÔNG xuất hiện dưới dạng code trong `src/ui/hero_list/` (chỉ trong comment).
- Không drift `client/src/data/generated` / `shared/contracts/openapi.json` (không đổi contract).
- **Godot binary cục bộ:** `D:\Godot_v4.7.1-stable_win64.exe\Godot_v4.7.1-stable_win64.exe` (thư mục cùng tên chứa exe); chạy qua
  PowerShell (Git Bash không exec được path này).

## Ranh giới / Nợ (Post-MVP)

- Signed/secure bundle + cryptographic verify + advanced LiveOps + live swap không-deploy = **Post-MVP** (không mở rộng scope).
- Hero System thật / combat / skill logic = phase 27+ / nhóm 5 (màn hero ở đây chỉ là mẫu đọc config).
- Demo server thật cần Docker (Postgres+Redis) — nếu môi trường không có, gdUnit4 mock là bằng chứng tự động chuẩn.

Liên qu: [[0014-client-configprovider-statecache-standardized]] · [[0019-config-service-standardized]] ·
[[0018-client-auth-profile-standardized]]. CLAUDE.md §4.6 (block Phase 22) + doc-sync matrix.
