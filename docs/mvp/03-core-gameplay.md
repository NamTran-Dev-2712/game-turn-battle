# 03 — Phân Tích Gameplay Cốt Lõi (Core Gameplay)

> Phân tích **từng cơ chế**. Mỗi cơ chế theo khung: **Purpose (mục đích) · Gameplay Value (giá trị gameplay) · Player Experience (trải nghiệm) · Dependencies (phụ thuộc) · Future Expansion (mở rộng tương lai)**. Cột "MVP?" cho biết mức độ đưa vào MVP.

**Chú thích MVP?:** ✅ Must · 🟡 Should · 🔵 Could · ⬜ Post-MVP.

---

## 1. Formation (Đội hình / Sắp xếp vị trí) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Cho người chơi quyết định vị trí 6 hero trên lưới trận (front/back, tank chắn, DPS/support phía sau) |
| Gameplay Value | Là **quyết định chiến thuật duy nhất** trong combat full-auto → nơi thể hiện "trí tuệ" người chơi |
| Player Experience | "Tôi đặt tank trước để chịu đòn, xạ thủ sau để sống lâu" — cảm giác điều khiển gián tiếp |
| Dependencies | Hero (role/stats), Battle (vị trí ảnh hưởng target/aggro), UI kéo-thả |
| Future Expansion | Preset formation nhiều slot, hiệu ứng theo vị trí, buff theo đội hình, faction synergy bonus |

> **Quyết định thiết kế (giả định — xem `13`):** MVP dùng lưới vị trí đơn giản (vd 2 hàng ×3, hoặc các slot cố định). Cơ chế aggro/target dựa vị trí ở mức tối giản.

---

## 2. Heroes (Anh hùng) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Đơn vị cốt lõi: mỗi hero có Faction, Class, Element, Role, bộ stats và skill |
| Gameplay Value | Nền tảng thu thập, team-building, progression; là "sản phẩm" chính của gacha |
| Player Experience | Sưu tầm, nuôi lớn, gắn bó với hero yêu thích |
| Dependencies | Summon, Formation, Battle, Skills, Progression, Inventory |
| Future Expansion | Thêm hero mới liên tục (data-driven), skin, awakening, faction bonus, hero mùa/giới hạn |

**Phân loại hero (MVP thiết kế khung, không cần đủ tất cả):**

| Chiều phân loại | Ví dụ giá trị | Vai trò trong game |
|---|---|---|
| Faction (phe) | vd 4–6 phe | Nhóm hero, nền tảng khắc chế & synergy |
| Class (lớp) | Warrior/Mage/Ranger/Support... | Phong cách chiến đấu |
| Element (hệ) | vd Fire/Water/Earth/Light/Dark | Cơ chế khắc chế (faction advantage) |
| Role (vai) | Tank / DPS / Support / Healer | Vị trí trong đội & formation |
| Rarity (độ hiếm) | vd 3★→5★+ | Giá trị gacha & trần sức mạnh |

> Faction/Element có thể trùng vai trò "khắc chế"; **cần chốt** ở `10-open-questions.md` xem dùng Faction hay Element làm trục khắc chế chính.

---

## 3. Skills (Kỹ năng) — ✅ (auto)

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Định nghĩa hành vi hero trong trận (đánh thường, kỹ năng, ultimate) |
| Gameplay Value | Tạo sự khác biệt giữa hero, chiều sâu synergy đội hình |
| Player Experience | Xem skill "nổ" tự động; cảm giác đội mình mạnh/đẹp |
| Dependencies | Hero, Battle (bộ giải trận), Formation |
| Future Expansion | Ultimate thủ công (bấm tay), skill nâng cấp riêng, combo/synergy giữa hero |

> **Quyết định (chốt):** MVP **full-auto** — skill/ultimate tự kích theo năng lượng/cooldown, người chơi **không** bấm. Ultimate thủ công là **Post-MVP** (xem `13-assumptions.md`).

