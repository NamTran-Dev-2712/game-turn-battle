# 09 — Phân Tích Rủi Ro (Risk Analysis)

> Nhận diện rủi ro theo nhóm. Mỗi rủi ro có: **Description · Impact · Probability · Mitigation**. Impact/Probability: Thấp / Trung bình / Cao.

**Điểm rủi ro (heuristic) = Impact × Probability** để ưu tiên xử lý.

---

## 1. Game Design Risks (Rủi ro thiết kế game)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| GD1 | Combat auto quá nhàm (thiếu tương tác) | Cao | Trung bình | Chiều sâu formation/faction; Post-MVP thêm ultimate thủ công; tua nhanh |
| GD2 | Thiếu USP → lẫn giữa vô số game cùng thể loại | Cao | Cao | Chốt USP sớm (`10`); playtest định tính |
| GD3 | Onboarding ngợp (quá nhiều hệ thống) | Cao | Trung bình | Progressive unlock; MVP giới hạn số lớp nâng cấp |
| GD4 | Quá nhiều lớp progression (bắt chước Idle Heroes) | Trung bình | Cao | MVP chỉ Level + Sao + Gear; mở dần |
| GD5 | Đường cong độ khó gây "tường" quá sớm | Cao | Trung bình | Tuning data-driven; nhiều nguồn tài nguyên |

---

## 2. Economy Risks (Rủi ro kinh tế)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| EC1 | Lạm phát/khan hiếm tài nguyên | Cao | Cao | Data-driven để tune nhanh; theo dõi source/sink (`06`) |
| EC2 | Gacha "cảm giác tệ" (rate/pity) | Cao | Trung bình | Pity minh bạch, công bố rate, playtest |
| EC3 | Bottleneck fragment/sao | Trung bình | Cao | Nhiều nguồn mảnh, event bù |
| EC4 | Mâu thuẫn AFK vs Energy | Trung bình | Trung bình | Chốt AFK=nền, energy=bonus (`13`) |
| EC5 | Không tune được vì hard-code | Cao | Trung bình | **Data-driven bắt buộc từ MVP** |

---

## 3. Technical Risks (Rủi ro kỹ thuật)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| TE1 | Chưa chốt client vs server authority | Cao | Cao | Quyết định sớm ở Architecture; đánh dấu ở `08`/`10` |
| TE2 | Combat không deterministic → khó verify/replay | Trung bình | Trung bình | Thiết kế mô phỏng theo seed nếu cần server-verify |
| TE3 | Combat nặng trên mobile tầm trung | Cao | Trung bình | Giữ mô hình trận đơn giản; giới hạn VFX; profiling sớm |
| TE4 | Schema save/config đổi làm vỡ dữ liệu | Cao | Trung bình | Versioning + migration ngay từ đầu |
| TE5 | Godot 4.7 + .NET 9 integration/ổn định | Trung bình | Trung bình | Prototype tích hợp sớm; theo dõi bản engine |

---

## 4. Backend Risks (Rủi ro backend)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| BE1 | Chống gian lận yếu (currency/gacha/AFK) | Cao | Trung bình | Server-authoritative cho hệ nhạy cảm (`08`) |
| BE2 | Mất/hỏng dữ liệu người chơi | Rất cao | Thấp–TB | Backup, transaction, idempotent claim |
| BE3 | Đồng bộ trạng thái phức tạp (chatty/lag) | Trung bình | Trung bình | Batch, thiết kế API gọn ở phase sau |
| BE4 | Thời gian server vs client (chỉnh giờ) | Trung bình | Trung bình | Regen/AFK dựa server time |

---

## 5. Performance Risks (Rủi ro hiệu năng)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| PF1 | Tụt FPS khi combat nhiều effect | Trung bình | Trung bình | Object pooling, giới hạn effect, LOD VFX |
| PF2 | Lag UI list lớn | Trung bình | Trung bình | Ảo hóa list |
| PF3 | Thời gian load/asset lớn | Trung bình | Trung bình | Atlas, streaming, nén |
| PF4 | Battery/nhiệt (chơi lâu) | Thấp–TB | Trung bình | Giới hạn frame khi idle, tối ưu tua |

---

## 6. Scalability Risks (Rủi ro mở rộng)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| SC1 | Thêm hero/mode phải refactor lớn | Cao | Trung bình | Data-driven + module hóa (mục tiêu Architecture) |
| SC2 | Ranking/leaderboard không chịu tải | Trung bình | Trung bình | Index/cache (phase sau) |
| SC3 | Content pipeline không kịp (content-hungry) | Cao | Cao | Công cụ tạo nội dung; template hero/stage |

---

## 7. LiveOps Risks (Rủi ro vận hành)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| LO1 | Không đổi được nội dung khi live (không có config động) | Cao | Cao | Lộ trình từ data-driven → live-config (`07`,`15`) |
| LO2 | Thiếu analytics → vận hành "mù" | Cao | Cao | Thêm telemetry sớm Post-MVP |
| LO3 | Sự cố cần đền bù mà không có công cụ | Trung bình | Trung bình | Mail system ngay từ MVP |

---

## 8. Solo Development Risks (Rủi ro dev đơn/đội nhỏ)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| SD1 | Scope creep (ôm quá nhiều) | Cao | Cao | Bám MoSCoW; cắt Could→Should khi trễ |
| SD2 | Kiệt sức/động lực giảm | Cao | Trung bình | Milestone nhỏ, mỗi mốc ra bản chơi được |
| SD3 | Thiếu chuyên môn ở mảng nào đó (art/backend) | Trung bình | Trung bình | Dùng asset/template; ưu tiên "chơi được" hơn "đẹp" |
| SD4 | Bus factor = 1 (tài liệu trong đầu) | Cao | Cao | **Bộ docs/mvp này chính là biện pháp** |

---

## 9. AI Coding Risks (Rủi ro lập trình bằng AI)

| ID | Rủi ro | Impact | Prob. | Mitigation |
|---|---|---|---|---|
| AI1 | Code AI thiếu nhất quán/kiến trúc trôi | Cao | Cao | SSOT + chuẩn kiến trúc rõ (phase sau); review |
| AI2 | AI "bịa" API/pattern không tồn tại | Trung bình | Cao | Ràng buộc bằng tài liệu & convention rõ |
| AI3 | Bug tinh vi khó phát hiện | Cao | Trung bình | Test cho hệ nhạy cảm (kinh tế/combat), verify end-to-end |
| AI4 | Mất ngữ cảnh giữa các phiên | Trung bình | Cao | Tài liệu SSOT + ghi chú quyết định |

---

## 10. Top rủi ro cần xử lý sớm nhất (ưu tiên)

| Hạng | Rủi ro | Vì sao ưu tiên |
|---|---|---|
| 1 | TE1 — Client vs Server authority | Chặn nhiều quyết định kiến trúc |
| 2 | GD2 — Thiếu USP | Quyết định game có đáng làm không |
| 3 | EC1/EC5 — Kinh tế & khả năng tune | Sai là chết game, phải sửa được |
| 4 | SD1 — Scope creep | Rủi ro số 1 của đội nhỏ |
| 5 | LO2 — Thiếu analytics | Không đo = không cải thiện được |

---

### Liên kết
- Kinh tế: `06-game-economy.md`
- Hàm ý kỹ thuật: `08-technical-impact.md`
- Câu hỏi mở (nhiều rủi ro bắt nguồn từ điểm chưa chốt): `10-open-questions.md`
- Roadmap giảm rủi ro theo mốc: `11-development-roadmap.md`
