# 0002 — Dev environment standardized (Phase 04)

- Date: 2026-08-11
- Scope: tooling
- Status: Active

## Decision
The local dev environment is standardized and **closed** in roadmap Phase 04
(`docs/roadmap/04-dev-environment-tooling.md`). One-command stack via Docker Compose
(`deploy/compose/docker-compose.yml`): `postgres:16-alpine` + `redis:7-alpine` (always-on, real
healthchecks) on the **`game-team-dev`** network; the `api` service is gated behind the **`api` profile**,
builds from `server/Dockerfile`, and receives `ConnectionStrings__Postgres/__Redis`. Config comes from
**`.env`** (local-only, git-ignored) templated by **`.env.example`**. Cross-platform scripts
`scripts/dev/up.{ps1,sh}` (`-Api`/`--api`, healthcheck-driven wait, prints status) and
`down.{ps1,sh}` (`-Volumes`/`-v`; **default preserves** the pgdata volume). Liveness at `GET /health` →
`{"status":"ok"}`. Ports overridable via `.env` (`POSTGRES_PORT`/`REDIS_PORT`/`API_PORT`). Canonical
how-to + troubleshooting: `docs/deployment/README.md` → **Local development**.

Future agents **reuse** this — inspect it before creating any new local infra/compose/ports/env
conventions/startup scripts, and only **extend** (never silently replace) it when a later phase requires
it. Keep `.env.example` + compose + scripts + docs + `CLAUDE.md` §4.6 in sync on any change.

## Why
Later backend phases (11 EF/Postgres, 12 Redis, 18–22 auth/save/config, 48/54 integration) all need a
reproducible local Postgres+Redis. Fixing the conventions once (image pins, network name, profile, port
overrides, volume-safe teardown) avoids "works on my machine" drift and per-phase reinvention. Verified by
real runs (Docker 28.5.1, PowerShell + bash): healthy stack, `/health`=200, volume preserved on normal
`down`, removed on `-v`.

## Not this
Rebuilding the bootstrap-skeleton compose/scripts from scratch was rejected — Phase 04 finalized the
existing skeleton (added the explicit `game-team-dev` network, profile/wait/status/volume flags) rather
than replacing working conventions. Production infra (k8s, real secrets, migrations, seed) is explicitly
out of scope → phases 11 / 55.
