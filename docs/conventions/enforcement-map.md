# Convention Enforcement Map (Bản đồ thực thi conventions)

> Ánh xạ **mỗi quy tắc → nơi được enforce** (công cụ nào kiểm) và **trạng thái** (đang hoạt động / hoãn tới phase nào / chỉ review). Mục tiêu: biến conventions thành **cổng tự động kiểm được bằng máy**, không phụ thuộc trí nhớ con người. Chốt ở [roadmap phase 01](../roadmap/01-repo-structure-conventions.md). Nguồn quy tắc: [`code-style.md`](code-style.md), [`naming.md`](naming.md), [`data-and-docs-conventions.md`](data-and-docs-conventions.md), [`git-conventions.md`](git-conventions.md).

---

## 1. Chú giải cột

| Cột | Ý nghĩa |
|---|---|
| **Quy tắc** | Convention cụ thể + nguồn |
| **Enforce bởi** | Công cụ/cơ chế kiểm: `.editorconfig` · Roslyn analyzers (`server/Directory.Build.props`) · pre-commit hook (`.pre-commit-config.yaml`) · git hook (`.githooks/pre-commit`) · `.gitattributes` · CI (`.github/workflows/`) · NetArchTest · config-validator · GitHub settings · review |
| **Trạng thái** | `Active` (đang chạy) · `Deferred → P##` (hoãn tới phase) · `Review-only` (chưa tự động hoá được) · `Manual` (cấu hình ngoài repo) |

---

## 2. Định dạng & mã hoá (mọi loại file)

| Quy tắc | Enforce bởi | Trạng thái |
|---|---|---|
| UTF-8, LF newline, final newline, trim trailing whitespace | `.editorconfig` `[*]` + pre-commit (`end-of-file-fixer`, `trailing-whitespace`, `mixed-line-ending --fix=lf`) | Active |
| Markdown giữ trailing whitespace (2 space = line break) | `.editorconfig` `[*.{md,markdown}]` + pre-commit exclude `.md` cho trailing-whitespace | Active |
| Indent: C#/mặc định 4-space; yaml/json/jsonc & csproj/props/targets 2-space | `.editorconfig` | Active |
| GDScript `.gd` + `.tscn/.tres/.godot/.import` dùng **TAB** | `.editorconfig` `[*.gd]`, `[*.{tscn,tres,godot,import}]` | Active |
| PowerShell `.ps1/.psm1` giữ **CRLF**; `.sh` LF | `.editorconfig` + `.gitattributes` | Active |
| Không commit file > 2048 KB | pre-commit (`check-added-large-files`) | Active |
| Asset nặng qua Git LFS | `.gitattributes` (mẫu, đang comment) | Deferred → dev-env (P04) |

## 3. Code style — C# (backend)

| Quy tắc (nguồn `code-style.md` §3) | Enforce bởi | Trạng thái |
|---|---|---|
| File-scoped namespace; namespace khớp folder | `.editorconfig` (`csharp_style_namespace_declarations = file_scoped`, `dotnet_style_namespace_match_folder`) | Active |
| System usings sắp trước; using ngoài namespace | `.editorconfig` (`dotnet_sort_system_directives_first`, `csharp_using_directive_placement`) | Active |
| `_camelCase` cho private field | `.editorconfig` (naming rule `private_fields_underscore`) | Active |
| `var` khi kiểu hiển nhiên; luôn dùng braces | `.editorconfig` (suggestion) | Active |
| Nullable enable; ImplicitUsings; Deterministic build | `server/Directory.Build.props` | Active |
| Compiler warnings-as-error | `server/Directory.Build.props` (`TreatWarningsAsErrors=true`) | Active |
| Analyzer warnings-as-error | `server/Directory.Build.props` (`CodeAnalysisTreatWarningsAsErrors=false` — siết dần) | Deferred → siết dần |
| `dotnet format` xanh toàn solution | CLI `dotnet format server/GameTeam.sln --verify-no-changes`; sẽ vào CI | Active (thủ công) → CI P02 |

## 4. Code style — GDScript (client)

| Quy tắc (nguồn `code-style.md` §2) | Enforce bởi | Trạng thái |
|---|---|---|
| Indent TAB, encoding LF/UTF-8 | `.editorconfig` | Active |
| Static typing, snake_case, guard clause, không magic number | gdlint + review | Deferred → CI client (P03) / Review-only |
| Format tự động `.gd` | gdformat (gdtoolkit) trong pre-commit + CI | Deferred → dev-env (P04) / CI client (P03) |

## 5. Naming

