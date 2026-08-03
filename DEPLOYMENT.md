# Triển khai (Deployment) — Điểm vào

> Tóm tắt. **Nguồn đầy đủ:** [`docs/deployment/`](docs/deployment/) — [môi trường](docs/deployment/README.md), [CI/CD](docs/deployment/ci-cd-pipeline.md), [release/rollback](docs/deployment/release-operations.md).

## Môi trường
`dev` (local docker-compose) · `staging` · `production`. Cấu hình qua biến môi trường / GitHub Environments; **không** commit secret.

## CI/CD (GitHub Actions)
| Workflow | Khi nào |
|---|---|
| [`ci-server.yml`](.github/workflows/ci-server.yml) | PR/push `server/**` — build + test |
| [`ci-client.yml`](.github/workflows/ci-client.yml) | PR/push `client/**` — Godot (skeleton) |
| [`validate-config.yml`](.github/workflows/validate-config.yml) | PR/push `config/**` — validate |
| [`release.yml`](.github/workflows/release.yml) | tag `v*` — image + release |

## Đóng gói
- **Server:** Docker image từ [server/Dockerfile](server/Dockerfile).
- **Client:** export Android (trước), iOS (sau) — keystore/chứng chỉ ở secret, không trong repo.
- **Config:** bundle versioned publish lên Configuration Service (ADR-005).

## Local
`scripts/dev/up.sh` dựng Postgres+Redis(+API) từ [deploy/compose/docker-compose.yml](deploy/compose/docker-compose.yml).

> **Bootstrap:** workflow là skeleton; bước push registry/export/publish là TODO — xem [docs/audit/bootstrap-audit.md](docs/audit/bootstrap-audit.md).
