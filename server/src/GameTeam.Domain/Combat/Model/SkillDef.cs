namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Định nghĩa một skill = thành phần của các effect nguyên thủy (skill-framework.md, ADR-004).
/// <see cref="CoeffFixed"/> là hệ số sát thương fixed-point (1.0 → 1000); <see cref="TargetRule"/> là
/// chính sách chọn mục tiêu (§14, mặc định: slot nhỏ nhất). Đòn thường MVP = 1 effect <c>damage</c>.
/// </summary>
public sealed record SkillDef(string Id, int CoeffFixed, string TargetRule, IReadOnlyList<EffectDef> Effects);
