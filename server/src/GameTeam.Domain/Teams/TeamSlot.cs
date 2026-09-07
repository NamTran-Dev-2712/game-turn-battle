namespace GameTeam.Domain.Teams;

/// <summary>
/// Một ô trong đội hình — cặp (vị trí lưới, hero). <see cref="SlotIndex"/> là chỉ số ô 0-based (ánh xạ
/// lưới hàng×cột từ config formation; cũng chính là <c>slot</c> đưa vào combat sim — vị trí nhỏ hơn bị
/// nhắm trước theo target/aggro, combat-framework §14). <see cref="HeroId"/> tham chiếu id definition ở
/// config (một hero người chơi <b>sở hữu</b> — kiểm ở tầng Application). Bất biến (immutable).
/// </summary>
public sealed class TeamSlot
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private TeamSlot()
    {
    }

    /// <summary>Dựng một ô đội hình. Guard bất biến cấu trúc (chỉ số ≥ 0, heroId không rỗng).</summary>
    /// <param name="slotIndex">Chỉ số ô 0-based (không âm).</param>
    /// <param name="heroId">Id definition hero ở config (không rỗng).</param>
    public TeamSlot(int slotIndex, string heroId)
    {
        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "SlotIndex không được âm.");
        }

        if (string.IsNullOrWhiteSpace(heroId))
        {
            throw new ArgumentException("HeroId không được rỗng.", nameof(heroId));
        }

        SlotIndex = slotIndex;
        HeroId = heroId;
    }

    /// <summary>Chỉ số ô 0-based (vị trí lưới = slot combat).</summary>
    public int SlotIndex { get; private set; }

    /// <summary>Id definition hero ở config (prefix <c>hero_</c>, ADR-004).</summary>
    public string HeroId { get; private set; } = string.Empty;
}
