# 01 — Cấu trúc repo & thực thi conventions

> Mục đích: Chốt và **thực thi tự động** layout repo + conventions để mọi phase sau xây trên nền nhất quán, kiểm được bằng máy.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 0 Nền tảng & Chuẩn hoá | P0 | S0 | hạ tầng |

# Mục tiêu

Đảm bảo cấu trúc monorepo (`client/ server/ shared/ config/ tools/ scripts/ deploy/ docs/ …`) khớp [`../architecture/project-structure.md`](../architecture/project-structure.md), và các conventions (naming, code-style, data/docs, git) được áp đặt qua `.editorconfig`, analyzers, pre-commit, CI — không phụ thuộc trí nhớ con người.

# Lý do

P0 bootstrap đã tạo layout, nhưng cần một phase "chốt cổng" xác nhận cấu trúc + conventions là **nguồn thực thi được kiểm chứng**, trước khi bất kỳ code nghiệp vụ nào ra đời. Sai nền ở đây lan ra toàn bộ 54 phase còn lại.

# Phụ thuộc

- **Trước:** (không có — phase mở đầu). Điều kiện tiên quyết: blueprint `docs/` đã duyệt.
- **Sau:** 02, 03, 04 (hạ tầng CI & dev env dựa trên layout này); mọi phase code tuân conventions chốt ở đây.

# Phạm vi

- Xác minh cây thư mục repo khớp `project-structure.md`; bổ sung README index còn thiếu cho từng `docs/<area>/` và thư mục gốc.
- Rà soát & hoàn thiện `.editorconfig` (C#, GDScript tab, yaml/json 2-space, LF, UTF-8) khớp [`../conventions/code-style.md`](../conventions/code-style.md).
- Xác nhận `.gitattributes`, `.gitignore`, `.githooks/`, `.pre-commit-config.yaml` hoạt động.
- Tài liệu hoá quy tắc naming ([`../conventions/naming.md`](../conventions/naming.md)) đang được enforce ở đâu (editorconfig/analyzer/review).

# Không thuộc phạm vi

- Bất kỳ logic nghiệp vụ, entity, endpoint, scene nào.
- Thay đổi quyết định kiến trúc (thuộc ADR).
- Cấu hình CI workflow chi tiết (thuộc phase 02–03).

# Deliverables

- Cây repo khớp `project-structure.md` (checklist đối chiếu đính kèm trong PR).
- `.editorconfig` hoàn thiện + pass trên toàn repo.
- `.pre-commit-config.yaml` chạy được (format/lint cơ bản).
- README index đầy đủ cho mọi thư mục cấp 1 và `docs/<area>/`.
- Ghi chú "convention enforcement map": mỗi quy tắc → công cụ enforce.

# Công việc cần thực hiện

- [ ] Đối chiếu từng thư mục cấp 1 với `project-structure.md`; liệt kê lệch (thiếu/thừa) và xử lý.
- [ ] Kiểm tra mỗi `docs/<area>/` có `README.md` mở đầu bằng blockquote mục đích + kết bằng "Liên kết" (theo [`../conventions/data-and-docs-conventions.md`](../conventions/data-and-docs-conventions.md)).
- [ ] Rà soát `.editorconfig`: C# file-scoped namespace, `_camelCase` private field, System usings first; GDScript `.gd` dùng **tab**; `.tscn/.tres/.godot/.import` tab; yaml/json 2-space; `.ps1` CRLF, `.sh` LF.
- [ ] Chạy `dotnet format --verify-no-changes` trên `server/` (nếu có lệch → format & commit).
- [ ] Xác minh `.pre-commit-config.yaml` + `.githooks/` cài được và chặn lỗi format cơ bản.
- [ ] Lập bảng "convention → nơi enforce" (editorconfig / analyzer / pre-commit / review-only) và đưa vào PR.
- [ ] Cập nhật README index còn thiếu; kiểm mọi link nội bộ resolve.

# Tiêu chí hoàn thành

- Cây repo khớp 100% `project-structure.md` (không lệch chưa giải thích).
- `dotnet format --verify-no-changes` không báo thay đổi.
- `.editorconfig` bao phủ mọi loại file đã dùng; không có quy tắc mâu thuẫn.
- Pre-commit chạy sạch trên toàn repo.
- Mọi thư mục docs có README index; link nội bộ không gãy.

# Cách kiểm tra

- `git status` sạch sau khi format.
- Chạy `dotnet format server/GameTeam.sln --verify-no-changes`.
- Chạy pre-commit toàn repo (`pre-commit run --all-files`).
- Duyệt thủ công cây thư mục so với `project-structure.md`.
- Kiểm link docs bằng công cụ link-check hoặc rà tay các README.

# Rủi ro

- **Editorconfig xung đột giữa C# và GDScript** (space vs tab) → tách rule theo glob `[*.gd]`/`[*.cs]`, test cả hai.
- **Pre-commit chặn nhầm** file generated → thêm exclude cho `build/`, `tmp/`, `obj/`, `bin/`.
- Lệch layout ẩn (thư mục rỗng không commit) → dùng `.gitkeep` + README.

# Ghi chú

Đây là phase "housekeeping có kiểm chứng", phần lớn đã đạt ở bootstrap (xem [`../audit/bootstrap-audit.md`](../audit/bootstrap-audit.md)); trọng tâm là **biến quy tắc thành cổng tự động** và vá chỗ thiếu.

# Technical Debt Review

- **Maintainability:** cao — conventions enforce bằng máy giảm tranh luận review.
- **Scalability/Perf:** không áp dụng.
- **Testing:** không có unit test nghiệp vụ; "test" ở đây là format/lint pass.
- **Documentation:** README index đầy đủ là điều kiện đóng.
- **Security:** đảm bảo `.gitignore`/`.gitattributes` không lọt secret; `.editorconfig` không nới lỏng cảnh báo bảo mật.

# Phase Review

Đóng khi: layout khớp, format/pre-commit xanh, bảng enforcement hoàn tất, docs index đủ. Không có nợ kỹ thuật mở.

---

## Liên kết
- [`../architecture/project-structure.md`](../architecture/project-structure.md) · [`../conventions/naming.md`](../conventions/naming.md) · [`../conventions/code-style.md`](../conventions/code-style.md) · [`../conventions/data-and-docs-conventions.md`](../conventions/data-and-docs-conventions.md) · [`../conventions/git-conventions.md`](../conventions/git-conventions.md)
- ADR liên quan: [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Roadmap: [`README.md`](README.md) → phase kế: [`02-ci-cd-server.md`](02-ci-cd-server.md)
