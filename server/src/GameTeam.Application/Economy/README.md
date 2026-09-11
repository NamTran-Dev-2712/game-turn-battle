# `Application/Economy/` — Feature kinh tế

Currency, AFK (tính server-side khi claim), shop, quest reward — giao dịch **atomic + idempotent** (ADR-007/011).

> **ĐÃ CHUYỂN → `Application/Features/Economy/`** (Phase 31): feature economy theo convention `Features/<Feature>/`
> (giống Profile/Teams/Heroes). Currency đầy đủ (`CurrencyWalletService` + `Grant/SpendCurrencyCommand` + `GetWalletQuery`)
> nằm ở đó; ví/ledger domain ở `Domain/Economy/`. AFK (37)/shop (40)/gacha reward (33) sẽ **tái dùng** `CurrencyWalletService`.

Nghiệp vụ: `../../../../docs/gameplay/progression-and-economy.md`.
