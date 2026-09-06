using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.Rng;
using GameTeam.Domain.Combat.State;
using GameTeam.Domain.Common;

namespace GameTeam.Domain.Combat;

/// <summary>
/// Bộ mô phỏng combat <b>thuần, tất định</b> — nguồn chân lý kết quả trận (ADR-011, combat-framework.md
/// §12–§19, §23). Không I/O, không wall-clock, không float, không RNG global. Cùng <see cref="BattleInput"/> ⇒
/// cùng <see cref="BattleOutput"/> bit-for-bit. Seed truyền tường minh; một <see cref="Pcg32"/> stream/trận.
/// Effect định tuyến qua <see cref="EffectRegistry"/> (không switch skill trong lõi). Skill/effect data-driven:
/// mỗi đơn vị chọn basic/ultimate theo năng lượng+hồi chiêu (§15); buff/debuff áp qua modifier chỉ số (§23).
/// </summary>
public sealed class BattleSimulator
{
    private const string TeamAlly = "ally";
    private const string TeamEnemy = "enemy";
    private const int RollBound = 10000; // basis points [0,10000)
    private const string TargetSingleAlly = "single_ally";
    private const string TargetSelf = "self";

    private readonly EffectRegistry _registry;

    /// <summary>Tạo simulator với registry effect tuỳ biến.</summary>
    public BattleSimulator(EffectRegistry registry) => _registry = Guard.NotNull(registry);

    /// <summary>Tạo simulator với registry mặc định (§23: damage/heal/apply_buff/apply_debuff).</summary>
    public BattleSimulator()
        : this(EffectRegistry.CreateDefault())
    {
    }

    /// <summary>Chạy mô phỏng một trận và trả event log + kết quả (tất định).</summary>
    public BattleOutput Simulate(BattleInput input)
    {
        Guard.NotNull(input);

        var log = new List<CombatEvent>();
        var rng = new Pcg32(input.Seed);
        CombatRules rules = input.Rules;

        List<UnitState> allies = BuildUnits(input.Ally, rules);
        List<UnitState> enemies = BuildUnits(input.Enemy, rules);
        var all = new List<UnitState>(allies.Count + enemies.Count);
        all.AddRange(allies);
        all.AddRange(enemies);

        int maxRounds = input.Stage.MaxRounds;
        bool ended = false;
        int roundsPlayed = 0;

        for (int round = 1; round <= maxRounds; round++)
        {
            roundsPlayed = round;
            StartRound(allies, enemies, round, log);

            foreach (UnitState actor in BuildActionOrder(all))
            {
                if (!actor.IsAlive)
                {
                    continue; // chết trong vòng này ⇒ bỏ lượt
                }

                if (!HasLivingEnemy(all, actor))
                {
                    break;
                }

                ExecuteAction(actor, all, input.BasicSkill, rules, rng, log);

                if (IsEnded(allies, enemies))
                {
                    ended = true;
                    break;
                }
            }

            log.Add(new RoundEnded(round));
            if (ended)
            {
                break;
            }
        }

        log.Add(new BattleEnded());
        BattleResult result = BuildResult(allies, enemies, roundsPlayed);
        return new BattleOutput(log, result);
    }

    private static List<UnitState> BuildUnits(IReadOnlyList<UnitSnapshot> units, CombatRules rules) =>
        units.Select(u => new UnitState(u, rules.Energy.Initial)).ToList();

    private static List<UnitState> BuildActionOrder(List<UnitState> all) =>
        all.Where(u => u.IsAlive)
            .OrderByDescending(u => u.Spd)
            .ThenBy(u => u.ActorId, StringComparer.Ordinal)
            .ToList();

    private static bool HasLivingEnemy(List<UnitState> all, UnitState actor) =>
        all.Any(u => !string.Equals(u.Team, actor.Team, StringComparison.Ordinal) && u.IsAlive);

    /// <summary>
    /// Đầu vòng (§23): phát <see cref="RoundStarted"/>, rồi giảm 1 vòng mọi buff/debuff (phát
    /// <see cref="BuffExpired"/> theo thứ tự tất định) và giảm hồi chiêu ultimate. Chỉ tick đơn vị còn sống;
    /// với vector không dùng buff/năng lượng (mặc định tắt) ⇒ không phát thêm sự kiện (byte-identical phase 24/26).
    /// </summary>
    private static void StartRound(List<UnitState> allies, List<UnitState> enemies, int round, List<CombatEvent> log)
    {
        log.Add(new RoundStarted(round));

        foreach (UnitState unit in TickOrder(allies, enemies))
        {
            if (!unit.IsAlive)
            {
                continue;
            }

            foreach (StatModifier expired in unit.TickModifiers())
            {
                log.Add(new BuffExpired(unit.ActorId, expired.SourceSkillId, StatKinds.Name(expired.Stat)));
            }

            unit.TickUltimateCooldown();
        }
    }

    private static IEnumerable<UnitState> TickOrder(List<UnitState> allies, List<UnitState> enemies) =>
        allies.OrderBy(u => u.Slot).ThenBy(u => u.ActorId, StringComparer.Ordinal)
            .Concat(enemies.OrderBy(u => u.Slot).ThenBy(u => u.ActorId, StringComparer.Ordinal));

    /// <summary>Chọn skill lượt này (§15): ultimate nếu đủ năng lượng và hết hồi chiêu, ngược lại basic.</summary>
    private static (SkillDef Skill, bool IsUltimate) SelectSkill(UnitState actor, SkillDef fallbackBasic)
    {
        SkillDef basic = actor.Skills?.Basic ?? fallbackBasic;
        SkillDef? ultimate = actor.Skills?.Ultimate;
        if (ultimate is not null && actor.Energy >= ultimate.EnergyCost && actor.UltimateCooldownRemaining == 0)
        {
            return (ultimate, true);
        }

        return (basic, false);
    }

