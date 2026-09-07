namespace GameTeam.Application.Features.Teams;

/// <summary>
/// POCO đọc cấu hình lưới đội hình từ config qua <c>IConfigProvider.Get&lt;FormationConfig&gt;("formation", id)</c>
/// (data-driven, ADR-004). Khoá JSON <c>snake_case</c> (rows/cols). Bám schema formation (phase 06/29).
/// Số ô đội hình (team size) = <see cref="Rows"/> × <see cref="Cols"/> — KHÔNG hardcode ở code.
/// </summary>
public sealed class FormationConfig
{
    /// <summary>Id cấu hình (prefix <c>formation_</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Số hàng của lưới (≥1).</summary>
    public int Rows { get; init; }

    /// <summary>Số cột của lưới (≥1).</summary>
    public int Cols { get; init; }

    /// <summary>Số ô đội hình (team size) = <see cref="Rows"/> × <see cref="Cols"/>.</summary>
    public int SlotCount => Rows * Cols;
}
