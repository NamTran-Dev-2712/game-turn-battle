# 53 — Security pass (anti-cheat, authz, idempotency audit)

> Mục đích: **Rà soát bảo mật** toàn diện: authz đúng chủ sở hữu, chống cheat (server-authority + determinism), audit idempotency, chống lạm dụng — trước soft-launch.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 12 Polish & Release | P7 | S13 | polish |

# Mục tiêu

Đảm bảo mọi hệ nhạy cảm (currency/summon/AFK/battle/progression — tiers mvp/08) là server-authoritative, authz theo `sub`, idempotent; thêm rate limiting/chống lạm dụng; rà không rò secret; checklist security đạt.

# Lý do

Game F2P + gacha + economy là mục tiêu gian lận. mvp/08 phân tier nhạy cảm (🔴 Currency/Summon/AFK). Trước soft-launch phải khoá các lỗ hổng cheat/authz/double-grant (D7, mvp/15).

# Phụ thuộc

- **Trước:** 31/33/37/30 (hệ nhạy cảm), 48 (idempotency), 18 (authz).
- **Sau:** 54 (regression), 55 (release).

# Phạm vi

- Rà authz: mọi endpoint state → chỉ thao tác dữ liệu của `sub` trong token; không IDOR.
- Xác nhận cheat surface: kết quả/thưởng/RNG/AFK/energy đều server-side; client không authority (grep + test).
- Audit idempotency mọi command nhạy cảm (double-grant/double-spend).
- Rate limiting cơ bản (auth, summon, claim) chống lạm dụng.
- Rà secret: không key/JWT/token trong source/log; `.env`/`*.pem` bị deny (settings).

# Không thuộc phạm vi

- Pentest bên ngoài đầy đủ (có thể thuê Post-MVP).
- Anti-cheat nâng cao/ML (Post-MVP).
- Compliance pháp lý sâu (DP4 — phối hợp riêng).

# Deliverables

- Security checklist hoàn tất (authz, cheat surface, idempotency, secret, rate limit).
- Test bảo mật: IDOR bị chặn; double-grant bị chặn; client-authority không tồn tại.
- Rate limiting cơ bản.
- Báo cáo security pass + rủi ro còn lại.
- Cập nhật [`../mvp/08-technical-impact.md`](../mvp/08-technical-impact.md) / audit.

# Công việc cần thực hiện

- [ ] Rà từng endpoint state: authz theo `sub`; test IDOR (đọc/ghi dữ liệu người khác → 403).
- [ ] Grep + review: không có quyết định kết quả/thưởng/RNG ở client; combat server re-sim (30); AFK/energy server-time.
- [ ] Audit idempotency: bảng mọi command nhạy cảm (battle/summon/claim/purchase/afk) có key + test retry không double.
- [ ] Rate limiting cơ bản (auth guest, summon, claim) chống spam/lạm dụng.
- [ ] Rà secret: không key/JWT/token trong source/appsettings commit/log; xác nhận deny `.env`/`*.pem` (`.claude/settings.json`).
- [ ] Chạy security-review (skill/agent) trên nhánh; xử lý phát hiện.
- [ ] Báo cáo + cập nhật `../mvp/08-technical-impact.md` (tiers) / audit.

# Tiêu chí hoàn thành

- IDOR bị chặn (test đọc/ghi chéo → 403).
- Không tồn tại client-authority cho kết quả/thưởng/RNG (grep + test).
- Mọi command nhạy cảm idempotent (audit + test retry).
- Rate limiting cơ bản hoạt động; không secret trong source/log.
- Security checklist đạt; rủi ro còn lại ghi rõ.

# Cách kiểm tra

- Integration test: IDOR 403; retry double-grant bị chặn; rate limit chặn spam.
- Grep/review: không random/quyết thưởng ở client; không secret.
- Chạy `/security-review` trên nhánh → xử lý findings.

# Rủi ro

- **Bỏ sót surface** → checklist theo tiers mvp/08 + test có hệ thống.
- **Rate limit chặn nhầm người thật** → ngưỡng hợp lý + theo dõi.
- **Secret lọt lịch sử git** → rà history; xoay key nếu lộ.

# Ghi chú

Server-authority + determinism (ADR-011/007/008) là nền chống cheat; phase này xác nhận toàn diện + vá. Pentest ngoài/anti-cheat nâng cao là Post-MVP. Bám mvp/08 (tiers) + mvp/15 (D7).

# Technical Debt Review

- **Maintainability:** authz/idempotency nhất quán.
- **Scalability:** rate limit bảo vệ khi tải/tấn công.
- **Testing:** IDOR/double-grant/rate-limit có test.
- **Security:** trọng tâm phase — khoá cheat surface.
- **Nợ:** pentest ngoài, anti-cheat nâng cao (Post-MVP).

# Phase Review

Đóng khi authz/cheat-surface/idempotency/secret/rate-limit rà xong + test bảo mật xanh + checklist đạt.

---

## Liên kết
- [`../mvp/08-technical-impact.md`](../mvp/08-technical-impact.md) · [`../mvp/15-next-phase.md`](../mvp/15-next-phase.md) · [`../backend/cross-cutting.md`](../backend/cross-cutting.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md)
- Roadmap: [`README.md`](README.md) → kế: [`54-regression-smoke.md`](54-regression-smoke.md)
