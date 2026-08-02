# 14 — Checklist Sẵn Sàng (Readiness Checklist)

> Xác định dự án đã **sẵn sàng bước vào giai đoạn Architecture** chưa. Nếu chưa, liệt kê **những gì còn thiếu** và mức độ chặn.

**Trạng thái:** ✅ Đủ · 🟡 Đủ để bắt đầu (có giả định) · 🔴 Còn thiếu/chặn.

---

## 1. Checklist Product Discovery (đầu ra giai đoạn này)

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Tầm nhìn & bản sắc game | ✅ | `00-project-overview.md` |
| Định nghĩa MVP + MoSCoW | ✅ | `01-mvp-definition.md` |
| Core loop mọi tầng thời gian | ✅ | `02-core-game-loop.md` |
| Phân tích từng cơ chế gameplay | ✅ | `03-core-gameplay.md` |
| Danh sách feature + ưu tiên | ✅ | `04-feature-analysis.md` |
| Progression & bottleneck | ✅ | `05-player-progression.md` |
| Kinh tế (cấu trúc & nguyên tắc) | 🟡 | `06` — **con số cụ thể chưa chốt** |
| Kế hoạch LiveOps (MVP vs Post) | ✅ | `07-liveops-planning.md` |
| Hàm ý kỹ thuật | ✅ | `08-technical-impact.md` |
| Phân tích rủi ro | ✅ | `09-risk-analysis.md` |
| Câu hỏi mở | ✅ | `10-open-questions.md` (đã ghi nhận đầy đủ) |
| Roadmap MVP theo milestone | ✅ | `11-development-roadmap.md` |
| Glossary | ✅ | `12-glossary.md` |
| Giả định | ✅ | `13-assumptions.md` |

**Kết luận Discovery:** ✅ **Bộ tài liệu SSOT đã hoàn chỉnh** ở mức Product Discovery.

---

## 2. Sẵn sàng cho Architecture? — Đánh giá

> **Kết luận:** 🟡 **Sẵn sàng bắt đầu Architecture với điều kiện** — có thể khởi động vì các quyết định gameplay cốt lõi đã rõ, nhưng **3 câu hỏi chặn (🔴)** nên có định hướng trong những bước đầu của phase Architecture.

### 2.1 Điều kiện đã đủ để bắt đầu
| Yếu tố | Vì sao đủ |
|---|---|
| Loop & feature cốt lõi rõ ràng | Kiến trúc biết cần phục vụ gì |
| MoSCoW & phạm vi MVP rõ | Biết cái gì làm trước |
| Hàm ý kỹ thuật & điểm nhạy cảm đã nêu | Kiến trúc có đầu vào rủi ro |
| Nguyên tắc data-driven đã xác lập | Định hướng nền tảng rõ |

### 2.2 Còn thiếu / cần chốt (chặn hoặc nên có)

| # | Thiếu | Mức | Vì sao quan trọng | Nguồn |
|---|---|---|---|---|
| R1 | **Combat & hệ nhạy cảm tính ở client hay server** | 🔴 chặn | Quyết định toàn bộ mô hình authority & đồng bộ | `10` CB1/BE1 |
| R2 | **Online-only vs offline-first có đồng bộ** | 🔴 chặn | Ảnh hưởng nền tảng đồng bộ & UX | `10` BE2 |
| R3 | **Combat có cần deterministic không** | 🔴 chặn | Ảnh hưởng thiết kế mô phỏng & verify | `10` CB2 |
| R4 | Con số kinh tế (rate, cap, đường cong) | 🟡 | Cần cho balance, không chặn kiến trúc (vì data-driven) | `06`,`10` EC |
| R5 | Danh sách faction/class/element/role & số hero | 🟡 | Cần cho schema hero & content, nhưng khung đã đủ để thiết kế | `10` GP2/GP4 |
| R6 | Trục khắc chế (Faction vs Element) | 🟡 | Ảnh hưởng data hero & combat rule | `10` GP3 |
| R7 | Nguồn art/animation & phong cách | 🟡 | Ảnh hưởng pipeline & hiệu năng | `10` AR/AN |
| R8 | Hệ tài khoản MVP (guest vs đăng nhập) | 🟡 | Ảnh hưởng auth & save | `10` BE3 |

---

## 3. Khuyến nghị hành động trước/đầu Architecture

| Ưu tiên | Hành động | Ai quyết |
|---|---|---|
| 1 | Chốt định hướng R1, R2, R3 (authority, online model, determinism) | Chủ dự án + Solution Architect |
| 2 | Xác nhận các giả định 🔴 ở `13` (A18, A19) | Chủ dự án |
| 3 | Cung cấp danh sách faction/class/element/role & số hero mục tiêu | Chủ dự án (Game Design) |
| 4 | Quyết trục khắc chế (Faction/Element) | Chủ dự án |
| 5 | Xác định nguồn art & phong cách | Chủ dự án |

> **Lưu ý:** nhờ nguyên tắc **data-driven**, phần lớn "con số" (R4) **không chặn** kiến trúc — có thể tune sau. Điều thật sự chặn là **mô hình authority & đồng bộ** (R1–R3).

---

## 4. Phán quyết cuối

| Câu hỏi | Trả lời |
|---|---|
| Discovery hoàn chỉnh chưa? | ✅ Có |
| Có thể bắt đầu Architecture chưa? | 🟡 Có, kèm điều kiện chốt R1–R3 sớm |
| Có điểm chặn tuyệt đối không? | Không tuyệt đối, nhưng R1–R3 cần định hướng ở tuần đầu Architecture |

---

### Liên kết
- Câu hỏi mở đầy đủ: `10-open-questions.md`
- Giả định: `13-assumptions.md`
- Bàn giao Architecture: `15-next-phase.md`
