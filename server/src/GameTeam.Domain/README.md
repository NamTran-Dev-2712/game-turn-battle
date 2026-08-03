# `GameTeam.Domain` — Domain layer

Entity/Aggregate/Value Object/Domain Service/Domain Event, invariant, business rule **thuần**. **Không** phụ thuộc EF/HTTP/framework/`DateTime.Now` (inject qua port). **Owner:** Backend. Thiết kế: `../../../docs/backend/domain-and-application.md`, ADR-003. **Bootstrap:** chỉ `AssemblyMarker`.