---

## 4. Battle (Chiến đấu) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Giải quyết một trận 6 hero vs quân địch, ra kết quả thắng/thua + thưởng |
| Gameplay Value | Nơi mọi quyết định (team, formation, nâng cấp) được "chấm điểm" |
| Player Experience | Xem trận auto, hồi hộp thắng sát nút, hài lòng khi "một phát ăn ngay" |
| Dependencies | Hero, Skills, Formation, Campaign/Tower (nguồn trận), Energy |
| Future Expansion | Tua nhanh 2x/4x, skip/sweep, auto-repeat, manual ultimate, boss cơ chế đặc biệt |

**Đặc tính combat MVP:**
- Full-auto, deterministic hoặc gần-deterministic (xem `08` & `10` về nơi tính toán trận).
- Có tốc độ tua (Should-have: 2x).
- Kết quả: thắng/thua + drop thưởng.

---

## 5. Campaign (Chiến dịch PvE) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Tuyến nội dung PvE chính: chuỗi stage tăng độ khó theo cốt truyện |
| Gameplay Value | Nguồn tiến trình + tài nguyên chính; đặt "mốc AFK stage" |
| Player Experience | Cảm giác "đi xa hơn", mở khóa vùng mới, first-clear thưởng lớn |
| Dependencies | Battle, Hero power, Energy, Rewards |
| Future Expansion | Chapter mới liên tục, độ khó Elite/Nightmare, campaign nhánh theo faction |

> Campaign progress thường **quyết định tốc độ AFK rewards** (đẩy càng xa, farm càng nhanh) — liên kết `05` & `06`.

---

## 6. Tower (Tháp thử thách) — 🔵/⬜

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Nội dung "đẩy sâu": leo tầng vô tận/nhiều tầng, thưởng theo mốc |
| Gameplay Value | Mục tiêu dài hạn cho người chơi mạnh; sink sức mạnh |
| Player Experience | "Tôi leo được tới tầng bao nhiêu?" — đo sức đội |
| Dependencies | Battle, Hero power, Reset (LiveOps) |
| Future Expansion | Nhiều tower theo faction/hệ, tower mùa, reset hằng ngày/tuần |

> MVP: có thể làm **1 tower khung** (Could) hoặc hoãn Post-MVP. Reset tower thuộc LiveOps (`07`).

---

## 7. Raid (Đánh boss hợp tác) — ⬜

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Nội dung co-op (thường theo Guild) đánh boss chung, chia thưởng |
| Gameplay Value | Gắn kết social, mục tiêu nhóm |
| Player Experience | Cùng guild "hạ trùm" |
| Dependencies | **Guild** (bắt buộc), Battle, backend đồng bộ |
| Future Expansion | Raid theo mùa, boss cơ chế, bảng damage |

> **Ngoài MVP** (phụ thuộc Guild). Ghi nhận để kiến trúc "chừa chỗ".

---

## 8. Guild (Bang hội) — ⬜

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Cộng đồng người chơi: tham gia, đóng góp, hoạt động chung |
| Gameplay Value | Retention social, nền cho Raid/PvP guild |
| Player Experience | Thuộc về một nhóm, so kè & hợp tác |
| Dependencies | Backend real-time/đồng bộ, chat, quản lý thành viên |
| Future Expansion | Guild war, guild shop, guild raid, cấp bậc |

> **Ngoài MVP** — phức tạp backend, giá trị chỉ có khi đông người.

---

## 9. Summon (Triệu hồi / Gacha) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Cơ chế thu thập hero qua rút thẻ ngẫu nhiên có tỉ lệ |
| Gameplay Value | Trái tim monetization & collection; nguồn hero mới |
| Player Experience | Hồi hộp rút, sung sướng khi ra hero hiếm, an tâm nhờ pity |
| Dependencies | Hero pool, Currencies/Ticket, Inventory, Rate config |
| Future Expansion | Banner giới hạn/rate-up, pity nâng cao, banner theo faction, multi-summon x10 |

