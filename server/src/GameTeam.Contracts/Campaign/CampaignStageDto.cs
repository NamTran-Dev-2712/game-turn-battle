namespace GameTeam.Contracts.Campaign;

/// <summary>
/// Trạng thái một stage campaign cho client hiển thị (server-authoritative, ADR-007). <paramref name="Order"/>
/// là chỉ số thứ tự toàn cục trong chuỗi (0-based, ổn định để sắp xếp). <paramref name="Cleared"/>/
/// <paramref name="Unlocked"/> do server tính từ tiến độ + chuỗi chapter — client KHÔNG tự suy.
/// </summary>
/// <param name="StageId">Id stage ở config (prefix <c>stage_</c>).</param>
/// <param name="ChapterId">Id chapter chứa stage (prefix <c>chapter_</c>).</param>
/// <param name="Order">Chỉ số thứ tự toàn cục trong chuỗi campaign (0-based).</param>
/// <param name="Cleared">Đã clear (first-clear) hay chưa.</param>
/// <param name="Unlocked">Đã mở khoá để đánh hay chưa (tuần tự — chống skip).</param>
public sealed record CampaignStageDto(string StageId, string ChapterId, int Order, bool Cleared, bool Unlocked);
