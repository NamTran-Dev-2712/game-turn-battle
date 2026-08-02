# ADR-001: Engine Choice (Chọn engine client)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: `../mvp/00-project-overview.md`, `../mvp/08-technical-impact.md`, ADR-002

## Context
Cần engine cho client mobile 2D Idle Squad RPG, landscape, live-service 5+ năm (`../mvp/00`). Ràng buộc dự án đã nêu: **Godot Engine 4.x + GDScript**. Combat full-auto, nhiều UI, yêu cầu hiệu năng mobile tầm trung (`../mvp/08` §3). Đội nhỏ + AI-assisted (`../mvp/09` SD/AI).

## Decision
Dùng **Godot Engine 4.x** với **GDScript** làm ngôn ngữ chính cho client.

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Unity (C#) | Ngoài ràng buộc dự án; chi phí license/biến động; dự án đã chốt Godot |
| Godot + C# | GDScript được chỉ định; C# thêm phức tạp toolchain mobile export; giữ 1 ngôn ngữ client cho AI đơn giản |
| Cocos/tự viết | Thiếu hệ sinh thái, tốn công nền tảng |

## Trade-offs
- **Được:** mã nguồn mở, không phí license, editor nhẹ, iterate nhanh, tích hợp tốt cho 2D, hợp đội nhỏ; GDScript dễ cho AI sinh & đọc.
- **Mất:** hệ sinh thái/tooling nhỏ hơn Unity; C#-only libs không dùng trực tiếp; một số tính năng 4.x còn trưởng thành dần (`../mvp/09` TE5).

## Consequences
- Kiến trúc client theo Godot best practices: composition (node), Resource cho data-driven, signals (ADR-002).
- Rendering/animation ở client; **không** đặt logic nhạy cảm ở client (ADR-011).
- Cần theo dõi bản Godot 4.x ổn định; ghim version trong `project.godot` & CI.
- Toolchain export mobile (Android trước, iOS sau — `../mvp/10` DP1) thiết lập ở `../deployment/`.
