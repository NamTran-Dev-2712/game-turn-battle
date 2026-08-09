# Architecture Decision Records (ADR)

> ADR ghi lại **các quyết định kiến trúc quan trọng** cùng bối cảnh và hệ quả, để người & AI agent hiểu **vì sao** hệ thống được thiết kế như vậy — và không vô tình đảo ngược quyết định.

---

## 1. ADR là gì & khi nào tạo

- ADR = một quyết định kiến trúc **có hệ quả lâu dài** (khó/đắt để đảo ngược).
- Tạo ADR mới khi: chọn công nghệ, đổi ranh giới module, đổi chiến lược dữ liệu/mạng/lưu trữ, hoặc bất kỳ quyết định ảnh hưởng nhiều module.
- **Không** sửa nghĩa một ADR đã "Accepted"; thay vào đó tạo ADR mới **Supersedes** nó.

## 2. Trạng thái ADR

`Proposed` → `Accepted` → (`Deprecated` | `Superseded by ADR-XXX`).

## 3. Template

```markdown
# ADR-XXX: <Tiêu đề>
- Status: Accepted
- Date: YYYY-MM-DD
- Deciders: <vai trò>
- Related: <ADR/doc liên quan>

## Context
Bối cảnh, ràng buộc, nguồn (tham chiếu docs/mvp/*).

## Decision
Quyết định cụ thể.

## Alternatives
Các phương án đã cân nhắc.

## Trade-offs
Đánh đổi (được gì / mất gì).

## Consequences
Hệ quả tích cực/tiêu cực, việc phải làm tiếp.
```

## 4. Danh mục ADR

| ADR | Tiêu đề | Status | Chốt điều gì |
|---|---|---|---|
| [ADR-001](ADR-001-engine-choice.md) | Engine Choice | Accepted | Godot 4.x + GDScript (client) |
| [ADR-002](ADR-002-godot-architecture.md) | Godot Architecture | Accepted | Feature-based + composition + autoload tối giản |
| [ADR-003](ADR-003-backend-architecture.md) | Backend Architecture | Accepted | Clean Architecture + CQRS/MediatR |
| [ADR-004](ADR-004-data-driven-design.md) | Data-Driven Design | Accepted | Gameplay = dữ liệu, không code |
| [ADR-005](ADR-005-configuration-strategy.md) | Configuration Strategy | Accepted | Configuration Service + versioning |
| [ADR-006](ADR-006-liveops.md) | LiveOps Foundation | Accepted | Remote config/flags/schedule (chừa chỗ) |
| [ADR-007](ADR-007-save-strategy.md) | Save Strategy | Accepted | Server-authoritative save + versioning |
| [ADR-008](ADR-008-networking.md) | Networking | Accepted | Online-required, REST + SignalR optional |
| [ADR-009](ADR-009-asset-loading.md) | Asset Loading | Accepted | Chiến lược nạp/giải phóng asset |
| [ADR-010](ADR-010-dependency-management.md) | Dependency Management | Accepted | Quản lý package/addon 2 phía |
| [ADR-011](ADR-011-combat-authority-and-determinism.md) | Combat Authority & Determinism | Accepted | Server-authoritative + deterministic re-sim |

## 5. Liên kết
- Yêu cầu nghiệp vụ (SSOT): [`../mvp/`](../mvp/) · 3 quyết định chặn R1–R3: [`../mvp/14-readiness-checklist.md`](../mvp/14-readiness-checklist.md) → chốt ở ADR-007/008/011.
- Kiến trúc: [`../architecture/`](../architecture/) · Conventions: [`../conventions/`](../conventions/) · Master index: [`../README.md`](../README.md)
