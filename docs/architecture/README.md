# Architecture (Kiến trúc tổng thể)

> Bức tranh kiến trúc tổng: tổng quan hệ thống, cấu trúc repo (layout SSOT), đồ thị phụ thuộc module, và thứ tự hiện thực. Đây là tầng **blueprint kiến trúc** — dựa trên SSOT nghiệp vụ `../mvp/`, chi tiết quyết định ở `../adr/`.

## Danh mục
| File | Nội dung |
|---|---|
| [overview.md](overview.md) | Tổng quan hệ thống: client/server/shared, luồng dữ liệu, ranh giới lớn |
| [project-structure.md](project-structure.md) | **Layout repo SSOT** — cây thư mục đầy đủ + WHY từng thư mục |
| [dependency-graph.md](dependency-graph.md) | Đồ thị phụ thuộc module & hướng phụ thuộc (dependency rule) |
| [implementation-order.md](implementation-order.md) | Thứ tự kỹ thuật hiện thực module (S0–S13) khớp roadmap |

## Nguyên tắc chung
- `project-structure.md` là **nguồn sự thật về layout** — mọi thư mục/tệp mới phải khớp; lệch phải được giải thích và cập nhật vào đây (xem [roadmap phase 01](../roadmap/01-repo-structure-conventions.md)).
- Hướng phụ thuộc luôn vào trong (Domain thuần) — chi tiết `dependency-graph.md`, ADR-003.
- Kiến trúc dựa trên SSOT `../mvp/`, không phát minh yêu cầu.

## Liên kết
- SSOT nghiệp vụ: [`../mvp/`](../mvp/) · Quyết định: [`../adr/`](../adr/)
- Đặt tên & code style: [`../conventions/`](../conventions/)
- Thực thi layout & conventions: [`../roadmap/01-repo-structure-conventions.md`](../roadmap/01-repo-structure-conventions.md)
