namespace GameTeam.Domain.Combat.Model;

/// <summary>Thông tin màn: định danh + số vòng tối đa (đạt max ⇒ DRAW — §19).</summary>
public sealed record StageInfo(string Id, int MaxRounds);
