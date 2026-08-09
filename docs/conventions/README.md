# Conventions (Chuẩn dự án)

> Bộ quy ước chung để người & AI agent viết mã/đặt tên/commit nhất quán. Tuân thủ là **bắt buộc** — CI & review sẽ kiểm (`../ai/review-and-dod.md`).

## Danh mục
| File | Nội dung |
|---|---|
| [naming.md](naming.md) | Đặt tên: folder, file, scene, script, signal, node, resource, asset, config + versioning |
| [code-style.md](code-style.md) | Code style GDScript & C#, documentation style, quy tắc determinism |
| [git-conventions.md](git-conventions.md) | Branch naming, commit convention (Conventional Commits), PR |
| [data-and-docs-conventions.md](data-and-docs-conventions.md) | JSON/config convention, Markdown style |
| [enforcement-map.md](enforcement-map.md) | Bảng "convention → nơi enforce" (editorconfig/analyzer/pre-commit/githook/CI/review) + trạng thái |

## Nguyên tắc chung
- Nhất quán > sở thích cá nhân.
- Tên phản ánh ý định (intention-revealing).
- Quy ước phải **máy kiểm được** khi có thể (editorconfig, analyzers, linters).
- Mọi ngoại lệ phải có lý do ghi trong PR.

## Liên kết
- Kiến trúc: [`../architecture/`](../architecture/) · Quyết định: [`../adr/`](../adr/)
- Glossary (ngôn ngữ chung): [`../mvp/12-glossary.md`](../mvp/12-glossary.md) — dùng đúng thuật ngữ khi đặt tên domain.
- Thực thi conventions (cổng tự động): [`enforcement-map.md`](enforcement-map.md) · [`../roadmap/01-repo-structure-conventions.md`](../roadmap/01-repo-structure-conventions.md)
