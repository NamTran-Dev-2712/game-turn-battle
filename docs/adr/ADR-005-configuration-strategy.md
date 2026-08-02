# ADR-005: Configuration Strategy (Chiến lược cấu hình)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: ADR-004, ADR-006, `../liveops/remote-config.md`, `../mvp/07`, `../mvp/08`

## Context
ADR-004 yêu cầu gameplay data-driven. Cần phân biệt **data-driven ở MVP** (đọc config khi build/khởi động) với **live-config Post-MVP** (đổi từ server không update app) — `../mvp/07` §4. Config sẽ đổi liên tục khi live → cần **versioning & migration an toàn** (`../mvp/08` TE4).

## Decision
Xây **Configuration Service** ở backend là **nguồn sự thật runtime** cho config:
- Config author-time nằm ở `config/` (Git) → pipeline **validate (JSON Schema) → version → publish** thành **bundle bất biến có version** (vd `config@v42`).
- Backend phân phối bundle versioned cho client; client **cache** theo version, chỉ tải khi có version mới.
- Backend đọc config qua **provider interface** (không đọc file trực tiếp trong Domain/Application).
- **Schema versioning**: mỗi thay đổi schema tăng version + có migration/compat rule (`_versions/`).
- MVP: publish bundle khi deploy; Post-MVP: đổi bundle "live" không cần build client (đặt nền sẵn).

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Config nhúng trong client build | Không đổi được khi live; ngược ADR-004 |
| Đọc file config rải rác trong code | Không kiểm soát version/validation |
| Third-party remote config (Firebase...) | Cân nhắc sau; MVP tự chủ để kiểm soát & tránh phụ thuộc sớm (`../mvp/10` LO2) |

## Trade-offs
- **Được:** một nguồn sự thật, versioned, validate được, sẵn sàng LiveOps.
- **Mất:** hạ tầng phân phối + cache + versioning phức tạp hơn; cần pipeline.

## Consequences
- `Configuration Service` trong Infrastructure + cache Redis (`../backend/infrastructure.md`).
- Client `ConfigProvider` autoload cache theo version (`../godot/resources-and-assets.md`).
- Feature flags & schedule dựa trên nền này (ADR-006).
- CI validate config trước publish (`../deployment/ci-cd-pipeline.md`).
