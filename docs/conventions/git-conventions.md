# Git Conventions (Quy ước Git)

> Branch, commit, PR nhất quán để lịch sử rõ ràng, tự động hoá được (changelog, release) và an toàn cho nhiều người/AI cùng làm.

---

## 1. Branch naming

Định dạng: `<type>/<scope>-<short-desc>`

| Type | Dùng cho | Ví dụ |
|---|---|---|
| `feat` | Tính năng mới | `feat/battle-resim` |
| `fix` | Sửa lỗi | `fix/afk-claim-idempotency` |
| `refactor` | Tái cấu trúc, không đổi hành vi | `refactor/hero-module` |
| `docs` | Tài liệu | `docs/adr-011` |
| `test` | Thêm/sửa test | `test/gacha-pity` |
| `chore` | Việc lặt vặt/hạ tầng | `chore/ci-cache` |
| `perf` | Tối ưu hiệu năng | `perf/asset-pool` |

- Nhánh chính: `main` (luôn xanh, deploy được). Tuỳ chọn `develop` nếu cần (`../deployment/`).
- Nhánh ngắn hạn, merge nhanh; rebase/clean trước PR.

## 2. Commit convention — Conventional Commits

Định dạng: `<type>(<scope>): <mô tả ngắn>`

```
feat(battle): server-authoritative re-simulation for campaign
fix(economy): prevent double AFK claim via idempotency key
docs(adr): add ADR-011 combat authority & determinism
```

| Quy tắc | |
|---|---|
| Type | feat/fix/refactor/docs/test/chore/perf/build/ci |
| Scope | module/feature (`battle`, `hero`, `config`, `ci`) |
| Mô tả | thể mệnh lệnh, ngắn gọn, tiếng Việt hoặc Anh nhất quán trong dự án |
| Breaking change | thêm `!` sau type/scope hoặc footer `BREAKING CHANGE:` |
| Body | giải thích WHY khi cần |

> Conventional Commits cho phép **tự sinh changelog & versioning** (`../deployment/release-operations.md`).

## 3. Pull Request

| Yêu cầu PR | |
|---|---|
| Tiêu đề | Theo Conventional Commits |
| Mô tả | Vấn đề, cách giải, liên kết ADR/doc/issue, tham chiếu `docs/mvp/*` nếu liên quan gameplay |
| Kích thước | Nhỏ, một mục đích; PR lớn khó review (đặc biệt với AI) |
| Checklist | Theo `../ai/review-and-dod.md` (test, không vi phạm ranh giới, không hardcode config) |
| CI | Phải xanh (build/test/lint/validate-config) trước merge |
| Review | Ít nhất 1 review (người hoặc quy trình AI-review có kiểm chứng) |

## 4. Merge strategy
- **Squash merge** vào `main` (lịch sử gọn, 1 commit/PR theo Conventional Commits).
- Không force-push `main`. Không commit trực tiếp lên `main` (qua PR).

## 5. Tag & version
- Tag release `v<major>.<minor>.<patch>` (SemVer) — `../deployment/release-operations.md`.
- Version app tách với version API (`/api/v1`) và version config (`config@vN`).

## 6. File không commit
- Secrets (`.env`, key), output generated (`build/`, `bin/`, `obj/`, `.godot/`), tmp — theo `.gitignore` (`../architecture/project-structure.md`).

## 7. Liên kết
- Code style: `code-style.md`
- Review & DoD: `../ai/review-and-dod.md`
- Release: `../deployment/release-operations.md`
