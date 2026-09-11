using GameTeam.Contracts.Enums;

namespace GameTeam.Application.Features.Economy;

/// <summary>
/// Kết quả một giao dịch tiền tệ (nội bộ Application — không phải wire DTO). Trả về bởi
/// <see cref="CurrencyWalletService"/> và được trả lại <b>nguyên vẹn</b> khi retry idempotent (dựng lại từ
/// dòng ledger đã lưu).
/// </summary>
/// <param name="Currency">Loại tiền tệ của giao dịch.</param>
/// <param name="Delta">Biến động có dấu (dương = cấp, âm = tiêu).</param>
/// <param name="BalanceAfter">Số dư của loại tiền này sau giao dịch.</param>
/// <param name="IdempotencyKey">Khoá idempotency của giao dịch.</param>
public sealed record CurrencyTransactionResult(Currency Currency, long Delta, long BalanceAfter, string IdempotencyKey);
