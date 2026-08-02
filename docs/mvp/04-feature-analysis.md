# 04 — Phân Tích Tính Năng (Feature Analysis)

> Liệt kê **mọi feature**, mỗi feature có: Description, Business Value, Player Value, Priority, Complexity, Dependencies, và phân loại **MVP / Post-MVP / Future**.

**Thang đánh giá:**
- Priority: P0 (sống còn) · P1 (cao) · P2 (trung bình) · P3 (thấp)
- Complexity: S (nhỏ) · M (vừa) · L (lớn) · XL (rất lớn)
- Giai đoạn: **MVP** · **Post-MVP** · **Future**

---

## 1. Bảng tổng feature

| # | Feature | Mô tả ngắn | Business Value | Player Value | Priority | Complexity | Dependencies | Giai đoạn |
|---|---|---|---|---|---|---|---|---|
| F01 | Hệ thống Hero | Đơn vị cốt lõi: faction/class/element/role/stats/skill | Cao (sản phẩm gacha) | Cao (sưu tầm, nuôi) | P0 | L | — | MVP |
| F02 | Summon/Gacha | Rút hero có tỉ lệ + pity, 1 banner | Rất cao (monetization) | Cao (hồi hộp) | P0 | L | F01, F10 | MVP |
| F03 | Đội hình 6 + Formation | Chọn 6 hero + sắp vị trí lưới | Trung bình | Cao (chiến thuật) | P0 | M | F01, F05 | MVP |
| F04 | Combat full-auto | Giải trận tự động, thắng/thua + thưởng | Cao | Cao (cốt lõi) | P0 | L | F01, F03 | MVP |
| F05 | Campaign PvE | Chuỗi stage tăng độ khó | Cao (giữ chân) | Cao (tiến trình) | P0 | M | F04, F11 | MVP |
| F06 | Nâng cấp Hero (level/EXP) | Tăng cấp bằng EXP/tài nguyên | Trung bình | Cao (power) | P0 | M | F01, F10 | MVP |
| F07 | AFK/Idle rewards | Tích thưởng offline, có trần, claim | Cao (retention) | Rất cao (đặc trưng) | P0 | M | F05, F10 | MVP |
| F08 | Energy | Giới hạn lượt chơi tốn energy | Trung bình | Trung bình | P0 | S | F04 | MVP |
| F09 | Inventory | Quản lý hero/vật phẩm/tài nguyên | Trung bình | Trung bình | P0 | M | F01, F10 | MVP |
| F10 | Currencies | Soft/Premium/Ticket | Cao | Trung bình | P0 | S | — | MVP |
| F11 | Lưu & đồng bộ tiến trình | Persist account state | Rất cao (sống còn) | Rất cao (không mất đồ) | P0 | L | Backend | MVP |
| F12 | Tutorial/Onboarding | Dạy loop cho người mới | Cao (D1 retention) | Cao | P0 | M | Hầu hết | MVP |
| F13 | Settings cơ bản | Âm thanh, tài khoản | Thấp | Trung bình | P1 | S | F11 | MVP |
| F14 | Nâng sao/Ascension | Nâng bậc hero bằng mảnh/bản sao | Cao | Cao (mục tiêu) | P1 | M | F01, F02, F09 | MVP (Should) |
| F15 | Equipment cơ bản | Trang bị tăng stats | Trung bình | Trung bình | P1 | M | F01, F09 | MVP (Should) |
| F16 | Daily Quest | Nhiệm vụ ngày + thưởng | Cao (retention) | Trung bình | P1 | M | Nhiều hệ | MVP (Should) |
| F17 | Mail | Kênh trao thưởng/đền bù | Cao (vận hành) | Trung bình | P1 | M | Backend, F09 | MVP (Should) |
| F18 | Shop cơ bản (tĩnh) | Đổi tài nguyên lấy vật phẩm | Trung bình | Trung bình | P1 | M | F10, F09 | MVP (Should) |
| F19 | Hero fragments/shards | Mảnh để nâng sao/mở hero | Cao | Cao (near-goal) | P1 | S | F14, F02 | MVP (Should) |
| F20 | Ranking đơn giản | Leaderboard power/stage | Trung bình | Trung bình | P2 | M | Backend, F05 | MVP (Should) |
| F21 | Tua trận / tốc độ 2x | Tăng tốc combat | Trung bình | Cao (tiện) | P2 | S | F04 | MVP (Could) |
| F22 | Sweep/Quick battle | Quét lại stage đã qua | Trung bình | Cao (tiện) | P2 | M | F05 | MVP (Could)/Post |
| F23 | Faction advantage (khắc chế) | Hệ khắc chế hệ | Trung bình | Cao (chiến thuật) | P2 | M | F01, F04 | MVP (Could)/Post |
| F24 | Tower | Nội dung leo tầng | Trung bình | Trung bình | P2 | M | F04, LiveOps | Could/Post-MVP |
| F25 | Push notification | Nhắc kho/energy đầy | Trung bình | Trung bình | P2 | M | Backend/OS | Post-MVP |
| F26 | Weekly Quest | Nhiệm vụ tuần | Trung bình | Trung bình | P2 | S | F16 | Post-MVP |
| F27 | Shop rotation | Cửa hàng xoay vòng | Cao | Trung bình | P2 | M | F18, LiveOps | Post-MVP |
| F28 | Event/Banner rotation | Sự kiện & banner giới hạn | Rất cao | Cao | P1 (sau MVP) | L | F02, LiveOps | Post-MVP |
| F29 | Season/Battle Pass | Mùa + pass thưởng | Rất cao | Cao | P2 | L | LiveOps, F16 | Post-MVP |
| F30 | Guild | Bang hội | Cao (retention) | Cao | P2 | XL | Backend social | Post-MVP |
| F31 | Raid | Boss co-op | Trung bình | Cao | P3 | XL | F30 | Post-MVP |
| F32 | PvP/Arena | Đấu người chơi + rank mùa | Cao | Cao | P2 | XL | Backend, cân bằng | Post-MVP |
| F33 | Equipment sâu (set/forge) | Set bonus, cường hóa, reforge | Cao | Cao | P3 | XL | F15 | Future |
| F34 | Monetization đầy đủ (IAP) | Gói nạp, first-purchase, deal | Rất cao | — | P2 | L | F10, thanh toán | Post-MVP |
| F35 | Backend LiveOps CMS | Cấu hình nội dung từ backend | Rất cao (vận hành) | Gián tiếp | P2 | XL | Backend | Post-MVP → Future |
| F36 | Analytics/Telemetry | Thu thập chỉ số hành vi | Cao | Gián tiếp | P1 (sau MVP) | M | Backend | Post-MVP (đo sớm càng tốt) |
| F37 | Hero skin/awakening | Ngoại hình & thăng cấp cao | Cao | Cao | P3 | L | F01 | Future |
| F38 | Cloud save / liên kết tài khoản | Đồng bộ đa thiết bị | Cao | Cao | P2 | L | F11, Backend | Post-MVP |