    private static bool IsAttackSkill(SkillDef skill) =>
        skill.Effects.Any(e => string.Equals(e.EffectType, DamageEffectHandler.TypeName, StringComparison.Ordinal));

    /// <summary>
    /// Giải mục tiêu theo target rule (§23, tập tối thiểu tất định): <c>single_ally</c> = đồng minh sống slot
    /// nhỏ nhất (gồm cả bản thân); <c>self</c> = chính actor; còn lại (kể cả <c>default</c>) = kẻ địch sống
    /// slot nhỏ nhất — tie-break kết bằng <c>actor_id</c>. Aggro/target nâng cao (CB3) là phase sau.
    /// </summary>
    private static UnitState? ResolveByRule(List<UnitState> all, UnitState actor, string rule)
    {
        if (string.Equals(rule, TargetSelf, StringComparison.Ordinal))
        {
            return actor.IsAlive ? actor : null;
        }

        bool wantAlly = string.Equals(rule, TargetSingleAlly, StringComparison.Ordinal);
        return all
            .Where(u => u.IsAlive
                && (string.Equals(u.Team, actor.Team, StringComparison.Ordinal) == wantAlly))
            .OrderBy(u => u.Slot)
            .ThenBy(u => u.ActorId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private void ExecuteAction(
        UnitState actor,
        List<UnitState> all,
        SkillDef fallbackBasic,
        CombatRules rules,
        Pcg32 rng,
        List<CombatEvent> log)
    {
        (SkillDef skill, bool isUltimate) = SelectSkill(actor, fallbackBasic);
        log.Add(new ActionStarted(actor.ActorId));

        UnitState? primary = ResolveByRule(all, actor, skill.TargetRule);
        if (primary is null)
        {
            log.Add(new ActionCompleted(actor.ActorId));
            return;
        }

        log.Add(new TargetSelected(actor.ActorId, primary.ActorId));

        bool crit = false;
        if (IsAttackSkill(skill))
        {
            int hitRoll = (int)rng.Bounded(RollBound);
            log.Add(new RandomRoll("hit", RollBound, hitRoll));
            if (hitRoll >= rules.AccuracyBp)
            {
                log.Add(new Miss(actor.ActorId, primary.ActorId));
                log.Add(new ActionCompleted(actor.ActorId));
                return;
            }

            log.Add(new Hit(actor.ActorId, primary.ActorId));

            int critRoll = (int)rng.Bounded(RollBound); // luôn tiêu thụ sau Hit, kể cả crit_rate_bp==0
            log.Add(new RandomRoll("crit", RollBound, critRoll));
            crit = critRoll < rules.CritRateBp;
            if (crit)
            {
                log.Add(new Crit(actor.ActorId, primary.ActorId));
            }
        }

        foreach (EffectDef effect in skill.Effects)
        {
            string rule = effect.Target ?? skill.TargetRule;
            UnitState? target = ResolveByRule(all, actor, rule);
            if (target is null)
            {
                continue; // không còn mục tiêu hợp lệ cho effect này ⇒ bỏ qua (vd không còn đồng minh để buff)
            }

            var context = new EffectContext(actor, target, skill, effect, rules, crit, log);
            _registry.Resolve(effect.EffectType).Apply(context);
        }

        UpdateAttackerEnergy(actor, skill, isUltimate, rules, log);
        log.Add(new ActionCompleted(actor.ActorId));
    }

    /// <summary>§15: ultimate tiêu năng lượng + đặt hồi chiêu; đòn thường nạp on_attack. Phát <see cref="EnergyChanged"/> khi giá trị đổi.</summary>
    private static void UpdateAttackerEnergy(UnitState actor, SkillDef skill, bool isUltimate, CombatRules rules, List<CombatEvent> log)
    {
        if (isUltimate)
        {
            if (actor.SpendEnergy(skill.EnergyCost))
            {
                log.Add(new EnergyChanged(actor.ActorId, actor.Energy));
            }

            actor.SetUltimateCooldown(skill.CooldownRounds);
        }
        else if (actor.AddEnergy(rules.Energy.OnAttack, rules.Energy.Max))
        {
            log.Add(new EnergyChanged(actor.ActorId, actor.Energy));
        }
    }

    private static bool IsEnded(List<UnitState> allies, List<UnitState> enemies) =>
        !allies.Any(u => u.IsAlive) || !enemies.Any(u => u.IsAlive);

    private static BattleResult BuildResult(List<UnitState> allies, List<UnitState> enemies, int roundsPlayed)
    {
        bool allyAlive = allies.Any(u => u.IsAlive);
        bool enemyAlive = enemies.Any(u => u.IsAlive);

        string outcome;
        string? winner;
        if (!enemyAlive && allyAlive)
        {
            outcome = "VICTORY";
            winner = TeamAlly;
        }
        else if (!allyAlive && enemyAlive)
        {
            outcome = "DEFEAT";
            winner = TeamEnemy;
        }
        else
        {
            // cả hai cùng bị xoá (đồng thời) hoặc cả hai còn sống (đạt max_rounds) ⇒ DRAW (§19).
            outcome = "DRAW";
            winner = null;
        }

        var finalHp = new List<KeyValuePair<string, int>>(allies.Count + enemies.Count);
        foreach (UnitState unit in allies)
        {
            finalHp.Add(new KeyValuePair<string, int>(unit.ActorId, unit.Hp));
        }

        foreach (UnitState unit in enemies)
        {
            finalHp.Add(new KeyValuePair<string, int>(unit.ActorId, unit.Hp));
        }

        return new BattleResult(outcome, winner, roundsPlayed, finalHp);
    }
}
