# 31 — Currencies + atomic transaction + idempotency

> Mục đích: Hiện thực **3 tiền tệ nền** (Gold/Gem/Ticket) với mọi thay đổi qua **giao dịch server atomic + idempotency** — chống double-grant/double-spend (ADR-007).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 7 Collection Core | P3 | S8 | F09 |

# Mục tiêu

Model currency gắn profile (server-authoritative); mọi cộng/trừ đi qua command atomic có idempotency key; số dư đọc qua query; client chỉ hiển thị. Nền cho gacha (33), thưởng (30/37), shop (40).

# Lý do

Currency là hệ **nhạy cảm nhất** (🔴 mvp/08). Phải atomic + idempotent trước khi bất kỳ nguồn/sink nào (gacha, reward, shop) đụng tới, tránh lỗi kinh tế không thể sửa.

# Phụ thuộc

- **Trước:** 19 (profile), 11 (transaction/idempotency nền), 30 (battle reward tối giản).
- **Sau:** 33 (gacha tiêu ticket/gem), 37 (AFK cấp gold), 38–40 (sink), 32 (inventory).

# Phạm vi

- Currency (Gold soft, Gem premium, Summon Ticket) gắn profile; loại tiền theo enum (phase 05).
- Command `GrantCurrency`/`SpendCurrency` atomic + idempotency key (không double).
- Query số dư; lịch sử giao dịch tối giản (audit).
- Ràng buộc: không âm; spend đủ số dư mới thực hiện.

# Không thuộc phạm vi

- Hero Fragment (semi-currency, Should — phase 33/39).
- Energy (phase 36).
- Số liệu giá/tỉ lệ (config).

# Deliverables

- Currency model + command grant/spend atomic + idempotency.
- Query số dư + audit log giao dịch.
- Integration test: grant/spend atomic; idempotent (retry không double); spend thiếu tiền bị chặn; concurrency an toàn.
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Công việc cần thực hiện

