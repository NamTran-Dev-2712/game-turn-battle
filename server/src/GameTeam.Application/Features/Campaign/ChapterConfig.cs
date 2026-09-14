namespace GameTeam.Application.Features.Campaign;

/// <summary>
/// POCO đọc cấu hình chương campaign từ config qua <c>IConfigProvider.Get&lt;ChapterConfig&gt;("chapter", id)</c>
/// (data-driven, ADR-004). Khoá JSON <c>snake_case</c> (order/stages). Bám <c>chapter.schema.json</c> (phase 34).
/// Chương nhóm các stage theo THỨ TỰ ⇒ tạo chuỗi campaign tuần tự — địch/thưởng/độ khó nằm ở stage config,
/// KHÔNG đặt ở chapter.
/// </summary>
public sealed class ChapterConfig
{
    /// <summary>Id cấu hình (prefix <c>chapter_</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Tên hiển thị chương (tuỳ chọn).</summary>
    public string? Name { get; init; }

    /// <summary>Thứ tự chương trong campaign (nhỏ trước).</summary>
    public int Order { get; init; }

    /// <summary>Danh sách id stage của chương theo thứ tự chơi (<c>chapter.stages[] → stage id</c>).</summary>
    public IReadOnlyList<string> Stages { get; init; } = Array.Empty<string>();
}
