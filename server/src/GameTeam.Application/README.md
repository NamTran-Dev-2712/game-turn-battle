# `GameTeam.Application` — Application layer (CQRS/MediatR)

Command/Query + MediatR handler, **ports** (interface), validators, pipeline behaviors. Tổ chức **theo feature-folder** (không theo loại file). **Owner:** Backend.

## Cấu trúc (docs/backend/solution-structure.md §3)
```text
Common/            # Behaviors/, Ports/, Results/ (dùng chung)
Heroes/  Battles/  Summons/  Campaigns/  Economy/  Progression/
```
**Quy tắc:** chỉ thấy **interface** của Infrastructure (DIP); KHÔNG `ProjectReference` tới Infrastructure. **Bootstrap:** thư mục feature chỉ có README; `DependencyInjection.AddApplication()` wiring MediatR/FluentValidation rỗng.
