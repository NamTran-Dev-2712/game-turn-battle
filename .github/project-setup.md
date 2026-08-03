# GitHub Project / Milestones / Discussions — Hướng dẫn thiết lập

> Cấu hình phía GitHub (không tự động hoá ở bootstrap). Làm một lần khi khởi tạo tổ chức repo.

## Project board (khuyến nghị)
- 1 board kiểu **Table + Board view**, cột: `Backlog → Ready → In progress → In review → Done`.
- Trường tuỳ biến: `Phase` (P0–P7), `Area` (client/server/platform/design), `Priority` (Must/Should/Could — MoSCoW `docs/mvp/01`).

## Milestones
Tạo theo roadmap `docs/roadmap/README.md`:
`P0 Bootstrap`, `P1 Core Framework`, `P2–P3 Gameplay`, `P4–P5 Backend Integration`, `P6 LiveOps`, `P7 Polish/Release`.

## Discussion categories
- 📣 Announcements · 💡 Ideas · 🙋 Q&A · 🏗️ Architecture (trỏ ADR) · 🎮 Design (trỏ mvp/).

## Definition of Ready / Done
- **Ready:** có mục tiêu WHY, liên kết SSOT/ADR, acceptance, phạm vi rõ (issue template `feature_request.yml`).
- **Done:** theo `docs/ai/review-and-dod.md` §4.

## Branch protection (khuyến nghị)
- `main`/`dev`: yêu cầu PR + CI xanh (`ci-server`, `ci-client`, `validate-config`) + review từ CODEOWNERS; squash-merge (`docs/conventions/git-conventions.md`).
