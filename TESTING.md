# Kiểm thử (Testing) — Điểm vào

> Tóm tắt. **Nguồn đầy đủ:** [`docs/testing/`](docs/testing/) — [chiến lược](docs/testing/README.md), [backend](docs/testing/backend-testing.md), [godot](docs/testing/godot-testing.md).

## Kim tự tháp test
- **Unit** (nhiều nhất): Domain rule, combat sim, helper.
- **Integration**: repository/EF/Redis (Testcontainers), API (`WebApplicationFactory`).
- **Golden vector** (đặc biệt): combat sim **client == server** (ADR-011) — lệch là fail.
- **Smoke/acceptance**: luồng chính chạy được.

## Ưu tiên theo rủi ro
Combat · gacha · AFK · currency · save — **bắt buộc** có test; deterministic (inject clock/RNG seeded).

## Chạy
```bash
# Backend
dotnet test server/GameTeam.sln           # unit + integration + architecture (NetArchTest)

# Client (khi CI/tooling sẵn sàng)
# godot --headless --path client ... (gdUnit4)
```

## Trạng thái bootstrap
- Backend: smoke test mỗi tầng + **2 architecture test** (dependency rule) — xanh.
- Client: gdUnit4 + golden vector là **TODO** (phase Core Framework). Xem [docs/audit/bootstrap-audit.md](docs/audit/bootstrap-audit.md).

## Gate CI
`ci-server` + `ci-client` + `validate-config` phải xanh trước merge ([docs/testing/README.md §4](docs/testing/README.md)).
