# `client/` — Godot 4.x Client (GDScript)

> Project Godot của game **2D Idle Squad RPG**. Mở thư mục này bằng Godot editor (`project.godot` nằm ngay đây).

| Mục | Nội dung |
|---|---|
| **Purpose** | Toàn bộ client game: scene, UI, feature module, bản sim combat client-side, cache trạng thái. |
| **Responsibilities** | Trình bày & tương tác người chơi; gọi backend qua `src/core/net`; hiển thị dữ liệu từ config đã cache; chạy sim combat để render (kết quả nhạy cảm do server quyết — ADR-011). |
| **Allowed contents** | Scene `.tscn`, script `.gd`, resource `.tres`, asset đã import, test Godot. |
| **Not allowed** | ❌ Logic nghiệp vụ nhạy cảm quyết định phần thưởng/kinh tế (thuộc server); ❌ hardcode số cân bằng gameplay (dùng config); ❌ secret/khoá ký. |
| **Dependencies** | `shared/contracts` (hợp đồng API), config versioned nhận từ backend. Không phụ thuộc `server/` ở mức source. |
| **Owner** | Client team. |
| **Future expansion** | Thêm feature dưới `src/features/`, addon dưới `addons/`, ngôn ngữ dưới `localization/`. |

## Cấu trúc (SSOT: `../docs/architecture/project-structure.md` §3)

```text
client/
├── project.godot
├── addons/          # Plugin Godot (editor tools, gdUnit4…)
├── src/
│   ├── core/        # Autoload services (net, config, events, state, scene)
│   ├── features/    # Mỗi feature 1 thư mục (scene+script+resource)
│   ├── combat/      # Deterministic sim (thuần, không UI) — bản client
│   ├── ui/          # UI dùng chung: theme, widget, layout landscape
│   ├── data/        # Resource models map từ config schema
│   └── shared/      # Tiện ích chung client (math fixed-point, result types)
├── assets/          # Asset đã import (art/audio/vfx/fonts)
├── localization/    # File dịch runtime (.csv/.po)
├── tests/           # Test Godot (gdUnit4)
└── export_presets.cfg
```

## Liên kết
- Thiết kế scene/autoload: `../docs/godot/scene-architecture.md`
- State & signals: `../docs/godot/state-and-signals.md`
- Combat determinism: `../docs/adr/ADR-011-combat-authority-and-determinism.md`
- Test client: `../docs/testing/godot-testing.md`
