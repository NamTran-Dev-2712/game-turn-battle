# `tools/content-importer/` — Import bảng → config (TODO stub, Post-MVP)

| Mục | Nội dung |
|---|---|
| **Purpose** | Chuyển bảng tính (csv/xlsx) của game design → JSON trong `config/`. |
| **Responsibilities** | Chuẩn hoá/validate khi import; giữ config là nguồn author-time. |
| **Allowed** | Mã importer + mapping. |
| **Not allowed** | ❌ ghi đè config không qua validate. |
| **Dependencies** | `config/`, `shared/config-schema`. |
| **Owner** | Content-tools team. |
| **Future expansion** | Round-trip export, diff. |

## TODO (Post-MVP)
- [ ] Định dạng bảng nguồn & mapping cột → schema.
- [ ] Validate sau import (gọi `config-validator`).
