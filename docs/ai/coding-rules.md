# AI Coding Rules & Forbidden Patterns

> Quy tắc prompt & coding cho AI agent, và **các pattern bị cấm**. Vi phạm = PR bị từ chối (`review-and-dod.md`).

---

## 1. Prompt Rules (cho người giao việc cho AI)
| Rule | Chi tiết |
|---|---|
| Một task, một mục tiêu | Task nhỏ, rõ acceptance |
| Kèm context package | Theo `context-strategy.md` §1 |
| Trỏ SSOT/ADR cụ thể | Không nói "làm hero system" chung chung |
| Nêu ranh giới | Module nào được sửa, không được sửa gì |
| Yêu cầu test | Định nghĩa test cần có |

## 2. Coding Rules (cho AI khi viết code)
| Rule | Chi tiết |
|---|---|
| Tuân dependency rule | `../architecture/dependency-graph.md` |
| Data-driven | Số liệu/cân bằng từ config, không hardcode (ADR-004) |
| SRP + hàm nhỏ | `../conventions/code-style.md` |
| Composition over inheritance | Cả Godot & C# |
| Server-authoritative | Không đặt quyết định nhạy cảm ở client (ADR-007/011) |
| Determinism combat | Integer/fixed-point, seeded RNG (ADR-011) |
| Tái sử dụng trước | Tìm code/pattern có sẵn trước khi viết mới |
| Test kèm code | Đặc biệt logic nhạy cảm (combat/kinh tế) |
| Đặt tên theo glossary | `../mvp/12-glossary.md` |
| Mơ hồ → hỏi | Trỏ `../mvp/10-open-questions.md`, không đoán |

## 3. Forbidden Patterns (CẤM)

| ❌ Cấm | Vì sao | Thay bằng |
|---|---|---|
| God Object / giant manager | Coupling, khó test | Tách theo SRP/feature |
| `switch`/`if` phình để mở rộng gameplay | Vi phạm OCP | Polymorphism/registry/data (ADR-004) |
| Hardcode config gameplay (số cân bằng, rate, đường cong) | Không tune/LiveOps | Config + Configuration Service (ADR-005) |
| Float trong combat sim | Lệch nền tảng | Integer/fixed-point (ADR-011) |
| RNG toàn cục trong sim | Không tái lập | Seeded PRNG truyền vào |
| Client quyết kết quả/thưởng nhạy cảm | Gian lận | Server-authoritative (ADR-011) |
| Domain phụ thuộc EF/HTTP/framework | Vỡ Clean Arch | Port/interface + DI (ADR-003) |
| Feature client import chéo feature | Coupling | Event Bus/signals (ADR-002) |
| `DateTime.Now` trong logic | Không test/ gian lận giờ | Inject `IClock`, dùng server time |
| Nuốt lỗi (empty catch) | Ẩn bug | Xử lý/log/trả Result rõ |
| Thêm dependency tuỳ tiện | Bề mặt rủi ro | Theo ADR-010 (qua PR có lý do) |
| Đọc file config trực tiếp trong Domain/App | Vỡ ranh giới config | `IConfigProvider` (ADR-005) |

## 4. Nguyên tắc "khi không chắc"
- Không chắc yêu cầu → **hỏi/ghi open-question**, không tự quyết.
- Không chắc pattern → theo doc module; nếu thiếu → đề xuất ADR.
- Không chắc số cân bằng → để config + đánh dấu cần tuning (`../mvp/10` EC).

## 5. Liên kết
- Code style: `../conventions/code-style.md` · Review/DoD: `review-and-dod.md`
- ADR: `../adr/`
