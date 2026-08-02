# 08 — Hàm Ý Kỹ Thuật (Technical Impact)

> **KHÔNG thiết kế kiến trúc** ở đây. Tài liệu này chỉ **nhận diện hàm ý kỹ thuật** của từng hệ thống gameplay để giai đoạn Architecture có đầu vào. Với mỗi hệ thống: **Expected Complexity · Data Requirements · Backend Dependency · Godot Dependency · Performance Concerns · Future Expansion Risks**.

**Thang complexity:** S · M · L · XL.

---

## 1. Bảng hàm ý kỹ thuật theo hệ thống

| Hệ thống | Complexity | Data Requirements | Backend Dependency | Godot Dependency | Performance | Future Risks |
|---|---|---|---|---|---|---|
| Hero | L | Định nghĩa hero (stats/faction/class/element/role/skill ref); trạng thái hero của người chơi (level/sao/gear) | Cao (lưu trạng thái) | Trung bình (UI/hiển thị) | Thấp | Bùng nổ số lượng hero → cần schema mở rộng, versioning |
| Skills | L | Định nghĩa skill (effect, target, scaling); tham chiếu từ hero | Trung bình | Trung bình (VFX/anim) | Trung bình (giải effect) | Effect ngày càng phức tạp → cần hệ mô tả skill linh hoạt |
| Formation | M | Layout vị trí, ràng buộc slot | Cao (lưu đội) | Trung bình (UI kéo-thả) | Thấp | Thêm bonus vị trí/synergy → thêm rule |
| Battle (auto) | L | Snapshot đội + địch + seed; log/kết quả trận | **Cao & nhạy cảm** (chống gian lận) | Cao (mô phỏng/hiển thị) | **Cao** (mô phỏng nhiều unit/effect) | Cần xác định **tính trận ở client hay server** (xem dưới) |
| Campaign | M | Cấu hình stage (địch, thưởng, yêu cầu); tiến trình người chơi | Cao | Thấp | Thấp | Số lượng stage lớn → cần content pipeline |
| Tower | M | Cấu hình tầng, reset state | Cao | Thấp | Thấp | Reset theo lịch → phụ thuộc thời gian server |
| Summon/Gacha | L | Banner + rate + pity state; hero pool | **Cao & nhạy cảm** (RNG, tiền tệ) | Thấp (UI/anim) | Thấp | Rate/pity phải server-authoritative để chống gian lận/khiếu nại |
| Inventory | M | Danh mục sở hữu (hero/item/currency) | Cao | Thấp | Trung bình (list lớn) | Kho lớn → cần phân trang/tối ưu |
| Equipment | M | Định nghĩa gear + trạng thái lắp | Cao | Thấp | Thấp | Gear sâu (set/forge) → schema phức tạp |
| Currencies | S | Số dư nhiều loại tiền | **Cao** (nguồn sự thật) | Thấp | Thấp | Thêm loại tiền mới thường xuyên |
| Energy | S | Giá trị + timestamp hồi | Cao (chống chỉnh giờ) | Thấp | Thấp | Regen phải dựa server time |
| AFK/Idle rewards | M | Timestamp claim cuối + rate theo stage | **Cao** (tính theo thời gian, chống gian lận) | Thấp | Thấp | Công thức AFK cần config được |
| Quest | M | Định nghĩa quest + tiến độ; reset | Cao | Thấp | Thấp | Nhiều loại điều kiện → cần hệ đếm sự kiện |
| Mail | M | Danh sách mail + đính kèm | **Cao** (gửi hàng loạt) | Thấp | Trung bình (nhiều mail) | Gửi diện rộng → tải backend |
| Shop | M | Danh mục + giá + (rotation) | Cao | Thấp | Thấp | Rotation/lịch → live-config |
| Ranking | M | Bảng xếp hạng theo tiêu chí | **Cao** (tổng hợp toàn server) | Thấp | Trung bình (truy vấn top) | Số người lớn → cần index/cache |
| Settings | S | Tùy chọn cục bộ + tài khoản | Thấp–Trung bình | Thấp | Thấp | Localization/cloud save |
| Tài khoản/Save | L | Toàn bộ profile người chơi | **Rất cao** (sống còn) | Trung bình | Thấp | Cần schema versioning & migration |

---

## 2. Điểm kỹ thuật xuyên suốt cần lưu ý (không phải kiến trúc, chỉ nêu vấn đề)

| Chủ đề | Vấn đề đặt ra | Vì sao quan trọng cho Architecture |
|---|---|---|
| **Client vs Server authority** | Combat, gacha, AFK, currency tính/kiểm ở đâu? | Quyết định chống gian lận & độ phức tạp đồng bộ. **Câu hỏi mở lớn nhất** (`10`) |
| **Data-driven config** | Hero/stage/gacha/shop/reward là dữ liệu, không code | Nền cho LiveOps & tuning; ảnh hưởng cách tổ chức dữ liệu |
| **Determinism combat** | Trận auto có tái lập được (seed) không? | Ảnh hưởng khả năng verify server & replay |
| **Time/schedule** | Energy regen, AFK, reset dựa server time | Chống chỉnh giờ thiết bị |
| **Schema versioning** | Save & config sẽ đổi liên tục khi live | Tránh vỡ dữ liệu người chơi khi cập nhật |
| **Offline-first?** | Chơi được khi mất mạng tới đâu? | Ảnh hưởng UX & đồng bộ |

---

## 3. Performance Concerns (Quan ngại hiệu năng — mobile)

| Vùng | Quan ngại | Ghi chú |
|---|---|---|
| Combat rendering | Nhiều unit + VFX skill trên mobile tầm trung | Cần giới hạn hiệu ứng, object pooling (giai đoạn build) |
| Combat simulation | Giải trận nhiều effect/tick | Giữ mô hình trận đủ đơn giản ở MVP |
| UI list lớn (inventory/hero) | Lag khi cuộn hàng trăm mục | Ảo hóa list |
| Load/asset | Nhiều hero art/anim | Streaming/atlas |
| Network | Đồng bộ trạng thái | Batch request, giảm chatty API |

---

## 4. Bảng "độ nhạy cảm bảo mật" (server-authoritative ưu tiên)

| Hệ thống | Mức nhạy cảm | WHY |
|---|---|---|
| Currency, Summon, AFK reward | 🔴 Cao | Liên quan giá trị/tiền, dễ bị gian lận/khiếu nại |
| Battle result, Ranking | 🟠 Trung bình–cao | Có thể bị giả kết quả |
| Progression (level/sao) | 🟠 Trung bình | Nên xác thực nguồn tài nguyên |
| Settings, UI state | 🟢 Thấp | Có thể để client |

> Đây **không** phải quyết định kiến trúc — chỉ đánh dấu nơi cần chú ý khi thiết kế authority ở phase sau.

---

### Liên kết
- Cơ chế gameplay: `03-core-gameplay.md`
- Rủi ro kỹ thuật/scalability: `09-risk-analysis.md`
- Câu hỏi mở kỹ thuật (authority, determinism...): `10-open-questions.md`
- Bàn giao cho Architecture: `15-next-phase.md`
