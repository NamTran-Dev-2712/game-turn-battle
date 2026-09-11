using GameTeam.Domain.Economy;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository cho sổ cái giao dịch tiền tệ <see cref="CurrencyTransaction"/> (mở rộng
/// <see cref="IRepository{TEntity,TId}"/> với tra theo idempotency key). <c>idempotency_key</c> là unique
/// index — dùng để nhận diện retry và trả lại kết quả đã lưu mà KHÔNG áp dụng lần hai (ADR-007). Khai báo ở
/// Application, hiện thực ở Infrastructure (DIP) — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface ICurrencyTransactionRepository : IRepository<CurrencyTransaction, Guid>
{
    /// <summary>Dòng ledger theo idempotency key, hoặc <c>null</c> nếu key chưa từng xử lý.</summary>
    Task<CurrencyTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
}
