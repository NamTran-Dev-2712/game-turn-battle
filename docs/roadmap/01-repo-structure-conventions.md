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

- [x] ✅ Đối chiếu từng thư mục cấp 1 với `project-structure.md`; liệt kê lệch (thiếu/thừa) và xử lý. → xem [§ Bảng đối chiếu cây repo](#bảng-đối-chiếu-cây-repo-t1) bên dưới.
- [x] ✅ Kiểm tra mỗi `docs/<area>/` có `README.md` mở đầu bằng blockquote mục đích + kết bằng "Liên kết" (theo [`../conventions/data-and-docs-conventions.md`](../conventions/data-and-docs-conventions.md)). → tạo `architecture/README.md`, `mvp/README.md`; thêm "Liên kết" cho 7 README (adr, audit, backend, conventions, gameplay, godot, liveops); `audit/README.md` chuyển sang mở đầu blockquote.
- [x] ✅ Rà soát `.editorconfig`: C# file-scoped namespace, `_camelCase` private field, System usings first; GDScript `.gd` dùng **tab**; `.tscn/.tres/.godot/.import` tab; yaml/json 2-space; `.ps1` CRLF, `.sh` LF. → đầy đủ, không mâu thuẫn (chi tiết [`../conventions/enforcement-map.md`](../conventions/enforcement-map.md) §2–3); không cần sửa.
- [x] ✅ Chạy `dotnet format --verify-no-changes` trên `server/` (nếu có lệch → format & commit). → `dotnet format server/GameTeam.sln --verify-no-changes` **exit 0, không thay đổi**.
- [x] ✅ Xác minh `.pre-commit-config.yaml` + `.githooks/` cài được và chặn lỗi format cơ bản. → hiệu lực từng hook kiểm thủ công tương đương (đều xanh sau khi vá `README.md` thiếu newline cuối); `.githooks/pre-commit` chặn đúng file secret. Ràng buộc môi trường Python 3.8 xem [`../conventions/enforcement-map.md`](../conventions/enforcement-map.md) §10.
- [x] ✅ Lập bảng "convention → nơi enforce" (editorconfig / analyzer / pre-commit / review-only) và đưa vào PR. → [`../conventions/enforcement-map.md`](../conventions/enforcement-map.md).
- [x] ✅ Cập nhật README index còn thiếu; kiểm mọi link nội bộ resolve. → link-check: **134 file docs, 671 link nội bộ, 0 gãy**.

# Bảng đối chiếu cây repo (T1)

Đối chiếu thư mục cấp 1 (đĩa ↔ [`../architecture/project-structure.md`](../architecture/project-structure.md)). Không thư mục nào bị **thiếu**; các thư mục **ngoài §2** đều là bổ sung có chủ đích, nay đã ghi vào `project-structure.md` §9 (layout doc tự chứa 100%).

| Nhóm | Thư mục | Trạng thái |
|---|---|---|
| Sản phẩm (§2) | `client/ server/ shared/ config/ tools/ scripts/ deploy/ .github/ docs/ assets/ localization/ build/ third_party/ tmp/` | ✅ Hiện diện, khớp §2 |
| Bổ sung — đã giải thích (§9 mới) | `.claude/ .prompts/ .templates/ .context/ .rules/ .instructions/ .memory/ .tasks/ .agents/` (AI execution layer), `.githooks/`, `.vscode/`, `design/` | ✅ Ghi nhận ở `project-structure.md` §9 + `CLAUDE.md` §4 + `bootstrap-audit.md` §2 |
| Thư mục rỗng (ghost) | — | ✅ Không có (mọi thư mục tracked đều có file) |

> Kết luận: cây repo **khớp 100%**; không có lệch chưa giải thích.

# Kết quả kiểm chứng (verification)

| Kiểm tra | Lệnh / phương pháp | Kết quả |
|---|---|---|
| Format C# | `dotnet format server/GameTeam.sln --verify-no-changes` | ✅ exit 0, không thay đổi |
| `.editorconfig` bao phủ | Rà mọi loại file dùng trong repo vs section editorconfig | ✅ Đủ, không mâu thuẫn |
| pre-commit (hiệu lực hook) | Kiểm thủ công tương đương từng hook (Python 3.8 → pre-commit 2.x không đọc manifest v5.0.0) | ✅ Xanh (sau vá `README.md` thiếu newline cuối) |
| git hook secret | Dry-test logic `.githooks/pre-commit` với tên file mẫu | ✅ Chặn `.env/.pem/.key/.p12`; cho qua `.env.example`, source |
| Link nội bộ docs | Script quét `docs/**/*.md` | ✅ 134 file, 671 link, 0 gãy |
| Cây repo | Đối chiếu thủ công vs §2 (bảng trên) | ✅ Khớp 100% |
| README index | Mọi `docs/<area>/` có README (blockquote + "Liên kết") | ✅ Đủ (14/14 khu vực) |

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

**Đủ điều kiện đóng (eligible to close).** Layout khớp 100% (bảng đối chiếu T1); `dotnet format` xanh; hiệu lực pre-commit + git hook đã kiểm; bảng enforcement (`../conventions/enforcement-map.md`) hoàn tất; mọi `docs/<area>/` có README index đúng chuẩn; 671 link nội bộ resolve. Không có nợ kỹ thuật mở. Hạng mục `dotnet format`/`gdformat` vào pre-commit/CI và `pre-commit` chạy trên Python ≥ 3.9 được **hoãn có chủ đích** sang phase 02–03 (CI) / phase 04 (dev env) — đã ghi trong enforcement-map §3–4, §10.

---

## Liên kết
- [`../architecture/project-structure.md`](../architecture/project-structure.md) · [`../conventions/naming.md`](../conventions/naming.md) · [`../conventions/code-style.md`](../conventions/code-style.md) · [`../conventions/data-and-docs-conventions.md`](../conventions/data-and-docs-conventions.md) · [`../conventions/git-conventions.md`](../conventions/git-conventions.md)
- ADR liên quan: [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Roadmap: [`README.md`](README.md) → phase kế: [`02-ci-cd-server.md`](02-ci-cd-server.md)
