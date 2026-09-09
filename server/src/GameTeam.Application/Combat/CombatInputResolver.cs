using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Common;

namespace GameTeam.Application.Combat;

/// <summary>
/// Dựng <see cref="BattleInput"/> thuần từ <b>gameplay config</b> (hero/skill/stage) đọc qua
/// <see cref="IConfigProvider"/> — tầng <b>data-driven</b> nối config với sim thuần ở Domain (ADR-004/011).
/// Không hardcode chỉ số: đổi giá trị trong config ⇒ kết quả sim đổi, không cần sửa code. Không I/O, không
/// wall-clock. Phase 30 nối <c>config/heroes|skills|stages/*.json</c> thật vào trận (base_stats lồng,
/// <c>hero.skills[]</c> → basic/ultimate, coeff của effect <c>damage</c> nâng lên cấp skill).
/// </summary>
public sealed class CombatInputResolver
{
    /// <summary>Khoá config type cho hero.</summary>
    public const string HeroType = "hero";

    /// <summary>Khoá config type cho skill.</summary>
    public const string SkillType = "skill";

    /// <summary>Khoá config type cho stage.</summary>
    public const string StageType = "stage";

    /// <summary>Prefix định danh địch (suy theo thứ tự — tất định).</summary>
    public const string EnemyActorPrefix = "enemy_";

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

        Result<SkillDef> basicSkillResult = ResolveSkill(stage.BasicSkillId);
        if (basicSkillResult.IsFailure)
        {
            return Result.Failure<BattleInput>(basicSkillResult.Error);
        }

        SkillDef basicSkill = basicSkillResult.Value;

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
        for (int i = 0; i < stage.Enemies.Count; i++)
        {
            StageEnemyConfig enemyConfig = stage.Enemies[i];
            string actorId = $"{EnemyActorPrefix}{i}";
            int slot = enemyConfig.Slot ?? i;
            Result<UnitSnapshot> unit = BuildUnit(actorId, enemyConfig.HeroId, TeamEnemy, slot);
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

        Result<UnitSkillSet?> skills = BuildUnitSkills(hero);
        if (skills.IsFailure)
        {
            return Result.Failure<UnitSnapshot>(skills.Error);
        }

        var snapshot = new UnitSnapshot(
            actorId,
            heroId,
            team,
            slot,
            new UnitStats(hero.BaseStats.Hp, hero.BaseStats.Atk, hero.BaseStats.Def, hero.BaseStats.Spd),
            skills.Value);
        return Result.Success(snapshot);
    }

    /// <summary>
    /// Ánh xạ <c>hero.skills[]</c> → bộ skill của đơn vị (§23): resolve tất cả skill; <b>ultimate</b> = skill
    /// đầu tiên có năng lượng (<c>trigger.type=energy</c> ⇒ <c>EnergyCost&gt;0</c>); <b>basic</b> = skill đầu
    /// tiên không tốn năng lượng (fallback: skill đầu). Không skill ⇒ <c>null</c> (dùng basic dùng chung của màn).
    /// Quy ước tất định, data-driven (không đoán vai trò từ tên).
    /// </summary>
    private Result<UnitSkillSet?> BuildUnitSkills(HeroCombatConfig hero)
    {
        if (hero.Skills.Count == 0)
        {
            return Result.Success<UnitSkillSet?>(null);
        }

        var resolved = new List<SkillDef>(hero.Skills.Count);
        foreach (string skillId in hero.Skills)
        {
            Result<SkillDef> skill = ResolveSkill(skillId);
            if (skill.IsFailure)
            {
                return Result.Failure<UnitSkillSet?>(skill.Error);
            }

            resolved.Add(skill.Value);
        }

        SkillDef basic = resolved.FirstOrDefault(s => s.EnergyCost == 0) ?? resolved[0];
        SkillDef? ultimate = resolved.FirstOrDefault(s => s.EnergyCost > 0);
        return Result.Success<UnitSkillSet?>(new UnitSkillSet(basic, ultimate));
    }

    private Result<SkillDef> ResolveSkill(string skillId)
    {
        SkillCombatConfig? cfg = _config.Get<SkillCombatConfig>(SkillType, skillId);
        if (cfg is null)
        {
            return Result.Failure<SkillDef>(CombatErrors.SkillNotFound(skillId));
        }

        return Result.Success(BuildSkill(skillId, cfg));
    }

    /// <summary>
    /// Dựng <see cref="SkillDef"/> từ gameplay skill config: giữ nguyên danh sách effect (kèm params/target);
    /// <b>nâng</b> coeff của effect <c>damage</c> đầu tiên (<c>params.coeff_fixed</c>) lên
    /// <see cref="SkillDef.CoeffFixed"/> vì sim tiêu thụ coeff ở cấp skill (§17); <c>trigger.type=energy</c> ⇒
    /// <c>energy_cost</c>; <c>cooldown</c> ⇒ <c>cooldown_rounds</c>.
    /// </summary>
    private static SkillDef BuildSkill(string id, SkillCombatConfig cfg)
    {
        var effects = new List<EffectDef>(cfg.Effects.Count);
        int coeffFixed = 0;
        foreach (SkillEffectConfig effect in cfg.Effects)
        {
            effects.Add(new EffectDef(effect.EffectType, effect.Params, effect.Target));
            if (string.Equals(effect.EffectType, DamageEffectHandler.TypeName, StringComparison.Ordinal) &&
                effect.Params.TryGetValue("coeff_fixed", out long coeff))
            {
                coeffFixed = (int)coeff;
            }
        }

        if (effects.Count == 0)
        {
            effects.Add(new EffectDef(DamageEffectHandler.TypeName));
        }

        int energyCost = string.Equals(cfg.Trigger.Type, "energy", StringComparison.Ordinal) ? cfg.Trigger.Value : 0;
        return new SkillDef(id, coeffFixed, cfg.Target, effects, energyCost, cfg.Cooldown);
    }
}
