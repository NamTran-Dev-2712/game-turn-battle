# `core/config/` — ConfigProvider

Autoload (`config_provider.gd`, bỏ `class_name` — truy cập qua global `ConfigProvider`) nhận & cache **config bundle versioned** từ backend (ADR-005); **cửa đọc config DUY NHẤT** — feature **không** tự tải/nạp raw bundle, **không** hardcode số gameplay. **Owner:** Client core.

- `apply_bundle(bundle)`: cache envelope `config@vN` **BẤT BIẾN** ra `user://config_cache/config@vN.json` (**ghi-một-lần**, không ghi đè version cũ) + con trỏ `active.json`; phát `config_updated` khi active version đổi.
- Boot `_ready()` nạp lại từ đĩa (offline-view; thiếu/hỏng → rỗng, không crash).
- Truy vấn data-driven (trả **bản sao**): `get_entry(type,id)`, `get_all(type)`, `get_hero(id)`, `current_version()`, `config_label()`, `has_config()`.
- `check_for_update()` (coroutine) so version qua `NetworkClient` → tải bundle mới (endpoint `/api/v1/config/...` placeholder; Config Service = phase 21, e2e = phase 22).

Canonical: `../../../../docs/godot/resources-and-assets.md` §1.1, `../../../../docs/adr/ADR-005-configuration-strategy.md`. Test: `../../../tests/core/config/`.
