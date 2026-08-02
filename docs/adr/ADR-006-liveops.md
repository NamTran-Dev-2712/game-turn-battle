# ADR-006: LiveOps Foundation (Nền tảng LiveOps)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: ADR-004, ADR-005, `../liveops/`, `../mvp/07`

## Context
`../mvp/07` tách rõ: MVP **chưa** làm event/banner/season/CMS, nhưng thiết kế phải **chừa chỗ (hooks)** để cắm LiveOps sau mà không refactor lớn. MVP cần Mail + data-driven + daily reset.

## Decision
Đặt **nền LiveOps** dựa trên Configuration Service (ADR-005):
- Mọi "content có thời hạn" mô hình hoá với **start/end time + version** (event, banner, shop rotation, season) — MVP định nghĩa schema, chưa bật.
- **Feature Toggle** để bật/tắt tính năng theo cấu hình (MVP: hạ tầng cờ cơ bản).
- **Mail system** có ngay ở MVP (kênh trao thưởng/đền bù — `../mvp/07` §2).
- **Scheduling** dựa trên **server time** (chống chỉnh giờ — `../mvp/08`).
- **A/B testing & Admin CMS**: Post-MVP, nhưng data model để "chừa chỗ".

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Làm đầy đủ LiveOps ngay MVP | Vượt scope (`../mvp/01` Won't-have) |
| Không chừa hook, làm sau | Refactor lớn (`../mvp/09` LO1/SC1) |

## Trade-offs
- **Được:** MVP gọn nhưng mở rộng LiveOps mượt; đúng scope.
- **Mất:** phải thiết kế schema "dư" một chút ngay từ đầu (start/end/version).

## Consequences
- Schema `config/liveops/` + cờ tính năng (`../liveops/remote-config.md`, `feature-flags-and-ab-testing.md`).
- Mail vào MVP (`../liveops/mail-system.md`).
- Telemetry là điều kiện để LiveOps ra quyết định (Post-MVP sớm — `../mvp/09` LO2).
- Content update workflow ở `../liveops/content-update-and-admin-workflow.md`.
