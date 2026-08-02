# ADR-010: Dependency Management (Quản lý phụ thuộc/package)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: ADR-002, ADR-003, `../conventions/`, `../deployment/`

## Context
Dự án 2 phía (Godot + .NET), 5+ năm, nhiều dev/AI. Cần quản lý phụ thuộc **nhất quán, tái lập, an toàn** (tránh "works on my machine", tránh third-party rủi ro/lệch version — `../mvp/09` AI/TE).

## Decision
**Backend (.NET):**
- **Central Package Management** (`Directory.Packages.props`) — ghim version tập trung.
- `Directory.Build.props` bật `nullable`, analyzers, warning-as-error mức hợp lý.
- Chỉ thêm package qua PR có lý do; ưu tiên thư viện đã chốt (MediatR, EF Core, FluentValidation, Serilog...).

**Client (Godot):**
- Plugin/addon để trong `client/addons/`, **commit** kèm ghi chú version & license.
- Ghim version Godot trong `project.godot` + CI dùng đúng version.
- Hạn chế addon bên thứ ba; ưu tiên tính năng lõi Godot.

**Chung:**
- `third_party/` cách ly mã ngoài + theo dõi license.
- Renovate/dependabot (Post-bootstrap) để cập nhật có kiểm soát.
- **Không** thêm phụ thuộc chỉ để tiện lợi nhỏ (giảm bề mặt rủi ro).

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Version rải rác trong từng .csproj | Lệch version, khó bảo trì |
| Kéo addon không commit | Không tái lập build |
| Tự do thêm package | Bề mặt rủi ro/bảo mật tăng |

## Trade-offs
- **Được:** build tái lập, version nhất quán, kiểm soát license/bảo mật.
- **Mất:** thêm thủ tục khi nâng cấp; ít "tiện tay".

## Consequences
- File `Directory.Packages.props`/`Directory.Build.props` ở `server/` (`../architecture/project-structure.md`).
- Quy trình thêm dependency ghi ở `../conventions/` & review (`../ai/review-and-dod.md`).
- CI kiểm version & audit (`../deployment/ci-cd-pipeline.md`).
