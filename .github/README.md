# `.github/` — CI/CD & quy trình GitHub

> Workflow tự động, template issue/PR, CODEOWNERS, dependabot. Chuẩn hoá cộng tác cho người & AI.

| Mục | Nội dung |
|---|---|
| **Purpose** | Cấu hình GitHub Actions + quy trình đóng góp. |
| **Responsibilities** | CI build/test/validate, gate merge, release; template hoá issue/PR/discussion; phân quyền review (CODEOWNERS); cập nhật dependency (dependabot). |
| **Allowed** | YAML workflow, template, cấu hình GitHub. |
| **Not allowed** | ❌ secret thật (dùng GitHub Secrets/Environments). |
| **Dependencies** | Cấu trúc `server/`, `client/`, `config/`; `docs/deployment/ci-cd-pipeline.md`. |
| **Owner** | Platform/DevOps team. |
| **Future expansion** | CodeQL/security scan, matrix build, deploy tự động. |

## Nội dung
- `workflows/` — `ci-server.yml`, `ci-client.yml`, `validate-config.yml`, `release.yml`.
- `ISSUE_TEMPLATE/`, `pull_request_template.md`, `DISCUSSION_TEMPLATE/`.
- `CODEOWNERS`, `dependabot.yml`, `labels.md`, `project-setup.md`.

> **Bootstrap:** workflow là skeleton chạy được nhưng bước nặng (Godot export, config-validator, golden vector) đánh dấu **TODO** cho phase sau — xem `docs/audit/bootstrap-audit.md`.
