# Code Style (Chuẩn viết mã)

> Chuẩn cho GDScript (client) và C# (backend), documentation style, và **quy tắc determinism** (bắt buộc cho combat — ADR-011). Nguyên tắc nền: `clean-code`, SOLID, SRP, composition over inheritance.

---

## 1. Nguyên tắc chung (cả 2 phía)

| Nguyên tắc | Diễn giải |
|---|---|
| SRP | Mỗi hàm/lớp một trách nhiệm; hàm ngắn, làm một việc |
| Intention-revealing | Tên nói rõ ý định; tránh viết tắt tối nghĩa |
| Guard clause | Trả về sớm, giảm lồng ghép sâu |
| Không magic number | Đưa vào hằng có tên hoặc **config** (ADR-004) |
| Không God Object/giant manager | Đề bài cấm; tách trách nhiệm |
| Không switch để mở rộng gameplay | Dùng polymorphism/registry/data (ADR-004) |
| Composition > inheritance | Ghép thành phần thay vì kế thừa sâu |
| Immutability khi có thể | Giảm bug trạng thái chia sẻ |
| Error explicit | Trả Result/exception rõ ràng, không nuốt lỗi |

---

## 2. GDScript (Godot client)

- Bật **static typing** mọi nơi: `var hp: int = 0`, `func apply_damage(amount: int) -> void:`.
- `snake_case` cho biến/hàm; `PascalCase` cho `class_name`; `CONSTANT_CASE` cho hằng.
- Ưu tiên `@onready` cho ref node; export biến cấu hình qua `@export`.
- Signal: khai báo rõ tham số, đặt tên theo sự kiện (`signal battle_finished(result: BattleResult)`).
- **Không** logic nặng trong `_process` nếu tránh được; combat sim tách khỏi node (thuần).
- Tránh `get_node()` chuỗi dài; dùng scene composition & unique name (`%Node`).
- Không truy cập trực tiếp state cache toàn cục để ghi; đi qua service (ADR-007).
- Ưu tiên `preload`/`load` async cho asset nặng (ADR-009).

## 3. C# (.NET backend)

- Bật `nullable` reference types; xử lý null tường minh.
- `PascalCase` type/method/property; `camelCase` biến cục bộ/tham số; `_camelCase` field private.
- Async I/O toàn bộ (`async/await`, trả `Task`); không block (`.Result`/`.Wait()`).
- Dùng DI qua constructor; **không** service locator trong Domain/Application.
- Domain **thuần**: không `DbContext`, không HTTP, không thời gian hệ thống trực tiếp (inject `IClock`).
- CQRS: command đổi trạng thái, query chỉ đọc; không lẫn.
- Validation qua FluentValidation trong pipeline behavior (`../backend/domain-and-application.md`).
- Dùng `record`/value object cho bất biến; entity có invariant bảo vệ trong method, không setter công khai bừa.
- Ghi log có cấu trúc (Serilog), không log dữ liệu nhạy cảm.

## 4. Quy tắc Determinism (BẮT BUỘC cho combat — ADR-011)

| Quy tắc | Vì sao |
|---|---|
| **Không dùng float/double** trong sim combat; dùng **integer/fixed-point** | Float lệch giữa nền tảng/máy |
| RNG chỉ qua **seeded PRNG** truyền vào; không dùng RNG toàn cục | Tái lập kết quả |
| Thứ tự lặp **ổn định** (danh sách có thứ tự xác định, không dựa hash order) | Kết quả nhất quán |
| Không phụ thuộc thời gian thực/đồng hồ trong sim | Tất định |
| Cùng (config version, snapshot, seed) ⇒ cùng output | Điều kiện re-sim server |
| Client & server hiện thực **cùng ruleset**; có golden test vector | Verify khớp |

## 5. Documentation style (trong code)

- **GDScript**: docstring `##` cho class & hàm public; nêu ý định + tham số + tác dụng phụ (signal phát ra).
- **C#**: XML doc `///` cho public API (command/query/handler/interface); nêu invariant & lỗi có thể ném.
- Comment giải thích **WHY**, không lặp lại WHAT của code.
- Mỗi feature có `README.md` ngắn (mục đích, ranh giới, event công khai) — hỗ trợ AI context (`../ai/context-strategy.md`).

## 6. Kích thước & độ phức tạp (hướng dẫn, không cứng nhắc)
- Hàm ưu tiên < ~40 dòng; lớp < ~300 dòng — vượt là tín hiệu nên tách.
- Tránh tham số > 4; gom thành object/DTO.
- Cyclomatic complexity cao → tách hàm/policy.

## 7. Máy kiểm (enforcement)
- `.editorconfig` (dùng chung), Roslyn analyzers (BE), gdlint/gdformat (client, qua addon/CI).
- Warning-as-error mức hợp lý ở BE (ADR-010).
- Kiểm ở CI (`../deployment/ci-cd-pipeline.md`) + review checklist (`../ai/review-and-dod.md`).

## 8. Liên kết
- Đặt tên: `naming.md`
- Determinism chi tiết: `../gameplay/combat-framework.md`, ADR-011
- Forbidden patterns: `../ai/coding-rules.md`
