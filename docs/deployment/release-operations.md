# Release, Rollback, Backup & Monitoring

> Quy trình phát hành, rollback, sao lưu và giám sát. Server-authoritative → dữ liệu người chơi & config là tài sản sống còn (`../mvp/09` BE2).

---

## 1. Release process

```mermaid
flowchart LR
    Tag[Git tag vX.Y.Z] --> CI[release.yml build artifact]
    CI --> Staging[Deploy staging + smoke]
    Staging --> Migrate[DB migration - additive]
    Migrate --> Prod[Deploy production]
    Prod --> Config[Publish config bundle version]
    Config --> Verify[Post-deploy smoke/health]
```

| Nguyên tắc | Chi tiết |
|---|---|
| SemVer tag | `vX.Y.Z` (`../conventions/git-conventions.md`) |
| Migration additive-first | Tránh phá dữ liệu (`../backend/infrastructure.md`) |
| Staging trước prod | Verify trước |
| 3 version độc lập | App / API / Config (ADR-005/008) |
| Client tương thích API | Server hỗ trợ ≥1 major cũ trong chuyển đổi (ADR-008) |

## 2. Rollback

| Thành phần | Cách rollback |
|---|---|
| Backend | Redeploy image version trước |
| Config | Trỏ về `config@vN-1` (versioned, ADR-005) |
| DB migration | Ưu tiên additive để tránh rollback schema; nếu buộc, có script down + backup |
| Client | Không rollback được máy người dùng → **feature flag/kill-switch** để tắt tính năng lỗi (ADR-006) |

> **WHY flag quan trọng:** client đã phát hành không thu hồi được ngay → dựa flag để tắt nhanh (`feature-flags-and-ab-testing.md`).

## 3. Backup

| Đối tượng | Chính sách |
|---|---|
| PostgreSQL (profile) | Backup định kỳ + point-in-time (prod); test khôi phục |
| Config bundle | Versioned trong Git + store (bất biến) |
| Secrets | Sao lưu an toàn ngoài repo |
| Redis | Cache — tái tạo được; không backup như nguồn sự thật |

## 4. Monitoring & alerting

| Trụ cột | Chỉ số |
|---|---|
| Health | `/health/live`, `/health/ready` (`../backend/cross-cutting.md`) |
| Performance | Latency, error rate, sim time, DB/Redis |
| Business | Retention/funnel/source-sink (telemetry — `../mvp/09` LO2) |
| Alert | Ngưỡng lỗi/latency → cảnh báo; kill-switch sẵn sàng |

## 5. Incident & đền bù
- Sự cố → dùng **Mail system** đền bù (`../liveops/mail-system.md`); audit log admin (`../backend/cross-cutting.md`).

## 6. Liên kết
- CI/CD: `ci-cd-pipeline.md` · Config: ADR-005 · Save: ADR-007
- Flag/rollback tính năng: `../liveops/feature-flags-and-ab-testing.md`
