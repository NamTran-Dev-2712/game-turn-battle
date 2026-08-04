# 45 — Ranking/Leaderboard (simple)

> Mục đích: Hiện thực **bảng xếp hạng đơn giản** (theo power/stage) — động lực so sánh nhẹ, server-authoritative, cache Redis.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 10 Retention & Tích hợp | P5 | S11 | F20 |

# Mục tiêu

Leaderboard theo Power Rating hoặc tiến độ campaign; cập nhật khi state đổi; đọc top N + hạng của mình; cache Redis (12) để chịu tải đọc; server-authoritative.

# Lý do

Ranking đơn giản (F20, Should) tạo động lực retention nhẹ mà không cần PvP thật (PvP real là Post-MVP). Sau khi progression/power (35/39) đã có.

# Phụ thuộc

- **Trước:** 35/39 (power), 34 (tiến độ), 12 (Redis cache).
- **Sau:** 51 (telemetry ranking), Post-MVP arena.

# Phạm vi

- Tính điểm xếp hạng (power hoặc stage) server-side; cập nhật khi state đổi.
- Sorted set Redis (hoặc query + cache) cho top N + hạng cá nhân.
- Query leaderboard (top N, quanh mình).
- Client: màn leaderboard.

# Không thuộc phạm vi

- PvP/Arena thật + mùa giải (Post-MVP — F32).
- Phần thưởng hạng phức tạp (Post-MVP).
- Chống bot/gian lận nâng cao (một phần ở phase 53).

# Deliverables

- Leaderboard tính + lưu/cache + query.
- Integration test: cập nhật hạng khi power đổi; top N đúng; hạng cá nhân đúng.
- Client màn leaderboard.
- Cập nhật [`../mvp/04-feature-analysis.md`](../mvp/04-feature-analysis.md) (F20) / progression.

# Công việc cần thực hiện

- [ ] Chọn metric (Power/stage) + tính server-side; cập nhật khi state đổi (event-driven).
- [ ] Lưu sorted set Redis (score=metric) hoặc bảng + cache; top N + rank cá nhân.
- [ ] Application: `GetLeaderboardQuery` (top N) + `GetMyRankQuery`.
- [ ] Client feature: màn leaderboard (top N + hạng mình).
- [ ] Integration test: power đổi→hạng cập nhật; top N; rank cá nhân; cache hoạt động.
- [ ] Cập nhật doc feature.

# Tiêu chí hoàn thành

- Leaderboard cập nhật khi power/tiến độ đổi; top N + hạng cá nhân đúng.
- Đọc dùng cache Redis (chịu tải); server-authoritative.
- Client hiển thị đúng.
- Test xanh.

# Cách kiểm tra

- `dotnet test`: cập nhật hạng, top N, rank cá nhân, cache.
- Local: nâng power → hạng thay đổi.
- gdUnit4: màn leaderboard.

# Rủi ro

- **Tính hạng nặng khi nhiều người** → sorted set Redis O(log n); cache.
- **Điểm giả (cheat power)** → power dựa state server-authoritative; hardening 53.
- **Cache lệch** → cập nhật cache khi state đổi; TTL hợp lý.

# Ghi chú

Đơn giản, không PvP. Arena/mùa giải là Post-MVP. Bám mvp/04 (F20). Cache theo phase 12.

# Technical Debt Review

- **Maintainability:** metric + query tách rõ.
- **Scalability:** Redis sorted set chịu tải đọc.
- **Testing:** cập nhật/top N/rank có test.
- **Security:** power server-authoritative; chống cheat ở 53.
- **Nợ:** arena/mùa giải (Post-MVP).

# Phase Review

Đóng khi leaderboard cập nhật + top N/rank + cache + server-authoritative, test xanh.

---

## Liên kết
- [`../mvp/04-feature-analysis.md`](../mvp/04-feature-analysis.md) · [`../backend/infrastructure.md`](../backend/infrastructure.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`46-tutorial-onboarding.md`](46-tutorial-onboarding.md)
