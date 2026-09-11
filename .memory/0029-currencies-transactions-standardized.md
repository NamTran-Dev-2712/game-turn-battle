# 0029 — Currencies + atomic transaction + idempotency standardized (Phase 31)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-10, Docker Desktop 28.5.1 + Godot 4.7.1). Nền economy: 3 tiền tệ
  (Gold/Gem/Ticket) gắn profile, **server-authoritative**; mọi cấp/tiêu qua **giao dịch atomic + idempotent +
  concurrency-safe** (ADR-007); client CHỈ hiển thị. Nền tái dùng cho gacha (33)/AFK (37)/shop (40)/mail (42).
- **Bối cảnh:** Phase 11 hoãn "bảng idempotency dùng thật" tới 31/37; Phase 30 mới có `Wallet` credit-only + idempotency
  battle theo unique `(profile_id,attempt_id)`. Phase 31 **tổng quát hoá** mẫu đó thành cơ chế giao dịch dùng chung.

## Quyết định (thiết kế, đã justify)

- **Loại tiền = enum ở boundary, mã chuỗi ở storage.** `GameTeam.Contracts.Currency` (05, None/Gold/Gem/Ticket) ở command/DTO;
  `Wallet`/ledger lưu `gold`/`gem`/`ticket` (không migrate dữ liệu Phase 30, battle `refId` giữ nguyên). Ánh xạ `CurrencyCode`
  (Application) hai chiều — KHÔNG duplicate enum.
- **Một cơ chế dùng chung `CurrencyWalletService` (Application, scoped).** Grant/Spend command handler **mỏng** uỷ cho service;
  **battle reward cũng gọi service** (thống nhất + audit). Service **không tự mở transaction** — chạy trong transaction của
  command top-level (`TransactionBehavior`) ⇒ tránh lồng transaction (`UnitOfWork` cấm nested).
- **Ledger `CurrencyTransaction` kiêm bảng idempotency.** Append-only; **unique `idempotency_key`** = backstop idempotency (tổng
  quát mẫu battle). Key đã xử lý ⇒ trả kết quả cũ (dựng lại từ `balance_after`), KHÔNG áp dụng lại. KHÔNG dựng framework
  idempotency thứ hai.
- **Concurrency = pessimistic row lock (`SELECT … FOR UPDATE`).** `IWalletRepository.GetByProfileIdForUpdateAsync` (`FromSql`).
  Chọn thay optimistic `xmin` vì semantics test sạch (loser thấy số dư mới → từ chối `INSUFFICIENT_FUNDS`, không exception) và
  **không cần cột concurrency token**. Cả grant + spend đều khoá (hai grant song song cũng lost-update nếu không khoá).
- **Thiếu tiền = `Result` lỗi, không exception** (Phase 09). Service kiểm `BalanceOf >= amount` trước, thiếu ⇒ `Result.Failure`
  không mutate. `Wallet.Spend` vẫn guard không-âm (ném) = backstop lỗi lập trình.
- **KHÔNG endpoint cấp/tiêu công khai** (lỗ hổng kinh tế). Chỉ `GET /api/v1/wallet` (đọc, protected). Grant/Spend là command
  nội bộ (test qua `ISender`/service).

## Thành phần

- **Contracts** `GameTeam.Contracts/Economy/`: `WalletDto{balances[]}`, `CurrencyBalanceDto{currency:Currency, amount}` (list,
  không map — codegen chỉ hỗ trợ mảng). Codegen sinh `wallet_dto.gd`+`currency_balance_dto.gd`; `RealSpecTests` file-list cập nhật.
- **Domain** `GameTeam.Domain/Economy/`: `Wallet` **+`Spend`** (trả số dư mới) + `WalletBalance.Subtract` (bất biến không âm);
  **`CurrencyTransaction : AggregateRoot<Guid>`** (append-only ledger: profile/currency/delta có dấu/balance_after/source/
  idempotency_key; `Record`/`Restore`; không event).
- **Application** `GameTeam.Application/Features/Economy/`: `CurrencyCode` (enum↔mã), `CurrencyWalletService`
  (`GrantAsync`/`SpendAsync`), `CurrencyErrors` (`UNAUTHENTICATED`/`PROFILE_NOT_FOUND`/`CURRENCY_INVALID`/
  `CURRENCY_INSUFFICIENT_FUNDS`), `Commands/{Grant,Spend}CurrencyCommand`(+Handler+Validator, `ITransactionalRequest`),
  `Queries/GetWalletQuery`(+Handler), `WalletMapping`, `CurrencyTransactionResult`. Ports: `ICurrencyTransactionRepository` +
  `IWalletRepository.GetByProfileIdForUpdateAsync`. DI: `CurrencyWalletService` scoped (AddApplication).
