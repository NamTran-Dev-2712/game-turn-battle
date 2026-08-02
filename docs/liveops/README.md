# LiveOps Design

> Thiết kế nền LiveOps: remote config, feature flags, A/B, scheduling (event/banner/shop/season), mail, content update & admin workflow. Nền: ADR-005 (config), ADR-006 (liveops). Tách rõ MVP vs Post-MVP theo `../mvp/07`.

## Danh mục
| File | Nội dung | MVP? |
|---|---|---|
| [remote-config.md](remote-config.md) | Remote configuration foundation | Nền MVP (data-driven) |
| [feature-flags-and-ab-testing.md](feature-flags-and-ab-testing.md) | Feature toggle + A/B testing | Flag cơ bản MVP; A/B Post |
| [content-scheduling.md](content-scheduling.md) | Events, banner, shop rotation, season | Post-MVP (schema chừa chỗ) |
| [mail-system.md](mail-system.md) | Mail rewards | MVP cơ bản |
| [content-update-and-admin-workflow.md](content-update-and-admin-workflow.md) | Quy trình cập nhật nội dung & admin | Workflow MVP → CMS Post |

## Nguyên tắc
- LiveOps sống nhờ **data-driven + config versioned** (ADR-004/005).
- MVP **chừa hook** (start/end/version, flag) nhưng chưa bật đầy đủ (ADR-006).
- Scheduling dựa **server time** (`../mvp/08`).
- Telemetry là điều kiện để LiveOps ra quyết định (`../mvp/09` LO2).

## Nguồn
- `../mvp/07-liveops-planning.md` (SSOT).
