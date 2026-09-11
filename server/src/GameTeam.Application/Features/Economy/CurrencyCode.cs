using GameTeam.Contracts.Enums;

namespace GameTeam.Application.Features.Economy;

/// <summary>
/// Ánh xạ giữa enum dùng chung <see cref="Currency"/> (Phase 05 — ranh giới contract/boundary) và <b>mã
/// chuỗi</b> lưu trong <see cref="Domain.Economy.Wallet"/>/ledger (<c>gold</c>/<c>gem</c>/<c>ticket</c>).
/// Một nguồn sự thật duy nhất cho hai chiều chuyển đổi — tránh rải literal chuỗi khắp nơi; <see cref="Currency.None"/>
/// không hợp lệ cho giao dịch.
/// </summary>
public static class CurrencyCode
{
    /// <summary>Mã chuỗi lưu trữ cho một loại tiền tệ; ném nếu <paramref name="currency"/> là <c>None</c>.</summary>
    public static string ToCode(Currency currency) => currency switch
    {
        Currency.Gold => "gold",
        Currency.Gem => "gem",
        Currency.Ticket => "ticket",
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Loại tiền tệ không hợp lệ cho giao dịch."),
    };

    /// <summary>
    /// Phân giải mã chuỗi (không phân biệt hoa/thường) về <see cref="Currency"/>. Trả <c>false</c> cho mã
    /// không nhận diện (ví dụ reward không phải tiền tệ) — người gọi bỏ qua thay vì ném.
    /// </summary>
    public static bool TryParse(string? code, out Currency currency)
    {
        switch (code?.Trim().ToLowerInvariant())
        {
            case "gold":
                currency = Currency.Gold;
                return true;
            case "gem":
                currency = Currency.Gem;
                return true;
            case "ticket":
                currency = Currency.Ticket;
                return true;
            default:
                currency = Currency.None;
                return false;
        }
    }
}
