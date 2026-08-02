# 01 — Định Nghĩa MVP (MVP Definition)

> Tài liệu này chốt **phạm vi MVP** — cái tối thiểu để game "chơi được, vui được, và chứng minh core loop hoạt động". Dùng khung **MoSCoW** và luôn giải thích **WHY**.

---

## 1. MVP Goal (Mục tiêu MVP)

**Mục tiêu:** Xây một bản game chơi được, chứng minh **core loop giữ chân**:

> Thu thập hero (Gacha) → Xây đội 6 hero → Sắp formation → Auto Battle qua Campaign → Nhận thưởng (kể cả AFK) → Nâng cấp hero/gear → Vượt chặng khó hơn → lặp lại.

MVP **không** nhằm tối đa doanh thu hay đầy đủ tính năng social. MVP nhằm **kiểm chứng giả thuyết cốt lõi**: *người chơi có thấy vui và quay lại với vòng lặp idle collection này không?*

---

## 2. Success Criteria (Tiêu chí thành công)

| Loại | Tiêu chí | WHY |
|---|---|---|
| Chức năng | Người chơi hoàn tất toàn bộ core loop end-to-end không lỗi chặn | Chứng minh loop khép kín |
| Chức năng | Summon → nhận hero → đưa vào đội → ra trận → thắng/thua → nhận thưởng → nâng cấp | Đây là "mạch máu" game |
| Chức năng | AFK rewards tích lũy khi offline và claim được | Đặc trưng thể loại idle |
| Trải nghiệm | Một người chơi mới hiểu phải làm gì trong 5 phút đầu (tutorial) | Onboarding quyết định D1 retention |
| Kỹ thuật | Combat auto chạy ổn định trên thiết bị mobile tầm trung | Tính khả thi kỹ thuật |
| Dữ liệu | Hero/stage/gacha nạp từ dữ liệu cấu hình, không hard-code | Nền tảng cho LiveOps sau này |
| Định tính (playtest) | Người chơi thử nghiệm muốn "chơi tiếp ngày mai" | Tín hiệu retention sớm |

> **Lưu ý:** Chỉ số retention/monetization định lượng (D1/D7, ARPPU...) là mục tiêu **Post-MVP** khi có tệp người chơi thật — xem `10-open-questions.md`.

---

## 3. Scope (Phạm vi MVP — tóm tắt)

**Trong phạm vi:** Hero + Summon/Gacha (1 banner), Đội hình 6 + Formation, Combat full-auto, Campaign (PvE tuyến chính), Nâng cấp hero cơ bản (level/EXP + nâng sao/ascension tối giản), Equipment cơ bản, AFK/Idle rewards, Energy, Currencies nền, Inventory, Mail cơ bản, Shop cơ bản, Quest/Daily cơ bản, Settings, tài khoản/lưu tiến trình.

**Ngoài phạm vi (MVP):** Guild, Raid, PvP/Arena real ranking, Tower đầy đủ (có thể để "khung"), Event/Season/Banner rotation, hệ gear/artifact sâu, social/chat, monetization IAP hoàn chỉnh.

Chi tiết bằng MoSCoW ở mục 4.

---

## 4. MoSCoW

### 4.1 MUST HAVE (Bắt buộc — không có thì không phải game này)

| Feature | WHY bắt buộc |
|---|---|
| Hệ thống Hero (thuộc tính, faction/class/element/role, stats) | Đơn vị cốt lõi của toàn bộ game |
| Summon / Gacha (≥1 banner, có tỉ lệ + pity) | Cửa ngõ thu thập hero — trái tim thể loại |
| Đội hình 6 hero + Formation (lưới vị trí) | Quyết định chiến thuật duy nhất của người chơi trong combat auto |
| Combat full-auto (skill tự động, có kết quả thắng/thua) | Cơ chế chiến đấu cốt lõi |
| Campaign PvE (chuỗi stage tăng độ khó) | Nội dung tiến trình chính + nguồn tài nguyên |
| Nâng cấp Hero cơ bản (level bằng EXP) | Vòng lặp power growth tối thiểu |
| AFK / Idle rewards (tích lũy offline, có trần, claim) | Đặc trưng định danh thể loại idle |
| Energy (giới hạn lượt chơi tốn energy) | Điều tiết nhịp & tài nguyên |
| Currencies nền (soft + premium + summon ticket) | Bôi trơn toàn bộ kinh tế |
| Inventory (hero + vật phẩm) | Chứa những gì người chơi sở hữu |
| Lưu & đồng bộ tiến trình tài khoản | Không mất dữ liệu = điều kiện sống còn |
| Tutorial / Onboarding cơ bản | Người chơi mới phải hiểu loop |
| Settings tối thiểu (âm thanh, tài khoản) | Kỳ vọng cơ bản của app mobile |

