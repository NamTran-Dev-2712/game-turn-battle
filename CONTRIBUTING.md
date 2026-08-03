# Đóng góp (Contributing)

Cảm ơn bạn đóng góp! Tài liệu này tóm tắt quy trình; chi tiết nằm trong [`docs/`](docs/README.md).

## Nguyên tắc vàng
1. **SSOT là tối cao.** Không đổi nghiệp vụ trong [`docs/mvp/`](docs/mvp/) hay quyết định trong [`docs/adr/`](docs/adr/). Điểm mơ hồ → ghi [`docs/mvp/10-open-questions.md`](docs/mvp/10-open-questions.md), **không tự quyết**.
2. **Tuân kiến trúc.** Dependency rule ([docs/architecture/dependency-graph.md](docs/architecture/dependency-graph.md)); cấm God Object/giant manager/switch-mở-rộng/hardcode config ([docs/ai/coding-rules.md §3](docs/ai/coding-rules.md)).
3. **Server-authoritative & data-driven.** Quyết định nhạy cảm ở server (ADR-007/011); số cân bằng ở `config/`, không hardcode (ADR-004).

## Quy trình
1. Tạo issue (dùng [template](.github/ISSUE_TEMPLATE/)) — nêu WHY + liên kết SSOT/ADR.
2. Nhánh từ `dev`: `feature/<id>-<slug>` / `fix/<id>-<slug>` ([git-conventions](docs/conventions/git-conventions.md)).
3. Code theo [STYLE_GUIDE.md](STYLE_GUIDE.md) + kèm test (nhất là combat/kinh tế).
4. Commit theo **Conventional Commits**; PR nhỏ, mô tả WHY, điền checklist DoD ([template](.github/pull_request_template.md)).
5. CI phải xanh (`ci-server`, `ci-client`, `validate-config`) + review CODEOWNERS → **squash-merge**.

## Definition of Ready / Done
- **Ready:** mục tiêu rõ, liên kết SSOT/ADR, acceptance, phạm vi module.
- **Done:** [docs/ai/review-and-dod.md §4](docs/ai/review-and-dod.md).

## Môi trường dev
Xem [SETUP.md](SETUP.md). Bật git hooks: `git config core.hooksPath .githooks`.

## Ứng xử
Tuân [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
