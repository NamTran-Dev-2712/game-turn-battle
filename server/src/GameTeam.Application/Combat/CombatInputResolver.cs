using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Common;

namespace GameTeam.Application.Combat;

/// <summary>
/// Dựng <see cref="BattleInput"/> thuần từ config (hero/skill/stage) đọc qua <see cref="IConfigProvider"/> —
/// tầng <b>data-driven</b> nối config với sim thuần ở Domain (ADR-004/011). Không hardcode chỉ số: đổi
/// giá trị trong config ⇒ kết quả sim đổi, không cần sửa code. Không I/O, không wall-clock.
/// </summary>
public sealed class CombatInputResolver
{
    /// <summary>Khoá config type cho hero.</summary>
    public const string HeroType = "hero";

    /// <summary>Khoá config type cho skill.</summary>
    public const string SkillType = "skill";

    /// <summary>Khoá config type cho stage.</summary>
    public const string StageType = "stage";

    private const string TeamAlly = "ally";
    private const string TeamEnemy = "enemy";

    private readonly IConfigProvider _config;

    /// <summary>Khởi tạo với config provider (Phase 21 <c>RuntimeConfigProvider</c>).</summary>
    public CombatInputResolver(IConfigProvider config) => _config = Guard.NotNull(config);

    /// <summary>Dựng đầu vào trận; trả <see cref="Result{T}"/> lỗi nếu thiếu config (không đoán mặc định).</summary>
    public Result<BattleInput> Resolve(BattleRequest request)
    {
        Guard.NotNull(request);

        StageCombatConfig? stage = _config.Get<StageCombatConfig>(StageType, request.StageId);
        if (stage is null)
        {
            return Result.Failure<BattleInput>(CombatErrors.StageNotFound(request.StageId));
        }

        SkillCombatConfig? skill = _config.Get<SkillCombatConfig>(SkillType, stage.BasicSkillId);
        if (skill is null)
        {
            return Result.Failure<BattleInput>(CombatErrors.SkillNotFound(stage.BasicSkillId));
        }

        var ally = new List<UnitSnapshot>(request.Ally.Count);
        foreach (CombatTeamMember member in request.Ally)
        {
            Result<UnitSnapshot> unit = BuildUnit(member.ActorId, member.HeroId, TeamAlly, member.Slot);
            if (unit.IsFailure)
            {
                return Result.Failure<BattleInput>(unit.Error);
            }

            ally.Add(unit.Value);
        }

        var enemy = new List<UnitSnapshot>(stage.Enemies.Count);
        foreach (StageEnemyConfig enemyConfig in stage.Enemies)
        {
            Result<UnitSnapshot> unit = BuildUnit(enemyConfig.ActorId, enemyConfig.HeroId, TeamEnemy, enemyConfig.Slot);
            if (unit.IsFailure)
            {
                return Result.Failure<BattleInput>(unit.Error);
            }

            enemy.Add(unit.Value);
        }

        CombatRulesConfig rulesConfig = stage.CombatRules;
        var rules = new CombatRules(
            rulesConfig.DefConstantK,
            rulesConfig.MinDamage,
            rulesConfig.CritMultiplierFixed,
            rulesConfig.AccuracyBp,
            rulesConfig.CritRateBp,
            stage.MaxRounds,
            new EnergyRules(
                rulesConfig.Energy.Initial,
                rulesConfig.Energy.OnAttack,
                rulesConfig.Energy.OnHit,
                rulesConfig.Energy.UltimateCost,
                rulesConfig.Energy.Max));

        IReadOnlyList<EffectDef> effects = skill.Effects.Count > 0
            ? skill.Effects.Select(type => new EffectDef(type)).ToList()
            : new List<EffectDef> { new(DamageEffectHandler.TypeName) };

        var basicSkill = new SkillDef(stage.BasicSkillId, skill.CoeffFixed, skill.TargetRule, effects);

        string configVersion = $"config@v{_config.CurrentVersion.Bundle}";
        var input = new BattleInput(
            configVersion,
            request.Seed,
            new StageInfo(request.StageId, stage.MaxRounds),
            ally,
            enemy,
            rules,
            basicSkill);

        return Result.Success(input);
    }

    private Result<UnitSnapshot> BuildUnit(string actorId, string heroId, string team, int slot)
    {
        HeroCombatConfig? hero = _config.Get<HeroCombatConfig>(HeroType, heroId);
        if (hero is null)
        {
            return Result.Failure<UnitSnapshot>(CombatErrors.HeroNotFound(heroId));
        }

        var snapshot = new UnitSnapshot(actorId, heroId, team, slot, new UnitStats(hero.Hp, hero.Atk, hero.Def, hero.Spd));
        return Result.Success(snapshot);
    }
}