| Quy tắc (nguồn `naming.md`) | Enforce bởi | Trạng thái |
|---|---|---|
| Private field `_camelCase` (C#) | `.editorconfig` naming rule | Active |
| 1 public type / file; PascalCase; `I`-prefix interface; `<Verb><Noun>Command`, `<...>Handler` | Roslyn analyzers (một phần) + review | Review-only (mở rộng analyzer sau) |
| Folder/file/scene/resource snake_case (client) | review | Review-only |
| Config `snake_case.json`, id `stable key` có tiền tố loại | config-validator + review | Deferred → P01/config-validator (P06–07) |

## 6. Data / Config (JSON data-driven)

| Quy tắc (nguồn `data-and-docs-conventions.md` §1) | Enforce bởi | Trạng thái |
|---|---|---|
| JSON hợp lệ, không comment | pre-commit (`check-json`) | Active |
| Indent 2-space | `.editorconfig` `[*.json]` | Active |
| Khoá snake_case; `schema_version` bắt buộc; id stable; integer cho combat | JSON Schema (`shared/config-schema/`) + config-validator | Deferred → config-validator (P06–07) |
| Referential integrity (id tham chiếu tồn tại) | config-validator (CI) | Deferred → config-validator (P06–07) |

## 7. Determinism & ranh giới kiến trúc

| Quy tắc | Enforce bởi | Trạng thái |
|---|---|---|
| Combat không dùng float/double; seeded RNG; thứ tự lặp ổn định (ADR-011) | review + golden vector test | Deferred → golden vector (P26+) / Review-only |
| Dependency rule: Domain thuần; Application ⊥ Infrastructure (ADR-003) | NetArchTest (2 luật hiện có) | Active (mở rộng → P09+) |
| Server-authoritative (không quyết định nhạy cảm ở client) | review | Review-only |

## 8. Bảo mật & Git

| Quy tắc | Enforce bởi | Trạng thái |
|---|---|---|
| Không commit secret (`.env/.pem/.key/.keystore/.p12/…`) | `.gitignore` + git hook (`.githooks/pre-commit`) + pre-commit (`detect-private-key`) | Active |
| Branch naming `<type>/<scope>-<desc>` (`git-conventions.md`) | review | Review-only |
| Conventional Commits `<type>(<scope>): <desc>` | review + PR title check | Review-only (commitlint → sau) |
| PR: CI xanh, ≥1 review, squash-merge, cấm force-push/direct commit `main` | GitHub branch protection + PR template | Manual (bật trên GitHub — `.github/project-setup.md`) |
| Central Package Management (ADR-010) | `server/Directory.Packages.props` + dependabot | Active |

## 9. Docs

| Quy tắc (nguồn `data-and-docs-conventions.md` §2–3) | Enforce bởi | Trạng thái |
|---|---|---|
| Mỗi `docs/<area>/` có README index; mở đầu blockquote, kết bằng "Liên kết" | review + rà tay (đã kiểm ở phase 01) | Review-only |
| Link nội bộ tương đối resolve được | script link-check (rà ở phase 01) + review | Active (thủ công) → CI docs sau |
| Một H1/tài liệu; ưu tiên bảng; tiếng Việt | review | Review-only |

---

## 10. Lưu ý môi trường — pre-commit

`.pre-commit-config.yaml` viết cho **pre-commit 3.x** (hook `pre-commit-hooks` v5.0.0 dùng tên stage mới `pre-commit`). Máy chỉ có **Python 3.8** chỉ cài được **pre-commit 2.x**, không đọc được manifest v5.0.0 (`InvalidManifestError`). Vì vậy ở phase 01, hiệu lực từng hook được **kiểm thủ công tương đương** (end-of-file, trailing-whitespace, mixed-line-ending, check-yaml, check-json, large-files, detect-private-key — tất cả xanh) và lệnh chuẩn vẫn là:

```bash
pre-commit run --all-files   # cần pre-commit 3.x trên Python ≥ 3.9
```

Cài đặt môi trường dev chuẩn (Python ≥ 3.9 + pre-commit 3.x + kích hoạt `git config core.hooksPath .githooks`) thuộc **phase 04 — dev environment & tooling**.

---

## 11. Liên kết
- Conventions: [`code-style.md`](code-style.md) · [`naming.md`](naming.md) · [`data-and-docs-conventions.md`](data-and-docs-conventions.md) · [`git-conventions.md`](git-conventions.md)
- Layout SSOT: [`../architecture/project-structure.md`](../architecture/project-structure.md) · ADR: [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Phase gốc: [`../roadmap/01-repo-structure-conventions.md`](../roadmap/01-repo-structure-conventions.md) · Kiểm toán bootstrap: [`../audit/bootstrap-audit.md`](../audit/bootstrap-audit.md)
