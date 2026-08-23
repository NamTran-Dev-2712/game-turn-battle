# 10 — Câu Hỏi Mở (Open Questions)

> Mọi điểm **chưa được trả lời**. **Không bịa câu trả lời** — nơi đây ghi nhận để chủ dự án/giai đoạn sau quyết định. Phân loại theo lĩnh vực. Mỗi câu hỏi ghi mức độ chặn: 🔴 chặn kiến trúc · 🟠 nên chốt sớm · 🟢 chốt sau được.

> Ghi chú: những gì **đã chốt** (north star Idle Heroes, combat full-auto, độ chi tiết docs) nằm ở `13-assumptions.md`, không lặp lại ở đây.

---

## 1. Gameplay

| # | Câu hỏi | Mức |
|---|---|---|
| GP1 | USP (điểm khác biệt bán hàng) của game là gì so với Idle Heroes/AFK Arena? | 🟠 |
| GP2 | Có bao nhiêu Faction, Class, Element, Role? Danh sách cụ thể? | 🟠 |
| GP3 | Trục "khắc chế" chính là Faction hay Element? Cơ chế khắc chế (bonus %) thế nào? | 🟠 |
| GP4 | Số lượng hero dự kiến cho MVP (bao nhiêu để "đủ chơi")? | 🟠 |
| GP5 | Lưới formation cụ thể (mấy hàng/cột, slot cố định hay tự do)? | 🟠 |
| GP6 | Có giới hạn "1 hero 1 slot đội" hay cho trùng bản sao? | 🟢 |
| GP7 | Bao nhiêu chương/stage campaign cho MVP? | 🟢 |

---

## 2. Combat

| # | Câu hỏi | Mức |
|---|---|---|
| CB1 | Combat tính ở **client hay server**? (ảnh hưởng chống gian lận & kiến trúc) | 🔴 |
| CB2 | Combat có **deterministic theo seed** không (để replay/verify)? | 🔴 |
| CB3 | Cơ chế target/aggro theo vị trí cụ thể ra sao? | 🟠 |
| CB4 | Ultimate/energy skill kích hoạt theo cơ chế nào (thời gian, đòn đánh, cột năng lượng)? | 🟠 |
| CB5 | Có yếu tố ngẫu nhiên (crit, miss) không? Mức độ? | 🟠 |
| CB6 | Độ dài một trận mục tiêu (giây)? Có giới hạn thời gian/hòa? | 🟢 |
| CB7 | Tốc độ tua hỗ trợ (2x/4x) và skip trận — vào MVP hay Post? | 🟢 |

---

## 3. Economy

| # | Câu hỏi | Mức |
|---|---|---|
| EC1 | Con số cụ thể: gacha rate theo rarity? Pity bao nhiêu lần? | 🟠 |
| EC2 | AFK reward rate & cap (giờ tối đa tích)? | 🟠 |
| EC3 | Energy: max, tốc độ hồi, chi phí mỗi trận, giá mua? | 🟠 |
| EC4 | Đường cong chi phí level/sao hero? | 🟠 |
| EC5 | AFK vs Energy: nguồn nào là chính? (giả định ở `13`, cần xác nhận) | 🟠 |
| EC6 | Có bao nhiêu loại tiền tệ ở MVP (danh sách chính thức)? | 🟢 |
| EC7 | Chính sách monetization (dù Post-MVP): gói nạp, battle pass? | 🟢 |

---

## 4. Backend

| # | Câu hỏi | Mức |
|---|---|---|
| BE1 | Server-authoritative cho những hệ nào ở MVP (currency/gacha/AFK chắc chắn?) | 🔴 |
| BE2 | Yêu cầu online: game **online-only** hay **offline-first có đồng bộ**? | 🔴 |
| BE3 | Hệ tài khoản MVP: guest-only hay có đăng nhập (Google/Apple/email)? | 🟠 |
| BE4 | Quy mô người chơi dự kiến (ảnh hưởng hạ tầng)? | 🟢 |
| BE5 | Hosting/hạ tầng dự kiến (cloud nào)? | 🟢 |

> **BE3 — cập nhật Phase 18 (2026-08-21, chưa đóng hẳn 🟠):** MVP hiện thực **guest-first** — `POST /api/v1/auth/guest`
> tạo `Account` (`AccountType.Guest`) + JWT. Schema account **chừa chỗ** liên kết provider (bảng `account_providers`
> tương lai) nên thêm Google/Apple/email **không cần refactor** (ADR-006). Câu hỏi **có** đăng nhập provider ở MVP hay
> không **vẫn mở** (Post-MVP theo hiện trạng) — chỉ đóng khi product chốt. Xem `docs/roadmap/18-auth-jwt-guest.md`.