**MVP summon cần:**
- ≥1 banner cố định; rút đơn + rút 10.
- Tỉ lệ theo rarity + **pity** (đảm bảo hiếm sau N lần).
- Ra hero mới hoặc **mảnh hero** (fragment) nếu đã sở hữu.
- Tỉ lệ/nội dung **config được** (nền LiveOps).

---

## 10. Inventory (Kho đồ) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Chứa & quản lý hero, vật phẩm, mảnh, tài nguyên |
| Gameplay Value | Nền cho mọi giao dịch nâng cấp/đổi thưởng |
| Player Experience | Xem "gia tài", quản lý, lọc, sắp xếp |
| Dependencies | Hero, Equipment, Currencies, Shop, Summon |
| Future Expansion | Sort/filter nâng cao, đánh dấu khóa, bán/phân giải vật phẩm |

---

## 11. Equipment (Trang bị) — 🟡

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Trang bị tăng stats hero (thêm trục sức mạnh ngoài level/sao) |
| Gameplay Value | Sink tài nguyên, tùy biến hero, chiều sâu min-max |
| Player Experience | "Lắp đồ" cho hero mạnh hơn |
| Dependencies | Hero, Inventory, Materials, Campaign drop |
| Future Expansion | Set bonus, cường hóa/reforge, độ hiếm gear, gem/khảm, gear theo class |

> MVP: gear **cơ bản** (một vài slot, tăng stats trực tiếp). Hệ gear sâu (set/forge) là **Won't-have** ở MVP (`01`).

---

## 12. Quest (Nhiệm vụ) — 🟡

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Định hướng hành vi (đăng nhập, đánh N trận, summon...) và trao thưởng |
| Gameplay Value | Dẫn dắt người chơi vào loop, tạo lý do hoạt động |
| Player Experience | Checklist thỏa mãn + thưởng đều |
| Dependencies | Hầu hết hệ thống (để đếm tiến độ), Rewards |
| Future Expansion | Quest tuần/tháng, quest sự kiện, achievement dài hạn, battle pass |

> MVP: **Daily quest** cơ bản (+ vài mốc). Weekly là Should.

---

## 13. Mail (Hòm thư) — 🟡

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Kênh gửi thưởng/đền bù/thông báo tới người chơi |
| Gameplay Value | Công cụ vận hành sống còn (đền bù lỗi, phát quà sự kiện) |
| Player Experience | "Có quà!" — nguồn thưởng bất ngờ |
| Dependencies | Backend (gửi mail), Inventory (nhận đồ) |
| Future Expansion | Mail hệ thống vs cá nhân, mail có hạn, đính kèm phức tạp |

> MVP: mail cơ bản (nhận, claim, xóa). Cần thiết cho vận hành ngay cả khi ít người.

---

## 14. Shop (Cửa hàng) — 🟡

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Nơi đổi tài nguyên/tiền tệ lấy vật phẩm/hero/mảnh |
| Gameplay Value | Sink tài nguyên & premium; điều tiết kinh tế |
| Player Experience | "Mua món cần", săn deal |
| Dependencies | Currencies, Inventory, (Post-MVP: rotation/LiveOps) |
| Future Expansion | Shop xoay vòng, shop sự kiện/guild, gói IAP, daily deal |

> MVP: shop **tĩnh cơ bản** (đổi soft/premium/ticket lấy mảnh/mat). Rotation là Post-MVP (`07`).

---

## 15. Currencies (Hệ tiền tệ) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Trung gian trao đổi & điều tiết mọi hệ thống kinh tế |
| Gameplay Value | Điều khiển tốc độ progression & monetization |
| Player Experience | "Đủ tiền làm gì?" — quyết định ưu tiên |
| Dependencies | Gần như mọi hệ thống |
| Future Expansion | Thêm tiền tệ chuyên biệt (guild coin, arena coin, event coin) |

