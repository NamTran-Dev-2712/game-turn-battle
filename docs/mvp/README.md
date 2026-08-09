# MVP — Product Discovery (SSOT nghiệp vụ)

> Chỉ mục điều hướng cho bộ tài liệu **Single Source of Truth (SSOT) nghiệp vụ**. Đây là **nội dung ĐÃ CHỐT, KHÔNG sửa** ở các phase kiến trúc/hiện thực — mọi yêu cầu game bắt nguồn từ đây. README này **chỉ là index**, không thêm/đổi quyết định nghiệp vụ. Điểm mơ hồ → ghi ở [`10-open-questions.md`](10-open-questions.md).

## Danh mục (theo thứ tự đọc)
| File | Nội dung |
|---|---|
| [00-project-overview.md](00-project-overview.md) | Tổng quan dự án, vision, north star |
| [01-mvp-definition.md](01-mvp-definition.md) | Định nghĩa MVP, phạm vi (MoSCoW), tiêu chí chấp nhận |
| [02-core-game-loop.md](02-core-game-loop.md) | Vòng lặp core (idle → battle → progress) |
| [03-core-gameplay.md](03-core-gameplay.md) | Hệ thống gameplay: hero, combat, skill, inventory, quest… |
| [04-feature-analysis.md](04-feature-analysis.md) | Phân tích feature & ưu tiên |
| [05-player-progression.md](05-player-progression.md) | Tiến trình người chơi (level, ascension, power) |
| [06-game-economy.md](06-game-economy.md) | Kinh tế game: currency, gacha, shop, sink/source |
| [07-liveops-planning.md](07-liveops-planning.md) | Kế hoạch LiveOps (event/banner/season) |
| [08-technical-impact.md](08-technical-impact.md) | Hàm ý kỹ thuật client & server |
| [09-risk-analysis.md](09-risk-analysis.md) | Phân tích rủi ro & ưu tiên test |
| [10-open-questions.md](10-open-questions.md) | Câu hỏi mở & quyết định cần chốt |
| [11-development-roadmap.md](11-development-roadmap.md) | Lộ trình phát triển (MVP → live) |
| [12-glossary.md](12-glossary.md) | Thuật ngữ chung (ngôn ngữ domain) |
| [13-assumptions.md](13-assumptions.md) | Giả định nền |
| [14-readiness-checklist.md](14-readiness-checklist.md) | Checklist sẵn sàng + 3 quyết định chặn R1–R3 |
| [15-next-phase.md](15-next-phase.md) | Cách tiêu thụ SSOT ở phase kế |

## Nguyên tắc
- **KHÔNG sửa nội dung SSOT** ở phase kiến trúc/hiện thực; thay đổi nghiệp vụ phải quay lại Discovery.
- Mọi rule/kiến trúc phải **truy vết** về file `mvp/*` tương ứng (traceability).
- Dùng đúng thuật ngữ ở [`12-glossary.md`](12-glossary.md) khi đặt tên domain.

## Liên kết
- Blueprint kiến trúc: [`../architecture/`](../architecture/) · Quyết định: [`../adr/`](../adr/)
- Ngôn ngữ chung: [`12-glossary.md`](12-glossary.md) · Master index: [`../README.md`](../README.md)