---

## 2. Phân bố theo giai đoạn (tóm tắt)

| Giai đoạn | Feature |
|---|---|
| **MVP – Must (P0)** | F01–F13 |
| **MVP – Should** | F14–F20 |
| **MVP – Could** | F21–F24 (một phần) |
| **Post-MVP** | F24–F32, F34–F36, F38 |
| **Future** | F33, F37 (+ mở rộng F28/F29/F35) |

---

## 3. Ma trận Value × Complexity (định hướng ưu tiên)

| | Complexity thấp (S/M) | Complexity cao (L/XL) |
|---|---|---|
| **Value cao** | Làm trước: F07, F10, F16, F19, F21 | Làm nhưng chia nhỏ: F01, F02, F04, F05, F11 |
| **Value thấp** | Làm khi rảnh: F13, F26 | Hoãn/Post-MVP: F30, F31, F32, F33 |

> **Nguyên tắc:** ưu tiên **P0 + core loop** trước, rồi các feature Value-cao/Complexity-thấp (quick win), tránh sa vào XL sớm.

---

### Liên kết
- Định nghĩa MVP & MoSCoW: `01-mvp-definition.md`
- Phân tích từng cơ chế: `03-core-gameplay.md`
- Thứ tự hiện thực: `11-development-roadmap.md`
- Hàm ý kỹ thuật theo feature: `08-technical-impact.md`
