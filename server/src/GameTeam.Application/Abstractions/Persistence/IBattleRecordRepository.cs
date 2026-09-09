using GameTeam.Domain.Battles;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù cho <see cref="BattleRecord"/> (mở rộng <see cref="IRepository{TEntity,TId}"/> với tra
/// theo idempotency key). (<c>profileId</c>, <c>attemptId</c>) là unique index — dùng để nhận diện retry và
/// trả lại kết quả đã lưu mà không cấp thưởng lần hai (ADR-007). Khai báo ở Application, hiện thực ở
/// Infrastructure (DIP) — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface IBattleRecordRepository : IRepository<BattleRecord, Guid>
{
    /// <summary>Bản ghi trận theo (profile, attemptId), hoặc <c>null</c> nếu chưa có (chưa đánh lần này).</summary>
    Task<BattleRecord?> GetByProfileAndAttemptAsync(Guid profileId, string attemptId, CancellationToken cancellationToken);
}
