# `shared/codegen/` — Sinh mã từ hợp đồng

| Mục | Nội dung |
|---|---|
| **Purpose** | Định nghĩa & output sinh model/DTO cho client từ `../contracts/` → giảm lệch tay. |
| **Responsibilities** | Cấu hình generator; output tạm (gitignore output sinh lại được). |
| **Allowed** | File cấu hình generator, template. |
| **Not allowed** | ❌ sửa tay output đã sinh; ❌ logic runtime. |
| **Dependencies** | `../contracts/` (nguồn), `tools/codegen` (bộ chạy). |
| **Owner** | Platform team. |
| **Future expansion** | Thêm target ngôn ngữ/format. |

> **Bootstrap:** placeholder. Pipeline codegen thật hiện thực sau (xem `../../tools/codegen/README.md`).
