# `tools/codegen/` — Sinh mã từ hợp đồng (TODO stub)

| Mục | Nội dung |
|---|---|
| **Purpose** | Sinh DTO/model client từ `shared/contracts/` (OpenAPI) → giảm lệch client-server. |
| **Responsibilities** | Chạy khi hợp đồng đổi; output vào `shared/codegen/` (gitignore output). |
| **Allowed** | Cấu hình generator + template. |
| **Not allowed** | ❌ sửa tay output. |
| **Dependencies** | `shared/contracts`, `shared/codegen`. |
| **Owner** | Platform team. |
| **Future expansion** | Thêm target ngôn ngữ. |

## TODO (chưa hiện thực ở bootstrap)
- [ ] Chọn generator (OpenAPI Generator / NSwag / tự viết).
- [ ] Map contract → GDScript model & C# DTO.
- [ ] Tích hợp vào CI kiểm "codegen sạch".
