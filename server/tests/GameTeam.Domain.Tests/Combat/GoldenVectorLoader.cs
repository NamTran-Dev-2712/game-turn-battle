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

        JsonElement excerpt = input.GetProperty("config_excerpt");
        int coeff = excerpt.GetProperty("skill_basic").GetProperty("coeff_fixed").GetInt32();

        // §23: bảng skill tuỳ chọn (config_excerpt.skills) cho skill riêng của unit/ultimate — vắng ⇒ rỗng.
        var skillsById = new Dictionary<string, SkillDef>(StringComparer.Ordinal);
        if (excerpt.TryGetProperty("skills", out JsonElement skillsEl))
        {
            foreach (JsonProperty p in skillsEl.EnumerateObject())
            {
                skillsById[p.Name] = ParseSkill(p.Name, p.Value);
            }
        }

        JsonElement team = input.GetProperty("team_snapshot");
        IReadOnlyList<UnitSnapshot> ally = ParseUnits(team.GetProperty("ally"), skillsById);
        IReadOnlyList<UnitSnapshot> enemy = ParseUnits(team.GetProperty("enemy"), skillsById);

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

    private static List<UnitSnapshot> ParseUnits(JsonElement array, IReadOnlyDictionary<string, SkillDef> skillsById)
    {
        var list = new List<UnitSnapshot>();
        foreach (JsonElement u in array.EnumerateArray())
        {
            JsonElement stats = u.GetProperty("stats");

            UnitSkillSet? skills = null;
            if (u.TryGetProperty("skills", out JsonElement s))
            {
                SkillDef basic = skillsById[s.GetProperty("basic").GetString()!];
                SkillDef? ultimate = s.TryGetProperty("ultimate", out JsonElement ultEl)
                    ? skillsById[ultEl.GetString()!]
                    : null;
                skills = new UnitSkillSet(basic, ultimate);
            }

            list.Add(new UnitSnapshot(
                u.GetProperty("actor_id").GetString()!,
                u.GetProperty("hero_id").GetString()!,
                u.GetProperty("team").GetString()!,
                u.GetProperty("slot").GetInt32(),
                new UnitStats(
                    stats.GetProperty("hp").GetInt32(),
                    stats.GetProperty("atk").GetInt32(),
                    stats.GetProperty("def").GetInt32(),
                    stats.GetProperty("spd").GetInt32()),
                skills));
        }

        return list;
    }

    private static SkillDef ParseSkill(string id, JsonElement el)
    {
        int coeff = el.TryGetProperty("coeff_fixed", out JsonElement c) ? c.GetInt32() : 0;
        string targetRule = el.TryGetProperty("target_rule", out JsonElement tr) ? tr.GetString()! : "default";
        int energyCost = el.TryGetProperty("energy_cost", out JsonElement ec) ? ec.GetInt32() : 0;
        int cooldown = el.TryGetProperty("cooldown_rounds", out JsonElement cd) ? cd.GetInt32() : 0;

        var effects = new List<EffectDef>();
        if (el.TryGetProperty("effects", out JsonElement effEl))
        {
            foreach (JsonElement e in effEl.EnumerateArray())
            {
                effects.Add(ParseEffect(e));
            }
        }

        if (effects.Count == 0)
        {
            effects.Add(new EffectDef(DamageEffectHandler.TypeName));
        }

        return new SkillDef(id, coeff, targetRule, effects, energyCost, cooldown);
    }

    private static EffectDef ParseEffect(JsonElement e)
    {
        string type = e.GetProperty("effect_type").GetString()!;
        string? target = e.TryGetProperty("target", out JsonElement t) ? t.GetString() : null;

        var pars = new Dictionary<string, long>(StringComparer.Ordinal);
        if (e.TryGetProperty("params", out JsonElement pEl))
        {
            foreach (JsonProperty pp in pEl.EnumerateObject())
            {
                pars[pp.Name] = pp.Value.GetInt64();
            }
        }

        return new EffectDef(type, pars, target);
    }
}

/// <summary>Vector đã nạp: input tự chứa + phần expected (event_log + result).</summary>
internal sealed record LoadedVector(BattleInput Input, JsonElement Expected);
