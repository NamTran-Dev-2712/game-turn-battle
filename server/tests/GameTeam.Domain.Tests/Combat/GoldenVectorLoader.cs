using System.Text.Json;
using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Model;

namespace GameTeam.Domain.Tests.Combat;

/// <summary>
/// Nạp golden vector phase 23 (<c>shared/combat-vectors/*.json</c>) thành <see cref="BattleInput"/>
/// tự chứa (từ <c>config_excerpt</c> + <c>team_snapshot</c> + stage + seed) + phần <c>expected</c>.
/// KHÔNG sửa vector — chỉ đọc (vector là hợp đồng).
/// </summary>
internal static class GoldenVectorLoader
{
    public static LoadedVector Load(string fileName)
    {
        string path = Path.Combine(RepoPaths.CombatVectorsDir, fileName);
        string json = File.ReadAllText(path);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement.Clone();

        JsonElement input = root.GetProperty("input");
        string configVersion = input.GetProperty("config_version").GetString()!;
        ulong seed = input.GetProperty("seed").GetUInt64();

        JsonElement stageEl = input.GetProperty("stage");
        var stage = new StageInfo(
            stageEl.GetProperty("id").GetString()!,
            stageEl.GetProperty("max_rounds").GetInt32());

        JsonElement team = input.GetProperty("team_snapshot");
        IReadOnlyList<UnitSnapshot> ally = ParseUnits(team.GetProperty("ally"));
        IReadOnlyList<UnitSnapshot> enemy = ParseUnits(team.GetProperty("enemy"));

        JsonElement excerpt = input.GetProperty("config_excerpt");
        int coeff = excerpt.GetProperty("skill_basic").GetProperty("coeff_fixed").GetInt32();

        JsonElement cr = excerpt.GetProperty("combat_rules");
        JsonElement en = cr.GetProperty("energy");
        var energy = new EnergyRules(
            en.GetProperty("initial").GetInt32(),
            en.GetProperty("on_attack").GetInt32(),
            en.GetProperty("on_hit").GetInt32(),
            en.GetProperty("ultimate_cost").GetInt32(),
            en.GetProperty("max").GetInt32());

        var rules = new CombatRules(
            cr.GetProperty("def_constant_k").GetInt32(),
            cr.GetProperty("min_damage").GetInt32(),
            cr.GetProperty("crit_multiplier_fixed").GetInt32(),
            cr.GetProperty("accuracy_bp").GetInt32(),
            cr.GetProperty("crit_rate_bp").GetInt32(),
            cr.GetProperty("max_rounds").GetInt32(),
            energy);

        var basicSkill = new SkillDef(
            "skill_basic",
            coeff,
            "default",
            new[] { new EffectDef(DamageEffectHandler.TypeName) });

        var battleInput = new BattleInput(configVersion, seed, stage, ally, enemy, rules, basicSkill);
        return new LoadedVector(battleInput, root.GetProperty("expected"));
    }

    private static List<UnitSnapshot> ParseUnits(JsonElement array)
    {
        var list = new List<UnitSnapshot>();
        foreach (JsonElement u in array.EnumerateArray())
        {
            JsonElement stats = u.GetProperty("stats");
            list.Add(new UnitSnapshot(
                u.GetProperty("actor_id").GetString()!,
                u.GetProperty("hero_id").GetString()!,
                u.GetProperty("team").GetString()!,
                u.GetProperty("slot").GetInt32(),
                new UnitStats(
                    stats.GetProperty("hp").GetInt32(),
                    stats.GetProperty("atk").GetInt32(),
                    stats.GetProperty("def").GetInt32(),
                    stats.GetProperty("spd").GetInt32())));
        }

        return list;
    }
}

/// <summary>Vector đã nạp: input tự chứa + phần expected (event_log + result).</summary>
internal sealed record LoadedVector(BattleInput Input, JsonElement Expected);
