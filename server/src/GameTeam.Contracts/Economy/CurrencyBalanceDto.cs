using GameTeam.Contracts.Enums;

namespace GameTeam.Contracts.Economy;

/// <summary>
/// Số dư một loại tiền tệ (bản wire, server-authoritative — ADR-007). <paramref name="Currency"/> dùng enum
/// dùng chung Phase 05 (wire serialize dạng chuỗi); <paramref name="Amount"/> là integer không âm (ADR-011).
/// </summary>
/// <param name="Currency">Loại tiền tệ (<c>Gold</c>/<c>Gem</c>/<c>Ticket</c>).</param>
/// <param name="Amount">Số dư hiện tại (integer, không âm).</param>
public sealed record CurrencyBalanceDto(Currency Currency, long Amount);
