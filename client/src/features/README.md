# `client/src/features/` — Feature modules

> Mỗi feature là **một thư mục tự chứa** (scene + script + resource). Feature **không import chéo nhau** — giao tiếp qua EventBus/signals (`../core/events`).

| Mục | Nội dung |
|---|---|
| **Purpose** | Chứa các module nghiệp vụ hiển thị của client. |
| **Responsibilities** | UI/flow của từng feature; gọi backend qua `core/net`; đọc dữ liệu qua `core/config` & `core/state`. |
| **Allowed** | Scene/script/resource riêng của feature. |
| **Not allowed** | ❌ import trực tiếp feature khác; ❌ quyết định phần thưởng/kinh tế client-side; ❌ hardcode số cân bằng. |
| **Dependencies** | `core/*`, `ui/`, `data/`, `shared/`. |
| **Owner** | Client feature squads. |
| **Future expansion** | Thêm feature = thêm thư mục con + README, đăng ký route ở `core/scene`. |

## Feature MVP (tạo ở phase Implementation)
| Feature | Doc nghiệp vụ |
|---|---|
| `hero/` | `../../../docs/gameplay/hero-system.md` |
| `summon/` | `../../../docs/gameplay/configuration-and-data.md` (gacha) · `mvp/03` |
| `battle/` | `../../../docs/gameplay/combat-framework.md` |
| `campaign/` | `../../../docs/gameplay/progression-and-economy.md` |
| `inventory/` | `../../../docs/gameplay/inventory-and-equipment.md` |
| `equipment/` | `../../../docs/gameplay/inventory-and-equipment.md` |
| `quest/` | `../../../docs/gameplay/quest-system.md` |
| `mail/` | `../../../docs/liveops/mail-system.md` |
| `shop/` | `../../../docs/gameplay/progression-and-economy.md` |
| `formation/` | `../../../docs/gameplay/combat-framework.md` |

> Các thư mục feature dưới đây hiện là **placeholder** (chỉ README). KHÔNG hiện thực gameplay ở phase bootstrap.
