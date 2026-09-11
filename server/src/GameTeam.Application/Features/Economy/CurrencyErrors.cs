using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Economy;

/// <summary>Lỗi nghiệp vụ của hệ tiền tệ (mã ổn định — ánh xạ HTTP bởi <c>ErrorHttpMapping</c>).</summary>
public static class CurrencyErrors
{
    /// <summary>Chưa xác thực (không có token hợp lệ) → 401.</summary>
    public static Error Unauthenticated => new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Không tìm thấy profile của người gọi → 404.</summary>
    public static Error ProfileNotFound => new("PROFILE_NOT_FOUND", "Không tìm thấy profile của người gọi.");

    /// <summary>Loại tiền tệ không hợp lệ (None/không nhận diện) → 400.</summary>
    public static Error InvalidCurrency => new("CURRENCY_INVALID", "Loại tiền tệ không hợp lệ.");

    /// <summary>Số dư không đủ để tiêu → 409 (lỗi nghiệp vụ mong đợi; KHÔNG làm thay đổi số dư).</summary>
    public static Error InsufficientFunds =>
        new("CURRENCY_INSUFFICIENT_FUNDS", "Số dư không đủ để thực hiện giao dịch.");
}
