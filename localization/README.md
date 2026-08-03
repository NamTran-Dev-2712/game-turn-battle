# `localization/` (root) — Nguồn i18n (source of truth)

| Mục | Nội dung |
|---|---|
| **Purpose** | Nguồn sự thật của khoá dịch (đa ngôn ngữ — `mvp/10` UX4). |
| **Responsibilities** | Quản lý khoá dịch gốc; sinh file runtime cho `client/localization/`. |
| **Allowed** | Bảng dịch nguồn (csv/xlsx/po). |
| **Not allowed** | ❌ sửa trực tiếp file runtime client (sinh từ đây). |
| **Dependencies** | Pipeline → `client/localization/`. |
| **Owner** | Localization team. |
| **Future expansion** | Thêm ngôn ngữ, quy trình dịch. |
