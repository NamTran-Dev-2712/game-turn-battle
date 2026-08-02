# ADR-002: Godot Architecture (Kiến trúc client)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: ADR-001, ADR-004, ADR-009, `../godot/`

## Context
Client cần mở rộng nhiều feature (hero, summon, battle, campaign, inventory...) suốt 5+ năm, nhiều AI agent/dev cùng làm, không tạo God Object/giant manager (đề bài). Cần low coupling, high cohesion, testable (`../mvp/08`, `../mvp/09` SC1).

## Decision
Áp dụng kiến trúc client:
1. **Feature-based modularization** — mỗi feature một thư mục tự chứa (scene+script+resource).
2. **Composition over inheritance** — dùng node/thành phần ghép, tránh cây kế thừa sâu.
3. **Autoload tối giản, một-trách-nhiệm** — chỉ dịch vụ nền (NetworkClient, ConfigProvider, EventBus, SceneRouter, StateCache). Không autoload "God".
4. **Event-driven** — feature giao tiếp qua Event Bus/signals, không import chéo.
5. **Data-driven** — dữ liệu dạng `Resource` (ADR-004).
6. **UI tách khỏi logic** — view-model/presenter; UI không gọi network trực tiếp.

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Một vài "Manager" toàn cục lớn | Trở thành God Object, coupling cao — đề bài cấm |
| Kế thừa sâu (Hero → FireHero → ...) | Cứng nhắc, khó mở rộng; dùng composition + data thay thế |
| MVC nặng nề | Quá tầng cho Godot; dùng feature module + view-model gọn hơn |

## Trade-offs
- **Được:** dễ thêm/xoá feature, test từng phần, AI context gọn theo feature.
- **Mất:** cần kỷ luật ranh giới (dễ lạm dụng Event Bus); nhiều file nhỏ hơn.

## Consequences
- Cấu trúc `client/src/{core,features,combat,ui,data,shared}` (`../architecture/project-structure.md`).
- Quy ước signal/node/scene ở `../conventions/naming.md`; chi tiết ở `../godot/`.
- Combat sim tách riêng, thuần, deterministic (ADR-011).
- Cần review chống import chéo feature (`../ai/review-and-dod.md`).
