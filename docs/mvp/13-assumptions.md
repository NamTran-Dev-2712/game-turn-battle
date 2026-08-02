# 13 — Giả Định (Assumptions)

> Ghi nhận **mọi giả định** đã dùng khi phân tích, để không có gì ngầm định. Mỗi giả định: nội dung, **cơ sở/WHY**, **mức độ tin cậy**, và **hệ quả nếu sai**. Giả định chưa được xác nhận nên đối chiếu với `10-open-questions.md`.

**Mức tin cậy:** 🟢 đã chốt với chủ dự án · 🟡 mặc định hợp lý (Lead Designer chọn) · 🔴 phỏng đoán cần xác nhận.

---

## 1. Giả định đã chốt với chủ dự án (🟢)

| # | Giả định | WHY / Nguồn | Hệ quả nếu sai |
|---|---|---|---|
| A01 | **North star = Idle Heroes**: idle sâu, nhiều lớp nuôi hero, thiên hardcore/grind | Chủ dự án chọn trực tiếp | Điều chỉnh chiều sâu progression & nhịp kinh tế |
| A02 | **Combat MVP = full-auto**: người chơi chỉ set formation, skill/ultimate tự động; ultimate thủ công là Post-MVP | Chủ dự án chọn trực tiếp | Nếu cần manual → thêm input & UX combat |
| A03 | **Tài liệu chuyên sâu, đầy đủ** (bảng + mermaid + WHY, tách MVP/Post-MVP) | Chủ dự án chọn trực tiếp | — |
| A04 | Engine Godot 4.7.x (client), backend .NET 9, mobile landscape | Thông tin dự án + `project.godot` | Đổi nền tảng → xem lại hàm ý kỹ thuật |

---

## 2. Giả định thiết kế do Lead Designer chọn (🟡)

| # | Giả định | WHY | Hệ quả nếu sai |
|---|---|---|---|
| A05 | Đội hình **đúng 6 hero** | Đề bài nêu rõ "team of exactly six" | — (đã chắc từ đề bài) |
| A06 | MVP chỉ mở **3 lớp nâng hero**: Level (Must), Sao/Ascension (Should), Equipment (Should); các lớp khác Post-MVP | Tránh ngợp & bùng nổ cân bằng (`09` GD4) | Nếu muốn nhiều lớp hơn → tăng scope & rủi ro |
| A07 | **AFK là nguồn tài nguyên nền chính; Energy chỉ tăng tốc cày chủ động** | Giải mâu thuẫn idle vs energy (`06` EC4) | Đổi nguồn chính → tái cân bằng kinh tế |
| A08 | MVP có **≥1 banner gacha cố định** với rate theo rarity + **pity** | Chuẩn thể loại, giữ niềm tin gacha | — |
| A09 | Gacha khi trùng hero trả **fragment/mảnh** để nâng sao | Chuẩn Idle-Heroes-like; tạo near-goal | Đổi cơ chế dup → xem lại kinh tế mảnh |
| A10 | MVP dùng **3 loại tiền tệ nền**: Gold (soft), Gem (premium), Summon Ticket | Tối thiểu đủ vận hành kinh tế | Thêm tiền tệ → mở rộng sau |
| A11 | Trục khắc chế dùng **Faction hoặc Element** (chưa chốt cái nào) — thiết kế "có khắc chế" nhưng chi tiết để mở | Cả hai đều phổ biến; cần chủ dự án chọn (`10` GP3) | Ảnh hưởng team-building & data hero |
| A12 | Lưới **formation đơn giản** (vd 2 hàng × 3) với cơ chế aggro theo vị trí tối giản | Đủ chiều sâu, rẻ để làm ở MVP | Lưới phức tạp hơn → thêm rule combat |
| A13 | **Monetization IAP hoãn Post-MVP**; premium currency vẫn vận hành như thật (kiếm free) | MVP kiểm chứng loop, không cần dòng tiền (`06`) | Nếu cần doanh thu sớm → thêm IAP |
| A14 | **Ranking MVP = leaderboard đơn giản** (power/stage), **không** PvP real-time | Giảm phức tạp cân bằng/chống gian lận (`09`) | PvP sớm → tăng scope lớn |
| A15 | **Tutorial MVP tối giản** dạy loop cốt lõi qua thao tác thật, mở khóa dần | Onboarding quyết định D1 (`02`) | — |

---

## 3. Giả định kỹ thuật (🟡/🔴) — cần Architecture xác nhận

| # | Giả định | Mức | WHY | Hệ quả nếu sai |
|---|---|---|---|---|
| A16 | Hệ nhạy cảm (currency, gacha, AFK) **nên** server-authoritative | 🟡 | Chống gian lận (`08`,`09` BE1) | Nếu client-authoritative → rủi ro gian lận cao |
| A17 | Nội dung cốt lõi **data-driven ngay từ MVP** (chưa cần live-config) | 🟡 | Nền LiveOps & tuning (`06`,`07`) | Nếu hard-code → không tune được |
| A18 | Combat có thể **deterministic theo seed** nếu cần server-verify | 🔴 | Cho phép replay/verify (`08` CB2) | Nếu không determinism → khó verify server |
| A19 | Game **online có đồng bộ** (chưa chốt online-only vs offline-first) | 🔴 | Cần cho save an toàn & chống gian lận | Ảnh hưởng lớn UX & kiến trúc (`10` BE2) |
| A20 | Tài khoản MVP có thể là **guest/ẩn danh** trước, liên kết sau | 🟡 | Giảm ma sát onboarding | Nếu bắt đăng nhập → thêm bước, giảm conversion |

---

## 4. Giả định về nội dung/quy mô (🔴 — placeholder, cần chủ dự án cấp)

| # | Giả định tạm | WHY dùng tạm | Cần chốt ở |
|---|---|---|---|
| A21 | Có "đủ" hero cho MVP (số lượng cụ thể chưa biết) | Cần con số để lập kế hoạch art/balance | `10` GP4 |
| A22 | Số faction/class/element/role vừa phải (chưa có danh sách) | Chưa được cung cấp | `10` GP2 |
| A23 | Campaign có nhiều chương/stage (số lượng chưa biết) | Cần cho content pipeline | `10` GP7 |
| A24 | Nguồn art/animation chưa xác định (tự vẽ/mua/AI) | Ảnh hưởng số hero & tiến độ | `10` AR1/AN1 |

---

## 5. Nguyên tắc xử lý giả định

1. Giả định 🔴 **không được coi là sự thật** — phải đưa vào backlog xác nhận.
2. Khi một giả định được xác nhận/bác bỏ → cập nhật tài liệu này + tài liệu liên quan.
3. Mọi quyết định thiết kế mới nếu dựa trên giả định phải **tham chiếu ID** giả định đó.

---

### Liên kết
- Câu hỏi mở tương ứng: `10-open-questions.md`
- Ảnh hưởng scope: `01-mvp-definition.md`
- Ảnh hưởng kinh tế: `06-game-economy.md`
- Ảnh hưởng kỹ thuật: `08-technical-impact.md`
