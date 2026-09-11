namespace GameTeam.Contracts.Economy;

/// <summary>
/// Ví tiền tệ của người chơi (bản wire chỉ-đọc, server-authoritative — ADR-007). Client hiển thị số dư từ
/// đây (qua <c>StateCache</c>), KHÔNG tự cộng/trừ. Danh sách các dòng số dư (thay cho map — codegen client
/// chỉ hỗ trợ mảng đối tượng); chỉ liệt kê loại tiền có số dư.
/// </summary>
/// <param name="Balances">Các dòng số dư theo loại tiền tệ.</param>
public sealed record WalletDto(IReadOnlyList<CurrencyBalanceDto> Balances);