### 4.2 SHOULD HAVE (Nên có — tăng mạnh chất lượng MVP, cắt được nếu kẹt)

| Feature | WHY nên có |
|---|---|
| Nâng sao / Ascension hero (tối giản) | Chiều sâu progression đặc trưng Idle-Heroes-like |
| Equipment cơ bản (trang bị tăng stats) | Thêm một trục sức mạnh & sink tài nguyên |
| Quest/Daily quest | Tạo lý do quay lại mỗi ngày (retention) |
| Mail cơ bản | Kênh trao thưởng/đền bù — cần cho vận hành |
| Shop cơ bản (đổi tài nguyên) | Vòng tiêu tài nguyên & tiền tệ |
| Hero fragments/shards (đổi/nâng sao) | Cơ chế "gần đạt được" giữ chân |
| Ranking đơn giản (bảng xếp hạng power/stage) | Động lực so kè nhẹ |

### 4.3 COULD HAVE (Có thì tốt — nếu dư thời gian)

| Feature | WHY có thể thêm |
|---|---|
| Tower (khung 1 tower) | Nội dung "đẩy sâu" thêm; nhưng có thể để Post-MVP |
| Multi-battle / Sweep (quét lại stage đã qua) | Tiện lợi cày lại, giảm nhàm |
| Faction advantage (khắc chế hệ) | Tăng chiều sâu team-building |
| Hero skill preview/animation cơ bản | Tăng "juice" cảm giác |

### 4.4 WON'T HAVE (Sẽ KHÔNG làm trong MVP — có chủ đích)

| Feature | WHY loại khỏi MVP |
|---|---|
| Guild / Social / Chat | Phức tạp backend/đồng bộ; cần tệp người chơi; giá trị chỉ có khi đông người |
| Raid (boss guild) | Phụ thuộc Guild |
| PvP / Arena real-time ranking | Cân bằng & chống gian lận phức tạp; để sau |
| Event / Season / Banner rotation | Là LiveOps — cần core ổn định trước |
| Hệ gear/artifact sâu (set, forge, reforge) | Bùng nổ độ phức tạp; MVP chỉ cần gear cơ bản |
| Monetization IAP hoàn chỉnh (gói nạp, battle pass) | Không cần để kiểm chứng loop; rủi ro pháp lý/thanh toán |
| Backend-config toàn diện (LiveOps CMS) | MVP chỉ cần data-driven ở mức file/cấu hình, chưa cần CMS |

> **Nguyên tắc cắt scope:** khi trễ tiến độ, cắt theo thứ tự **Could → Should**, giữ nguyên **Must**. Không bao giờ hy sinh tính khép kín của core loop.

---

## 5. Out of Scope — Nhắc lại rõ ràng

MVP **không bao gồm**: social/guild/raid, PvP ranking thật, LiveOps rotation, monetization đầy đủ, gear sâu, CMS backend. Tất cả nằm ở `07-liveops-planning.md` (Post-MVP) và `04-feature-analysis.md`.

---

### Liên kết
- Loop chi tiết: `02-core-game-loop.md`
- Danh sách feature đầy đủ + độ ưu tiên: `04-feature-analysis.md`
- Roadmap hiện thực từng milestone: `11-development-roadmap.md`
- Giả định làm nền cho scope: `13-assumptions.md`
