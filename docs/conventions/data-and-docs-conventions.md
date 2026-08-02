# Data & Docs Conventions (JSON/Config & Markdown)

> Quy ước cho file dữ liệu data-driven (JSON config) và tài liệu Markdown. Nền cho ADR-004/005.

---

## 1. JSON / Config convention

| Quy tắc | Chi tiết |
|---|---|
| Khoá | `snake_case` |
| Id | `stable key` không đổi khi rename hiển thị; kiểu chuỗi có tiền tố loại (`hero_ignis`, `stage_ch01_05`) |
| Enum | chuỗi cố định khớp enum code (`"role": "tank"`) |
| Số | integer cho giá trị gameplay tất định; tránh float cho combat (ADR-011) |
| schema_version | mọi file config có trường `schema_version` |
| Không comment | JSON chuẩn không comment; mô tả ở JSON Schema/`description` |
| Encoding | UTF-8, LF newline |
| Định dạng | 2 space indent; khoá ổn định thứ tự (dễ diff) |

**Mỗi loại config có JSON Schema** ở `shared/config-schema/` — validate ở CI (`tools/config-validator`). Ví dụ cấu trúc (minh hoạ, không phải giá trị cân bằng):

```json
{
  "schema_version": 1,
  "id": "hero_ignis",
  "faction": "flame",
  "class": "mage",
  "element": "fire",
  "role": "dps",
  "rarity": 4,
  "base_stats": { "hp": 0, "atk": 0, "def": 0, "spd": 0 },
  "skills": ["skill_ignis_basic", "skill_ignis_ult"]
}
```

> Giá trị số ở ví dụ để 0 — **giá trị cân bằng thật là việc tuning** (`../mvp/06`, `../mvp/10` EC). Code chỉ phụ thuộc **schema**, không phụ thuộc giá trị (ADR-004).

**Nguyên tắc thay đổi config:**
- Đổi giá trị → chỉ sửa dữ liệu, không sửa code.
- Đổi cấu trúc → tăng `schema_version` + migration/compat (ADR-005).
- Id không tái sử dụng cho thực thể khác.

## 2. Markdown style (tài liệu)

| Quy tắc | Chi tiết |
|---|---|
| Tiêu đề | Một `#` H1/tài liệu; phân cấp hợp lý |
| Bảng | Ưu tiên bảng hơn đoạn văn dài (theo phong cách `docs/mvp/`) |
| Mermaid | Dùng cho sơ đồ (flow/sequence/gantt) |
| WHY | Giải thích lý do, không chỉ mô tả |
| Liên kết | Dùng đường dẫn tương đối tới file khác (`../adr/...`) |
| Ngôn ngữ | Tài liệu dự án bằng **tiếng Việt** (đồng bộ `docs/mvp/`) |
| Traceability | Tham chiếu `docs/mvp/*` ở điểm phụ thuộc gameplay |
| Dòng | Không giới hạn cứng độ dài dòng; ưu tiên dễ đọc |

## 3. Cấu trúc file tài liệu
- Mỗi thư mục `docs/<area>/` có `README.md` làm index.
- Mỗi tài liệu mở đầu bằng blockquote mô tả mục đích + nguồn.
- Kết thúc bằng mục "Liên kết" trỏ tài liệu liên quan.

## 4. Liên kết
- Configuration strategy: `../adr/ADR-005-configuration-strategy.md`
- Data-driven: `../adr/ADR-004-data-driven-design.md`
- Configuration & data (gameplay): `../gameplay/configuration-and-data.md`
