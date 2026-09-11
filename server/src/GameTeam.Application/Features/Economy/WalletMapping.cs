using GameTeam.Contracts.Economy;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Economy;

namespace GameTeam.Application.Features.Economy;

/// <summary>Ánh xạ <see cref="Wallet"/> (domain) → <see cref="WalletDto"/> (wire). Chỉ liệt kê loại tiền nhận diện được.</summary>
public static class WalletMapping
{
    /// <summary>Ví rỗng (không có dòng số dư nào) — dùng khi người chơi chưa có ví.</summary>
    public static WalletDto Empty => new([]);

    /// <summary>Dựng DTO từ các dòng số dư; bỏ qua mã tiền không nhận diện (phòng thủ — dữ liệu lạ).</summary>
    public static WalletDto ToDto(Wallet wallet)
    {
        var balances = new List<CurrencyBalanceDto>();
        foreach (WalletBalance balance in wallet.Balances)
        {
            if (CurrencyCode.TryParse(balance.Currency, out Currency currency))
            {
                balances.Add(new CurrencyBalanceDto(currency, balance.Amount));
            }
        }

        return new WalletDto(balances);
    }
}