- [x] Domain: `Wallet`/currency trên profile; loại tiền enum (05); bất biến không âm. — `GameTeam.Domain/Economy/Wallet.cs` nay có `Spend` (trả số dư mới) + `WalletBalance.Subtract` (bất biến **không âm**, ném khi vi phạm); loại tiền = enum `GameTeam.Contracts.Currency` (05) ở boundary, lưu mã chuỗi (`gold`/`gem`/`ticket`), map `Features/Economy/CurrencyCode` — KHÔNG duplicate enum. `WalletTests` 6 + `CurrencyTransactionTests` 4 xanh.
- [x] Application: `GrantCurrencyCommand`/`SpendCurrencyCommand` (`ITransactionalRequest`) + idempotency key. — `Features/Economy/Commands/*` (record `(Currency, Amount, Source, IdempotencyKey)` + Validator amount>0/source/key) → handler **mỏng** (owner từ token `sub` — IDOR) uỷ cho `CurrencyWalletService`. KHÔNG endpoint công khai (cấp/tiêu là nội bộ). `CurrencyCommandHandlerTests` 5 xanh.
- [x] Infrastructure: bảng idempotency (dùng nền phase 11) — key đã xử lý → trả kết quả cũ, không thực hiện lại. — `CurrencyTransaction` ledger (append-only) = audit + idempotency; bảng `currency_transactions` **unique `idempotency_key`** (`CurrencyTransactionConfiguration` + migration `AddCurrencyTransactions`, `has-pending-model-changes` sạch). Service check key trước ⇒ trả `CurrencyTransactionResult` dựng lại từ `balance_after`, KHÔNG áp dụng lại. `CurrencyTransactionPersistenceTests` (Testcontainers) chứng minh.
- [x] Query `GetBalance` + ghi audit giao dịch (nguồn/sink, thời điểm server). — `GetWalletQuery` → `GET /api/v1/wallet` (protected, owner từ token; ví rỗng khi chưa có, không lỗi) trả `WalletDto{balances[]}`. Mỗi grant/spend ghi một dòng ledger (profile/currency/delta có dấu/balance_after/**source**/idempotency_key/**created_at** server `IClock`). `GetWalletQueryHandlerTests` 4 xanh.
- [x] Ràng buộc concurrency: cập nhật số dư an toàn (optimistic/row lock). — **Row lock** `IWalletRepository.GetByProfileIdForUpdateAsync` = `SELECT … FROM wallets … FOR UPDATE` (`FromSql`) tuần tự hoá cấp/tiêu đồng thời. Test concurrency (2 spend song song, balance 100→spend 80×2) ⇒ đúng 1 thành công, số dư = 20, không âm — xanh trên Postgres thật.
- [x] Integration test: atomic, idempotent (retry), spend thiếu→chặn, concurrency (2 spend song song không âm). — `CurrencyTransactionPersistenceTests` (Testcontainers pg16) **8 test**: grant/spend atomic (balance+ledger), idempotent grant/spend (retry không double), retry-returns-prior-result, insufficient (không mutate/không ledger), **concurrency** (2 spend song song), rollback (lỗi giữa transaction ⇒ balance+ledger+idempotency đều rollback). + `WalletEndpointTests` 4 (Api, HTTP thật).
- [x] Client: hiển thị số dư từ StateCache (không tự tính). — `NetworkResponseParser.parse_wallet`+`currency_code`; `AuthProfileFlow._fetch_balances` (`GET /wallet` → snapshot boot); `StateCache.apply_wallet` (refresh riêng số dư từ server; KHÔNG mutator chân lý — guard `test_no_authoritative_mutation_api_exists` vẫn xanh); `BattlePresenter._refresh_wallet` (sau trận); hub hiện số dư thật. gdUnit4 **133/133, 0 orphan**; rà soát: KHÔNG có phép cộng/trừ currency ở client.
- [x] Cập nhật `../gameplay/progression-and-economy.md`. — Thêm mục "Ví tiền tệ + giao dịch (Phase 31)": server-authoritative wallet, grant/spend atomic, idempotency (ledger unique key), concurrency (row lock), audit, client chỉ hiển thị, quan hệ battle reward, nền gacha/AFK/shop/mail. Kèm doc-sync: `infrastructure.md` §1.5, `domain-and-application.md` (Economy feature), `api-and-versioning.md`, `state-and-signals.md` §1.1, CLAUDE.md §4.6, `.memory/0029`.

# Tiêu chí hoàn thành

- Grant/spend atomic; retry cùng idempotency key → **không** double.
- Spend vượt số dư bị từ chối (số dư không âm).
- 2 thao tác song song không gây số dư sai (concurrency test).
- Client hiển thị số dư từ server; không client-authority.

# Cách kiểm tra

- `dotnet test`: atomic, idempotent, over-spend, concurrency.
- Local: battle reward (30) cộng gold → số dư đúng; gọi lại không double.
- Rà: không có phép cộng/trừ currency ở client.

# Rủi ro

- **Double-grant khi mạng chập/retry** → idempotency key bắt buộc mọi giao dịch.
- **Race condition số dư âm** → transaction + khoá/optimistic concurrency.
- **Client tự cộng tiền** → server-authoritative tuyệt đối (ADR-007).

# Ghi chú

Idempotency ở đây là mẫu tái dùng cho AFK claim (37), gacha (33), mail claim (42). Số liệu kinh tế là data-driven (config), không nằm ở code. Bám [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) + ADR-007.

# Technical Debt Review

- **Maintainability:** một cơ chế giao dịch tái dùng cho mọi nguồn/sink.
- **Scalability:** concurrency-safe; audit cho vận hành.
- **Testing:** atomic/idempotent/concurrency là hợp đồng.
- **Security:** hệ nhạy cảm nhất — server-authoritative + idempotent.
- **Nợ:** fragment (33/39); cân bằng số (tuning).

# Phase Review

**Đủ điều kiện đóng (2026-09-10, verify cục bộ — Docker Desktop 28.5.1 + Godot 4.7.1-stable).** Tất cả `# Công việc
cần thực hiện` `[x]` với evidence từ run thật; tất cả `# Tiêu chí hoàn thành` thoả (đo được):

- **Grant/spend atomic + idempotent:** đổi số dư + ghi ledger cùng một transaction (`TransactionBehavior`+`UnitOfWork`);
  retry cùng `idempotency_key` ⇒ trả kết quả cũ, KHÔNG double (unique index backstop). PASS (`CurrencyTransactionPersistenceTests`
  grant/spend atomic + idempotent grant/spend + retry-returns-prior; `WalletEndpointTests` battle retry no-double).
- **Spend vượt số dư bị từ chối, số dư không âm:** service kiểm đủ tiền trước ⇒ `CURRENCY_INSUFFICIENT_FUNDS`, không mutate;
  `Wallet.Spend`/`WalletBalance.Subtract` guard bất biến. PASS (insufficient test + `WalletTests`).
- **2 thao tác song song không sai số dư:** row lock `FOR UPDATE` ⇒ đúng 1 spend thành công, số dư = 20, không âm. PASS
  (concurrency test trên Postgres thật).
- **Client chỉ hiển thị, không client-authority:** số dư từ `GET /wallet` → `StateCache`; KHÔNG mutator chân lý (guard test);
  rà soát repo không còn currency math ở client. PASS.

Verify: build Release 0/0; `dotnet test` — Domain 117 / Application 91 / Contracts 36 / Infrastructure 60 / Api 80; codegen 41
+ drift sạch (additive); config-validator 15 OK; gdUnit4 133/133 (0 orphan); Godot import exit 0; `has-pending-model-changes`
sạch; `openapi.json` additive. Không TODO/blocker. Doc-sync + vibe-code sync hoàn tất (`.memory/0029`, CLAUDE.md §4.6).

**CI-verification pending:** kết quả GitHub Actions (`ci-server`/`ci-client`/`codegen-check`/`validate-config`) — đã xanh
đầy đủ ở local (gồm Testcontainers + Godot headless), chờ Actions xác nhận. Kết luận: **đủ điều kiện đóng.**

---

## Liên kết
- [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../backend/domain-and-application.md`](../backend/domain-and-application.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`32-inventory.md`](32-inventory.md)
