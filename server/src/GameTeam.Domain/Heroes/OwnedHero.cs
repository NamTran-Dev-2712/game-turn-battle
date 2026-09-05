using GameTeam.Domain.Common;

namespace GameTeam.Domain.Heroes;

/// <summary>
/// Một hero <b>người chơi sở hữu</b> — instance động gắn với gốc save <see cref="Profiles.PlayerProfile"/>
/// (server-authoritative, ADR-007). Định danh riêng (<see cref="Entity{TId}.Id"/>), tham chiếu
/// <see cref="ProfileId"/> (khoá ngoại) và <see cref="HeroId"/> (id definition ở config — ADR-004; chỉ số
/// tĩnh faction/class/stats KHÔNG lưu ở đây, đọc từ config qua <c>IConfigProvider</c>).
/// <para>
/// Phase 27 chỉ nền tảng ownership + cấp/sao nền: nâng cấp level/sao (35/39), gear (32), skill (28) là
/// phase sau. Client KHÔNG tự thêm/đổi owner/level/sao — mọi thay đổi qua command server.
/// </para>
/// </summary>
public sealed class OwnedHero : AggregateRoot<Guid>
{
    /// <summary>Cấp khởi tạo của một hero mới nhận (bản nền — nâng cấp ở phase 35).</summary>
    public const int InitialLevel = 1;

    /// <summary>Số sao khởi tạo của một hero mới nhận (bản nền — nâng sao ở phase 39).</summary>
    public const int InitialStars = 1;

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private OwnedHero()
    {
    }

    private OwnedHero(
        Guid id,
        Guid profileId,
        string heroId,
        int level,
        int stars,
        DateTimeOffset createdAt)
        : base(id)
    {
        ProfileId = profileId;
        HeroId = heroId;
        Level = level;
        Stars = stars;
        CreatedAt = createdAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Id definition hero ở config (prefix <c>hero_</c>, ADR-004). Không lưu chỉ số tĩnh ở đây.</summary>
    public string HeroId { get; private set; } = string.Empty;

    /// <summary>Cấp hiện tại (bản nền — nâng cấp ở phase 35).</summary>
    public int Level { get; private set; } = InitialLevel;

    /// <summary>Số sao hiện tại (bản nền — nâng sao ở phase 39).</summary>
    public int Stars { get; private set; } = InitialStars;

    /// <summary>Thời điểm nhận hero (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cấp một hero cho một profile (server-authoritative). <paramref name="id"/> do caller sinh
    /// (<c>Guid.NewGuid()</c>). Guard bất biến (id/profile không rỗng, heroId không rỗng, level/sao dương).
    /// Raise <see cref="OwnedHeroGranted"/>.
    /// </summary>
    /// <param name="id">Định danh instance hero mới (không rỗng).</param>
    /// <param name="profileId">Profile sở hữu (không rỗng).</param>
    /// <param name="heroId">Id definition hero ở config (không rỗng).</param>
    /// <param name="level">Cấp nền (dương).</param>
    /// <param name="stars">Sao nền (dương).</param>
    /// <param name="nowUtc">Server-time (từ <see cref="IClock"/>).</param>
    public static OwnedHero Grant(
        Guid id,
        Guid profileId,
        string heroId,
        int level,
        int stars,
        DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("OwnedHero id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(heroId))
        {
            throw new ArgumentException("HeroId không được rỗng.", nameof(heroId));
        }

        Guard.Positive(level);
        Guard.Positive(stars);

        OwnedHero hero = new(id, profileId, heroId, level, stars, nowUtc);
        hero.RaiseDomainEvent(new OwnedHeroGranted(id, profileId, heroId));
        return hero;
    }

    /// <summary>
    /// Dựng lại instance từ trạng thái đã lưu — KHÔNG raise event. Dùng cho phục dựng/thử nghiệm; không
    /// phải luồng cấp mới.
    /// </summary>
    public static OwnedHero Restore(
        Guid id,
        Guid profileId,
        string heroId,
        int level,
        int stars,
        DateTimeOffset createdAt)
        => new(id, profileId, heroId, level, stars, createdAt);
}