---

## 5. UI

| # | Câu hỏi | Mức |
|---|---|---|
| UI1 | Sơ đồ điều hướng màn hình chính (main hub) gồm những gì? | 🟠 |
| UI2 | Có wireframe/mockup tham chiếu không? | 🟠 |
| UI3 | Bố cục landscape ưu tiên tay cầm 1 tay hay 2 tay? | 🟢 |
| UI4 | Hệ thống thông báo/badge (chấm đỏ) thiết kế ra sao? | 🟢 |

---

## 6. UX

| # | Câu hỏi | Mức |
|---|---|---|
| UX1 | Độ dài tutorial mục tiêu (mấy phút, mấy bước)? | 🟠 |
| UX2 | Chính sách "skip cho người cũ"? | 🟢 |
| UX3 | Xử lý mất mạng giữa trận/giao dịch (UX)? | 🟠 |
| UX4 | Ngôn ngữ MVP: chỉ tiếng Việt hay đa ngôn ngữ? | 🟠 |

---

## 7. Art

| # | Câu hỏi | Mức |
|---|---|---|
| AR1 | Nguồn art hero (tự vẽ / mua asset / AI)? | 🟠 |
| AR2 | Phong cách nghệ thuật (anime/chibi/realistic 2D)? | 🟠 |
| AR3 | Độ phân giải & tỉ lệ khung hình mục tiêu (thiết bị chuẩn)? | 🟢 |
| AR4 | Ngân sách/thời gian cho art (ảnh hưởng số hero MVP)? | 🟠 |

---

## 8. Animation

| # | Câu hỏi | Mức |
|---|---|---|
| AN1 | Hero animation: skeletal (Spine/Godot bones) hay spritesheet? | 🟠 |
| AN2 | Mức độ animation MVP (idle/attack/skill/death) tối thiểu? | 🟠 |
| AN3 | VFX skill: mức độ hoành tráng vs hiệu năng? | 🟢 |

---

## 9. Audio

| # | Câu hỏi | Mức |
|---|---|---|
| AU1 | Nhạc nền & SFX: nguồn (mua/tự làm)? | 🟢 |
| AU2 | Phạm vi audio MVP (nhạc hub + SFX cơ bản)? | 🟢 |

---

## 10. LiveOps

| # | Câu hỏi | Mức |
|---|---|---|
| LO1 | Ưu tiên LiveOps đầu tiên sau MVP (login lịch / banner / event)? | 🟢 |
| LO2 | Có cần "live-config" (đổi từ server không update app) sớm tới đâu? | 🟠 |
| LO3 | Ai vận hành LiveOps (cần công cụ admin tới mức nào)? | 🟢 |

---

## 11. Deployment

| # | Câu hỏi | Mức |
|---|---|---|
| DP1 | Nền tảng phát hành đầu tiên: Android trước hay cả iOS? | 🟠 |
| DP2 | Kênh phát hành (Google Play/App Store/APK thử nghiệm)? | 🟢 |
| DP3 | Quy trình build/CI, ký ứng dụng, cập nhật OTA cho data? | 🟠 |
| DP4 | Yêu cầu tuân thủ (quyền riêng tư, độ tuổi, thanh toán)? | 🟠 |

---

## 12. Analytics

| # | Câu hỏi | Mức |
|---|---|---|
| AL1 | Chỉ số cốt lõi cần đo (retention D1/D7, funnel tutorial, source/sink)? | 🟠 |
| AL2 | Công cụ analytics dự kiến (tự làm / Firebase / GameAnalytics)? | 🟢 |
| AL3 | Có cần A/B testing ngay không? | 🟢 |
| AL4 | Chính sách thu thập dữ liệu & đồng ý người dùng? | 🟠 |

---

## 13. Câu hỏi CHẶN kiến trúc (tổng hợp 🔴)

> Những câu này **nên có định hướng trước khi bắt đầu Architecture**:

1. **CB1/BE1** — Combat & hệ nhạy cảm tính ở client hay server?
2. **CB2** — Combat có cần deterministic không?
3. **BE2** — Online-only hay offline-first có đồng bộ?

---

### Liên kết
- Giả định đã chốt: `13-assumptions.md`
- Sẵn sàng kiến trúc chưa: `14-readiness-checklist.md`
- Bàn giao Architecture: `15-next-phase.md`
