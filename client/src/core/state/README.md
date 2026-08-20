# `core/state/` — StateCache

Autoload (`state_cache.gd`, bỏ `class_name` — truy cập qua global `StateCache`) cache trạng thái **đọc-chỉ / CHỈ HIỂN THỊ** phục vụ hiển thị + offline-view (`const IS_DISPLAY_ONLY = true`). Nguồn sự thật là server (ADR-007); client chỉ cache. **Owner:** Client core. **Không** coi cache là nguồn quyết định nhạy cảm, **không** mutate chân lý.

- Đường ghi **DUY NHẤT**: `apply_snapshot(snapshot)` — thay toàn bộ cache bằng snapshot từ **server response**; phát `state_refreshed`. **Không** có mutator chân lý (không `add_currency`/`spend_currency`/`set_progress`…).
- Đọc (trả **bản sao**): `get_currency(code)`, `get_currencies()`, `get_heroes()`, `get_hero(id)`, `get_progress(key)`, `get_all_progress()`, `get_profile()`.
- Nhãn nguồn: `source()` (`empty｜server｜cache`), `is_offline()`. Boot nạp `user://state_cache/snapshot.json` với nhãn `cache` (offline/cũ) tới khi server refresh.

Mutation đi qua: `Feature/UI → NetworkClient → Server → response → StateCache.apply_snapshot`.

Canonical: `../../../../docs/godot/state-and-signals.md` §1.1, `../../../../docs/adr/ADR-007-save-strategy.md`. Test: `../../../tests/core/state/`.
