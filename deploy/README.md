# `deploy/` — Hạ tầng như mã (IaC)

> Định nghĩa cách tái lập môi trường: Docker, docker-compose local, và (tương lai) Kubernetes.

| Mục | Nội dung |
|---|---|
| **Purpose** | Chứa artifact triển khai/hạ tầng để tái lập môi trường nhất quán. |
| **Responsibilities** | `docker/` Dockerfile phụ; `compose/` stack local (postgres+redis+api); `k8s/` manifest tương lai. |
| **Allowed** | Dockerfile, compose, manifest, IaC. |
| **Not allowed** | ❌ secret thật (dùng `.env` local / GitHub Environments). |
| **Dependencies** | `server/Dockerfile` (image API), `.env`. |
| **Owner** | DevOps/platform team. |
| **Future expansion** | k8s, IaC (Terraform), multi-env. |

Chi tiết: `../docs/deployment/README.md`, `../docs/deployment/release-operations.md`.