- **Battle refactor** `StartBattleCommandHandler`: thay `wallet.Credit` trực tiếp bằng `CurrencyWalletService.GrantAsync`
  (gộp theo loại tiền; key `battle:{attemptId}:{code}`; source `battle_reward`). Idempotency battle (BattleRecord) vẫn chặn retry sớm.
- **Infrastructure**: `CurrencyTransactionConfiguration` (`currency_transactions`, unique `idempotency_key`, index `profile_id`,
  FK cascade), `CurrencyTransactionRepository`, `WalletRepository.GetByProfileIdForUpdateAsync` (`FROM wallets … FOR UPDATE`),
  DI, `DbSet<CurrencyTransaction>`, migration `AddCurrencyTransactions`.
- **API**: `GET /api/v1/wallet` (version set, protected) → `GetWalletQuery`.
- **Client**: `NetworkResponseParser.parse_wallet` + `currency_code` (map enum→mã); `AuthProfileFlow._fetch_balances`
  (boot đổ số dư vào snapshot); `StateCache.apply_wallet` (refresh riêng số dư từ server); `BattlePresenter._refresh_wallet`
  (sau trận); `MainHubPresenter` hiển thị số dư thật. **KHÔNG** thêm event EventBus. **KHÔNG** currency math ở client.

## Chống tự-vẽ (binding)

- **Server là chân lý (ADR-007/011):** client CHỈ hiển thị số dư (`GET /wallet` → `StateCache`); KHÔNG tự cộng/trừ; battle reward
  → server grant → client refresh. KHÔNG endpoint cấp/tiêu công khai.
- **Reuse, đừng reinvent:** gacha/AFK/shop/mail phải dùng `CurrencyWalletService` (idempotency key + ledger) — KHÔNG cơ chế giao
  dịch/idempotency/ví thứ hai, KHÔNG duplicate enum `Currency`, KHÔNG hardcode balance (số tiền là config, ADR-004).
- **Atomic + idempotent + concurrency-safe là hợp đồng:** đổi số dư + ledger cùng transaction; unique `idempotency_key`; khoá
  dòng `FOR UPDATE`. Test integration (Testcontainers) là hợp đồng hành vi — cập nhật khi đổi.
- Ngoài scope (nợ): Fragment/Material/Energy (33/36/39); tuning số kinh tế; endpoint lịch sử giao dịch (ledger đã ghi, query sau);
  `IdempotencyBehavior` pipeline tổng quát (mẫu ledger-key đủ Phase 31); refresh-token rotation.

## Verify (2026-09-10)

- `dotnet build -c Release` 0 error (production warnings-as-error sạch); `dotnet test` — Domain **117** (Wallet 6 + CurrencyTransaction 4)
  / Application **91** (Economy: CurrencyWalletService 7 + command handler 5 + GetWallet 4) / Contracts **36** / Infrastructure **60**
  (Testcontainers pg16 `CurrencyTransactionPersistenceTests` 8: grant/spend atomic, idempotent grant/spend, retry-returns-prior,
  insufficient, **concurrency 2-spend**, rollback) / Api **80** (`WalletEndpointTests` 4: auth, ví rỗng, battle-reward→ledger,
  retry no-double + `OpenApiContractTests` `/wallet`).
- Godot 4.7.1 `--headless --import` exit 0; gdUnit4 **133/133, 0 orphan** (state_cache apply_wallet, main_hub currency, parse_wallet,
  battle_presenter wallet refresh, auth_profile_flow +wallet request).
- Codegen **41** + drift sạch (chỉ 2 file DTO mới, additive); config-validator **15 file** OK; `has-pending-model-changes` sạch;
  `openapi.json` additive (`/wallet` + WalletDto/CurrencyBalanceDto; Currency enum chỉ reorder, không mất).
- **CI-pending:** kết quả GitHub Actions (`ci-server`/`ci-client`/`codegen-check`/`validate-config`) — xanh đầy đủ ở local (gồm
  Testcontainers + Godot headless), chờ Actions.

## Liên kết

- CLAUDE.md §4.6 (Currencies block) · [[0028-battle-flow-standardized]] (battle reward → nay qua service) ·
  [[0009-persistence-standardized]] (transaction/UoW + idempotency hoãn tới 31) · [[0017-profile-persistence-standardized]]
  (save root) · [[0011-api-layer-standardized]] (error mapping) · [[0006-codegen-pipeline-standardized]] (codegen).
- Docs: `docs/gameplay/progression-and-economy.md` (ví + giao dịch), `docs/backend/infrastructure.md` §1.5,
  `docs/backend/domain-and-application.md` (Economy feature), `docs/backend/api-and-versioning.md`,
  `docs/godot/state-and-signals.md` §1.1, `docs/roadmap/31-currencies-transactions.md`.
