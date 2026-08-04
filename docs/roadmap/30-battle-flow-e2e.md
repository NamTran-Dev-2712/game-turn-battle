# 30 — Battle flow end-to-end (re-sim → BattleResult{seed} → replay)

> Mục đích: Nối trọn luồng trận: client gửi intent (team, stage) → server sinh seed, **re-sim** quyết kết quả + cấp thưởng (transaction) → trả `BattleResult{seed,outcome,rewards,log}` → client **replay bằng seed**. Đây là lát cắt dọc chơi được đầu tiên (P2).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 6 Gameplay Vertical Slice | P2 | S7 | F04 |

# Mục tiêu

Endpoint `POST /api/v1/battles` nhận (teamId, stageId) → server snapshot team + sinh seed + re-sim (phase 24) + ghi kết quả/thưởng atomic → trả BattleResult; client dùng seed + sim client (25) replay khớp; hiển thị trận + kết quả.

# Lý do

ADR-011 flow chính: server-authoritative + re-sim + client replay bằng seed. Đây là cột mốc **"đánh 1 stage chơi được"** (P2 acceptance): kết thúc nhóm 6, game có lát cắt dọc thật.

# Phụ thuộc

- **Trước:** 24 (sim server), 25 (sim client), 29 (team/formation), 27–28 (hero/skill), 21 (config), 11 (transaction).
- **Sau:** 31–33 (collection dùng thưởng), 34 (campaign nhiều stage), 43 (sweep).

# Phạm vi

- Server: `StartBattleCommand` → snapshot team + seed + re-sim + ghi kết quả/thưởng (transaction, idempotency nền) → BattleResult.
- Client: gửi intent → nhận BattleResult → replay bằng seed (sim client) → hiển thị trận + màn kết quả/thưởng.
- Reward tối giản (config-driven) — cấp qua server; client chỉ hiển thị.
- Kiểm khớp: client replay ≡ kết quả server (cùng seed).

# Không thuộc phạm vi

- Nhiều stage/campaign chain (phase 34).
- Sweep/quick-battle (phase 43).
- Currency/inventory đầy đủ (phase 31–33) — reward tối giản ở đây.

# Deliverables

- Endpoint battle + BattleResult contract.
- Client: gọi trận, replay seed, màn kết quả.
- Test: server re-sim quyết kết quả; client replay khớp seed; thưởng cấp atomic (server), idempotent.
- Cập nhật [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) + [`../architecture/overview.md`](../architecture/overview.md) (flow).

# Công việc cần thực hiện

- [ ] Contract: `StartBattleRequest{teamId,stageId}` → `BattleResult{seed,outcome,rewards,log}` (mở rộng phase 05) + codegen.
- [ ] Server `StartBattleCommand`: lấy team snapshot (29) + stage config (21) → sinh seed → re-sim (24) → xác định outcome/rewards.
- [ ] Ghi kết quả + cấp thưởng trong **transaction** (11); idempotency key chống double-grant (nền 11, dùng đầy đủ 31).
- [ ] Trả BattleResult (seed + log + rewards).
- [ ] Client feature `battle/`: gửi intent → nhận result → replay bằng seed (25) → vẽ trận + màn kết quả/thưởng.
- [ ] Kiểm khớp: assert client replay outcome ≡ server outcome (cùng seed) trong test.
- [ ] Test integration server (re-sim, transaction, idempotent) + gdUnit4 client (replay khớp).
- [ ] Cập nhật `../gameplay/combat-framework.md` + `../architecture/overview.md`.

# Tiêu chí hoàn thành

- Đánh 1 stage cố định: **server re-sim quyết kết quả**; client replay bằng seed **khớp** (thắng/thua + diễn biến).
- Thưởng cấp bởi server (atomic, idempotent) — client không tự cấp.
- Gọi lại cùng battle (retry) không cấp thưởng lần hai (idempotency).
- Test server + client xanh; golden gate không đỏ.

# Cách kiểm tra

- Local: chạy server+client, đánh stage demo → xem replay → kết quả khớp server; thưởng vào profile.
- `dotnet test`: re-sim quyết outcome; transaction rollback khi lỗi; idempotent.
- gdUnit4: replay bằng seed ≡ outcome server.
- Retry battle → không double reward.

# Rủi ro

- **Client replay lệch server** → cùng spec/golden (23/26); dùng đúng config version server trả.
- **Double-grant khi retry/mạng chập** → idempotency key bắt buộc (ADR-007).
- **Ghi thưởng nửa chừng** → transaction atomic; rollback khi lỗi.

# Ghi chú

Đây là **P2 acceptance**: "đánh 1 stage, server-authoritative, client replay khớp seed". Reward tối giản; currency/inventory hoàn chỉnh ở nhóm 7. Bám ADR-011/007 + [`../architecture/overview.md`](../architecture/overview.md) (sequence diagram).

# Technical Debt Review

- **Maintainability:** flow rõ, tách sim/thưởng/hiển thị.
- **Scalability:** nền cho campaign/sweep tái dùng.
- **Testing:** re-sim + replay khớp + idempotent là hợp đồng.
- **Security:** server-authoritative + idempotency (chống cheat/double-grant).
- **Nợ:** reward đầy đủ (31–33); nhiều stage (34).

# Phase Review

Đóng khi battle e2e chạy (server re-sim + client replay khớp + thưởng atomic idempotent), test hai phía xanh. **Cột mốc P2 — game có lát cắt dọc chơi được.**

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../architecture/overview.md`](../architecture/overview.md) · [`../backend/domain-and-application.md`](../backend/domain-and-application.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md)
- Roadmap: [`README.md`](README.md) → kế: [`31-currencies-transactions.md`](31-currencies-transactions.md)