Chi tiết từng loại tiền ở `06-game-economy.md`. MVP tối thiểu: **Soft currency (gold), Premium currency (gem/đá quý), Summon ticket**.

---

## 16. Energy (Năng lượng) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Giới hạn số lần chơi tốn energy (điều tiết nhịp & tài nguyên) |
| Gameplay Value | Ngăn cày vô hạn, tạo nhịp session, điểm monetization tiềm năng |
| Player Experience | "Hôm nay còn bao nhiêu lượt?" — quản lý tài nguyên |
| Dependencies | Battle/Campaign (tiêu energy), thời gian (hồi), Shop (mua thêm) |
| Future Expansion | Nhiều loại energy theo mode, mua energy, energy sự kiện |

> **Chú ý cân bằng:** idle model + energy phải hài hòa — AFK là nguồn chính, energy điều tiết "cày chủ động". Rủi ro ở `06`/`09`.

---

## 17. Ranking (Bảng xếp hạng) — 🟡/⬜

| Khía cạnh | Nội dung |
|---|---|
| Purpose | So kè giữa người chơi (theo power, campaign stage, tower...) |
| Gameplay Value | Động lực đua, retention cho competitor |
| Player Experience | "Tôi đứng thứ mấy?" |
| Dependencies | Backend leaderboard, dữ liệu người chơi |
| Future Expansion | Arena PvP thật, mùa xếp hạng, thưởng rank |

> MVP: **ranking đơn giản** (leaderboard power/stage — Should). PvP/Arena real-time là **Post-MVP**.

---

## 18. Settings (Cài đặt) — ✅

| Khía cạnh | Nội dung |
|---|---|
| Purpose | Cấu hình người dùng (âm thanh, đồ họa, ngôn ngữ, tài khoản, hỗ trợ) |
| Gameplay Value | Không trực tiếp gameplay nhưng là kỳ vọng cơ bản |
| Player Experience | Kiểm soát trải nghiệm, tin tưởng app |
| Dependencies | Hệ thống tài khoản, audio, localization |
| Future Expansion | Liên kết tài khoản, đa ngôn ngữ, đồng bộ đám mây, xóa dữ liệu (GDPR) |

> MVP: âm thanh on/off, thông tin tài khoản, đăng xuất/liên kết cơ bản.

---

## 19. Bảng tổng mức độ MVP

| Cơ chế | MVP? | Ghi chú |
|---|---|---|
| Formation | ✅ Must | Lưới vị trí đơn giản |
| Heroes | ✅ Must | Data-driven |
| Skills | ✅ Must | Full-auto |
| Battle | ✅ Must | Auto, có tua |
| Campaign | ✅ Must | Tuyến chính |
| Summon/Gacha | ✅ Must | 1 banner + pity |
| Inventory | ✅ Must | — |
| Currencies | ✅ Must | Soft/Premium/Ticket |
| Energy | ✅ Must | — |
| Settings | ✅ Must | Tối thiểu |
| Nâng sao/Ascension | 🟡 Should | Tối giản |
| Equipment | 🟡 Should | Cơ bản |
| Quest | 🟡 Should | Daily |
| Mail | 🟡 Should | Cơ bản |
| Shop | 🟡 Should | Tĩnh |
| Ranking | 🟡 Should | Leaderboard đơn giản |
| Tower | 🔵 Could | 1 tower khung |
| Guild | ⬜ Post | Phụ thuộc backend social |
| Raid | ⬜ Post | Phụ thuộc Guild |
| PvP/Arena | ⬜ Post | Cân bằng phức tạp |

---

### Liên kết
- Ưu tiên & độ phức tạp từng feature: `04-feature-analysis.md`
- Hàm ý kỹ thuật: `08-technical-impact.md`
- Điểm chưa chốt (trục khắc chế, aggro, tính trận ở đâu): `10-open-questions.md`
