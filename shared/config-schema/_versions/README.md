# Config schema — quy tắc versioning & migration

> Nền migration cho JSON Schema config (ADR-005). Thư mục này ghi **quy tắc** đổi schema và lưu **migration** khi có thay đổi breaking. Phase 06 lập nền; tool validate là phase 07, Config Service runtime là phase 21.

---

## 1. Nhận diện phiên bản schema

- Mỗi file config gameplay có trường bắt buộc `schema_version` (integer ≥ 1) — xem `../common.schema.json#/$defs/schema_version`.
- `schema_version` gắn với **cấu trúc** của từng loại config (hero/skill/stage/…), độc lập với `config_version` (`config@vN`) của **bundle giá trị**.
- Bundle envelope: `../config-bundle.schema.json` (`config_version` khớp `^config@v[0-9]+$`).

## 2. Thay đổi additive (KHÔNG tăng `schema_version`)

Cho phép và không phá tương thích:
- Thêm field **optional** mới (không nằm trong `required`).
- Thêm **giá trị enum** mới (vd class/element/role/currency mới, effect_type mới, condition_type mới).
- Thêm loại config mới (schema mới).

> Additive-only là quy tắc chung của contract dự án (khớp `.memory/0003` — shared contracts). Nới lỏng ràng buộc (bỏ `required`, mở `additionalProperties`) cũng là additive.

## 3. Thay đổi breaking (BẮT BUỘC tăng `schema_version`)

Phá tương thích với config cũ:
- Xoá / đổi tên field.
- Đưa field đang optional vào `required`.
- Thắt chặt kiểu (vd string → integer) hoặc thu hẹp enum (xoá giá trị).
- Đổi **ý nghĩa** field/enum hiện có.
- Đổi pattern ID theo hướng loại bỏ id đang hợp lệ.

## 4. Chuỗi quy tắc khi breaking

```
đổi schema breaking
        ↓
tăng schema_version (file loại đó)
        ↓
thêm migration trong shared/config-schema/_versions/
        ↓
doc-sync: ../../.. → docs/gameplay/configuration-and-data.md + docs/liveops/remote-config.md
```

- Mỗi migration đặt tên `<type>-v<from>-to-v<to>.md`, mô tả: field đổi, cách map dữ liệu cũ → mới, ảnh hưởng runtime.
- Phase 06 chưa có migration nào (mọi schema khởi tạo ở `schema_version: 1`).

## 5. Quan hệ với các phase sau

- **Phase 07 (validator):** đọc `schema_version` để chọn schema đúng; kiểm referential integrity chéo file (hero→skill, stage→reward…) — **không** thuộc phase 06.
- **Phase 21 (Configuration Service):** publish bundle `config@vN` bất biến; rollback = trỏ version cũ; validate trước khi publish (cùng logic phase 07).

## 6. Liên kết
- `../README.md` · `../common.schema.json` · `../config-bundle.schema.json`
- `../../../docs/conventions/data-and-docs-conventions.md` · `../../../docs/gameplay/configuration-and-data.md`
- `../../../docs/liveops/remote-config.md`
- ADR-004 (data-driven), ADR-005 (configuration strategy)
- Roadmap: `../../../docs/roadmap/06-config-json-schema.md` → `../../../docs/roadmap/07-config-validator-tool.md`
