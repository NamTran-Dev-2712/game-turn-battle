using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Campaign;

/// <summary>
/// Chuỗi campaign <b>data-driven</b> (ADR-004): đọc mọi <see cref="ChapterConfig"/> qua <see cref="IConfigProvider"/>
/// và suy ra thứ tự stage + luật mở khoá <b>tuần tự</b> (server-authoritative, chống skip — ADR-007). Đây là
/// <b>nguồn thứ tự duy nhất</b>: chuỗi = các chapter theo <c>order</c> (tie-break id) rồi <c>stages</c> theo thứ
/// tự liệt kê (dedupe giữ lần đầu). Không I/O ngoài config; không hardcode danh sách stage.
/// </summary>
public sealed class CampaignChain
{
    /// <summary>Khoá loại config của chapter trong bundle (data-driven — dùng cho <see cref="IConfigProvider"/>).</summary>
    public const string ChapterConfigType = "chapter";

    private readonly IConfigProvider _config;

    public CampaignChain(IConfigProvider config) => _config = Guard.NotNull(config);

    /// <summary>
    /// Danh sách (chapter, stage) của campaign theo THỨ TỰ chơi (đã dedupe theo stage — giữ lần đầu). Rỗng nếu
    /// chưa có chapter config. Là nguồn thứ tự dùng chung cho mở khoá + AFK stage + hiển thị.
    /// </summary>
    public IReadOnlyList<CampaignStageRef> OrderedStages()
    {
        var chapters = new List<ChapterConfig>();
        foreach (string chapterId in _config.GetIds(ChapterConfigType))
        {
            ChapterConfig? chapter = _config.Get<ChapterConfig>(ChapterConfigType, chapterId);
            if (chapter is not null)
            {
                chapters.Add(chapter);
            }
        }

        var ordered = new List<CampaignStageRef>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (ChapterConfig chapter in chapters
                     .OrderBy(c => c.Order)
                     .ThenBy(c => c.Id, StringComparer.Ordinal))
        {
            foreach (string stageId in chapter.Stages)
            {
                if (seen.Add(stageId))
                {
                    ordered.Add(new CampaignStageRef(chapter.Id, stageId, ordered.Count));
                }
            }
        }

        return ordered;
    }

    /// <summary>Danh sách id stage của campaign theo THỨ TỰ chơi (đã dedupe).</summary>
    public IReadOnlyList<string> OrderedStageIds() => OrderedStages().Select(s => s.StageId).ToList();

    /// <summary>True nếu <paramref name="stageId"/> thuộc chuỗi campaign (một chapter tham chiếu nó).</summary>
    public bool IsCampaignStage(string stageId) =>
        OrderedStages().Any(s => string.Equals(s.StageId, stageId, StringComparison.Ordinal));

    /// <summary>
    /// True nếu <paramref name="stageId"/> đã mở khoá cho tập <paramref name="cleared"/>: là stage đầu chuỗi,
    /// hoặc stage liền trước trong chuỗi đã clear. Stage ngoài chuỗi ⇒ <c>false</c>.
    /// </summary>
    public bool IsUnlocked(string stageId, ISet<string> cleared)
    {
        IReadOnlyList<string> ordered = OrderedStageIds();
        int index = -1;
        for (int i = 0; i < ordered.Count; i++)
        {
            if (string.Equals(ordered[i], stageId, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return false;
        }

        return index == 0 || cleared.Contains(ordered[index - 1]);
    }

    /// <summary>
    /// "Current AFK stage" = stage đã clear <b>xa nhất</b> theo thứ tự chuỗi (cho phase 37). Rỗng nếu chưa clear
    /// stage campaign nào. Không phát minh accrual — chỉ suy vị trí tiến độ.
    /// </summary>
    public string NextAfkStageId(ISet<string> cleared)
    {
        IReadOnlyList<string> ordered = OrderedStageIds();
        string afkStageId = string.Empty;
        foreach (string stageId in ordered)
        {
            if (cleared.Contains(stageId))
            {
                afkStageId = stageId;
            }
        }

        return afkStageId;
    }
}

/// <summary>Một stage trong chuỗi campaign đã sắp thứ tự: thuộc chapter nào + chỉ số thứ tự toàn cục (0-based).</summary>
public sealed record CampaignStageRef(string ChapterId, string StageId, int Order);
