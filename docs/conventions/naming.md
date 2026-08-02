# Naming Conventions (Quy ước đặt tên)

> Áp dụng cho cả client (Godot/GDScript) và backend (.NET/C#) + dữ liệu/asset. Dùng thuật ngữ domain theo `../mvp/12-glossary.md`.

---

## 1. Bảng tổng nhanh

| Đối tượng | Quy ước | Ví dụ |
|---|---|---|
| Folder | `snake_case` (client), `PascalCase` (project .NET) | `features/hero`, `GameTeam.Domain` |
| GDScript file | `snake_case.gd` | `hero_card.gd` |
| Godot scene | `snake_case.tscn` | `hero_card.tscn` |
| Godot Resource file | `snake_case.tres` | `hero_fire_001.tres` |
| C# file | `PascalCase.cs` (1 type/file) | `StartBattleCommand.cs` |
| Class/Type (GDScript `class_name`) | `PascalCase` | `HeroCard` |
| Class/Type (C#) | `PascalCase` | `BattleResult` |
| Biến/hàm GDScript | `snake_case` | `current_hp`, `apply_damage()` |
| Biến/hàm C# | `camelCase`/`PascalCase` | `currentHp`, `ApplyDamage()` |
| Hằng | `CONSTANT_CASE` | `MAX_TEAM_SIZE` |
| Enum | `PascalCase` (giá trị PascalCase) | `HeroRole.Tank` |
| Node trong scene | `PascalCase` | `HealthBar`, `SkillButton` |
| Signal | `snake_case`, thể quá khứ/sự kiện | `battle_finished` |
| Config file | `snake_case.json` | `banner_standard.json` |
| Asset | `snake_case` + hậu tố loại | `hero_ignis_idle.png` |

---

## 2. Folder naming
- **Client**: `snake_case`, số ít cho feature (`hero`, `battle`, `summon`).
- **Backend**: theo project `PascalCase` (`GameTeam.Application`), thư mục feature-folder trong Application dùng `PascalCase` (`Battles/`, `Heroes/`).
- Không viết tắt mơ hồ; ưu tiên tên đầy đủ.

## 3. File naming
- GDScript/scene/resource: `snake_case`.
- C#: `PascalCase`, **một public type mỗi file**, tên file = tên type.
- Test: `<Subject>Tests.cs` (BE), `test_<subject>.gd` hoặc theo framework (client).

## 4. Scene naming (Godot)
- File `snake_case.tscn`; scene gốc trùng tên feature/widget (`hero_card.tscn` → root node `HeroCard`).
- Scene tái sử dụng đặt trong `ui/` hoặc `features/<f>/`; scene toàn màn hình hậu tố `_screen` (`summon_screen.tscn`).

## 5. Script naming (Godot)
- Mỗi script gắn scene đặt cùng tên (`hero_card.gd` ↔ `hero_card.tscn`).
- Khai báo `class_name PascalCase` khi cần dùng như type.
- Script thuần logic (không scene) đặt theo vai trò (`fixed_point.gd`, `combat_sim.gd`).

## 6. Signal naming (Godot)
- `snake_case`, mô tả **sự việc đã/đang xảy ra**, không phải mệnh lệnh: `hero_selected`, `battle_finished`, `energy_changed`.
- Tránh signal chung chung (`updated`, `changed` không ngữ cảnh).
- Tài liệu hoá signal công khai của feature (tránh Event Bus thành "kênh ngầm" — `../architecture/dependency-graph.md`).

## 7. Node naming (Godot)
- `PascalCase`, mô tả vai trò UI/logic: `HealthBar`, `TeamSlotContainer`.
- Không đặt tên theo kiểu node (`Panel2`, `Button3`).

## 8. Resource naming (Godot)
- `.tres` `snake_case`, tiền tố theo loại + id ổn định: `hero_<id>.tres`, `skill_<id>.tres`.
- Id trùng với id trong config (liên kết data-driven — ADR-004).

## 9. Asset naming
- `snake_case`, cấu trúc `<domain>_<name>_<variant>.<ext>`: `hero_ignis_idle.png`, `sfx_ui_click.wav`, `vfx_fire_burst.png`.
- Thư mục theo loại (`art/`, `audio/`, `vfx/`, `fonts/`) — `../architecture/project-structure.md`.

## 10. Config naming
- `snake_case.json`; tên phản ánh nội dung + id ổn định (`banner_standard.json`, `stage_ch01_05.json`).
- Khoá JSON `snake_case` (xem `data-and-docs-conventions.md`).
- Id là **stable key**, không đổi khi rename hiển thị.

## 11. Backend đặc thù (C#)
- Command/Query: `<Verb><Noun>Command`/`Query` (`StartBattleCommand`, `GetHeroListQuery`).
- Handler: `<Command/Query>Handler`.
- DTO: `<Noun>Dto`/`<Noun>Request`/`<Noun>Response`.
- Interface: tiền tố `I` (`IHeroRepository`, `IConfigProvider`).
- Entity/Aggregate: danh từ domain (`Hero`, `PlayerProfile`, `BattleRecord`).

## 12. Versioning tên
- API path: `/api/v{major}/...` (ADR-008).
- Config bundle: `config@v<N>`; schema: `schema_version` field.
- Không nhét version vào tên file source; version ở metadata/path.

## 13. Liên kết
- Code style: `code-style.md`
- JSON/config: `data-and-docs-conventions.md`
- Git: `git-conventions.md`
