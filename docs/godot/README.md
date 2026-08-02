# Godot Client Design (Godot 4.x — GDScript)

> Thiết kế client theo Godot best practices: feature-based, composition, autoload tối giản, event-driven, data-driven. Chi tiết quyết định: ADR-002. Client **không** là nguồn sự thật (ADR-007/011).

## Danh mục
| File | Nội dung |
|---|---|
| [scene-architecture.md](scene-architecture.md) | Scene, composition, feature module, scene transition, autoload |
| [state-and-signals.md](state-and-signals.md) | State management, signals, Event Bus, network client |
| [resources-and-assets.md](resources-and-assets.md) | Resource data-driven, asset loading, memory management |
| [ui-architecture.md](ui-architecture.md) | UI layer, landscape, view-model |
| [tooling-and-testing.md](tooling-and-testing.md) | Debug tools, editor tools, plugin strategy, testing |

## Nguyên tắc (vì sao theo Godot best practices)
| Nguyên tắc | Godot best practice |
|---|---|
| Composition over inheritance | Node/scene ghép lại; tránh kế thừa sâu |
| Feature-based | Scene+script+resource cùng chỗ → module hoá |
| Autoload tối giản | Chỉ dịch vụ nền; tránh God autoload |
| Signals/Event Bus | Giao tiếp lỏng, giảm coupling |
| Resource data-driven | `.tres`/custom Resource làm dữ liệu (ADR-004) |
| Tách UI khỏi logic | View-model; UI không gọi network |

## Nguồn
- Hàm ý kỹ thuật client: `../mvp/08`. Hiệu năng: `../mvp/09` PF.
