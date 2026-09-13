using GameTeam.Domain.Gacha;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù cho <see cref="SummonRecord"/> (mở rộng <see cref="IRepository{TEntity,TId}"/> với tra
/// theo idempotency key). (<c>profileId</c>, <c>requestId</c>) là unique index — dùng để nhận diện retry và trả
/// lại kết quả đã lưu mà không quay/tiêu/cấp lần hai (ADR-007). Khai báo ở Application, hiện thực ở
/// Infrastructure (DIP) — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface ISummonRecordRepository : IRepository<SummonRecord, Guid>
{
    /// <summary>Bản ghi triệu hồi theo (profile, requestId), hoặc <c>null</c> nếu chưa có (chưa quay lượt này).</summary>
    Task<SummonRecord?> GetByProfileAndRequestAsync(Guid profileId, string requestId, CancellationToken cancellationToken);
}
