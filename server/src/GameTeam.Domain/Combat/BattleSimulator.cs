using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.Rng;
using GameTeam.Domain.Combat.State;
using GameTeam.Domain.Common;

namespace GameTeam.Domain.Combat;

/// <summary>
/// Bộ mô phỏng combat <b>thuần, tất định</b> — nguồn chân lý kết quả trận (ADR-011, combat-framework.md
/// §12–§19). Không I/O, không wall-clock, không float, không RNG global. Cùng <see cref="BattleInput"/> ⇒
/// cùng <see cref="BattleOutput"/> bit-for-bit. Seed truyền tường minh; một <see cref="Pcg32"/> stream/trận.
/// Effect định tuyến qua <see cref="EffectRegistry"/> (không switch skill trong lõi).
/// </summary>
public sealed class BattleSimulator
{
    private const string TeamAlly = "ally";
    private const string TeamEnemy = "enemy";
    private const int RollBound = 10000; // basis points [0,10000)

    private readonly EffectRegistry _registry;

    /// <summary>Tạo simulator với registry effect tuỳ biến.</summary>
    public BattleSimulator(EffectRegistry registry) => _registry = Guard.NotNull(registry);

    /// <summary>Tạo simulator với registry mặc định (<c>damage</c> + <c>heal</c>).</summary>
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
            log.Add(new RoundStarted(round));

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

                ExecuteAttack(actor, all, input.BasicSkill, rules, rng, log);

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

    private static UnitState? ResolveTarget(List<UnitState> all, UnitState actor) =>
        all.Where(u => !string.Equals(u.Team, actor.Team, StringComparison.Ordinal) && u.IsAlive)
            .OrderBy(u => u.Slot)
            .ThenBy(u => u.ActorId, StringComparer.Ordinal)
            .FirstOrDefault();

    private void ExecuteAttack(
        UnitState actor,
        List<UnitState> all,
        SkillDef skill,
        CombatRules rules,
        Pcg32 rng,
        List<CombatEvent> log)
    {
        log.Add(new ActionStarted(actor.ActorId));

        UnitState? target = ResolveTarget(all, actor);
        if (target is null)
        {
            log.Add(new ActionCompleted(actor.ActorId));
            return;
        }

        log.Add(new TargetSelected(actor.ActorId, target.ActorId));

        int hitRoll = (int)rng.Bounded(RollBound);
        log.Add(new RandomRoll("hit", RollBound, hitRoll));
        if (hitRoll >= rules.AccuracyBp)
        {
            log.Add(new Miss(actor.ActorId, target.ActorId));
            log.Add(new ActionCompleted(actor.ActorId));
            return;
        }

        log.Add(new Hit(actor.ActorId, target.ActorId));

        int critRoll = (int)rng.Bounded(RollBound); // luôn tiêu thụ sau Hit, kể cả crit_rate_bp==0
        log.Add(new RandomRoll("crit", RollBound, critRoll));
        bool crit = critRoll < rules.CritRateBp;
        if (crit)
        {
            log.Add(new Crit(actor.ActorId, target.ActorId));
        }

        foreach (EffectDef effect in skill.Effects)
        {
            IEffectHandler handler = _registry.Resolve(effect.EffectType);
            var context = new EffectContext(actor, target, skill, effect, rules, crit, log);
            handler.Apply(context);
        }

        log.Add(new ActionCompleted(actor.ActorId));
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
