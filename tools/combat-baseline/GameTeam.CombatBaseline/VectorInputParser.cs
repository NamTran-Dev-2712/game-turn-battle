using System.Text.Json.Nodes;
using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Model;

namespace GameTeam.CombatBaseline;

/// <summary>
/// Doc phan <c>input</c> cua mot golden vector thanh <see cref="BattleInput"/> tu chua.
/// Song anh <c>GoldenVectorLoader</c> ben test server — chu y: neu hai parser lech nhau thi
/// <c>GoldenVectorTests</c> se DO (server output != expected ma tool sinh), nen chung tu-kiem cheo nhau.
/// KHONG doan mac dinh: thieu khoa =&gt; nem.
/// </summary>
public static class VectorInputParser
{
    /// <summary>Dung <see cref="BattleInput"/> tu node <c>input</c> cua vector.</summary>
    public static BattleInput Parse(JsonNode inputNode)
    {
        ArgumentNullException.ThrowIfNull(inputNode);
        JsonObject input = inputNode.AsObject();

        string configVersion = GetString(input, "config_version");
        ulong seed = Require(input, "seed").GetValue<ulong>();

        JsonObject stageEl = RequireObject(input, "stage");
        var stage = new StageInfo(GetString(stageEl, "id"), GetInt(stageEl, "max_rounds"));

        JsonObject excerpt = RequireObject(input, "config_excerpt");
        int coeff = GetInt(RequireObject(excerpt, "skill_basic"), "coeff_fixed");

        // §23: bang skill tuy chon (config_excerpt.skills) cho skill rieng cua unit/ultimate — vang => rong.
        var skillsById = new Dictionary<string, SkillDef>(StringComparer.Ordinal);
        if (excerpt["skills"] is JsonObject skillsObj)
        {
            foreach (KeyValuePair<string, JsonNode?> p in skillsObj)
            {
                skillsById[p.Key] = ParseSkill(p.Key, p.Value!.AsObject());
            }
        }

        JsonObject team = RequireObject(input, "team_snapshot");
        IReadOnlyList<UnitSnapshot> ally = ParseUnits(RequireArray(team, "ally"), skillsById);
        IReadOnlyList<UnitSnapshot> enemy = ParseUnits(RequireArray(team, "enemy"), skillsById);

        JsonObject cr = RequireObject(excerpt, "combat_rules");
        JsonObject en = RequireObject(cr, "energy");
        var energy = new EnergyRules(
            GetInt(en, "initial"),
            GetInt(en, "on_attack"),
            GetInt(en, "on_hit"),
            GetInt(en, "ultimate_cost"),
            GetInt(en, "max"));

        var rules = new CombatRules(
            GetInt(cr, "def_constant_k"),
            GetInt(cr, "min_damage"),
            GetInt(cr, "crit_multiplier_fixed"),
            GetInt(cr, "accuracy_bp"),
            GetInt(cr, "crit_rate_bp"),
            GetInt(cr, "max_rounds"),
            energy);

        var basicSkill = new SkillDef(
            "skill_basic",
            coeff,
            "default",
            new[] { new EffectDef(DamageEffectHandler.TypeName) });

        return new BattleInput(configVersion, seed, stage, ally, enemy, rules, basicSkill);
    }

    private static List<UnitSnapshot> ParseUnits(JsonArray array, IReadOnlyDictionary<string, SkillDef> skillsById)
    {
        var list = new List<UnitSnapshot>();
        foreach (JsonNode? node in array)
        {
            JsonObject u = (node ?? throw new InvalidDataException("Unit null trong team_snapshot.")).AsObject();
            JsonObject stats = RequireObject(u, "stats");

            UnitSkillSet? skills = null;
            if (u["skills"] is JsonObject s)
            {
                SkillDef basic = skillsById[GetString(s, "basic")];
                SkillDef? ultimate = s["ultimate"] is JsonNode ultNode
                    ? skillsById[ultNode.GetValue<string>()]
                    : null;
                skills = new UnitSkillSet(basic, ultimate);
            }

            list.Add(new UnitSnapshot(
                GetString(u, "actor_id"),
                GetString(u, "hero_id"),
                GetString(u, "team"),
                GetInt(u, "slot"),
                new UnitStats(
                    GetInt(stats, "hp"),
                    GetInt(stats, "atk"),
                    GetInt(stats, "def"),
                    GetInt(stats, "spd")),
                skills));
        }

        return list;
    }

    private static SkillDef ParseSkill(string id, JsonObject el)
    {
        int coeff = el["coeff_fixed"] is JsonNode c ? c.GetValue<int>() : 0;
        string targetRule = el["target_rule"] is JsonNode tr ? tr.GetValue<string>() : "default";
        int energyCost = el["energy_cost"] is JsonNode ec ? ec.GetValue<int>() : 0;
        int cooldown = el["cooldown_rounds"] is JsonNode cd ? cd.GetValue<int>() : 0;

        var effects = new List<EffectDef>();
        if (el["effects"] is JsonArray effArr)
        {
            foreach (JsonNode? e in effArr)
            {
                effects.Add(ParseEffect(e!.AsObject()));
            }
        }

        if (effects.Count == 0)
        {
            effects.Add(new EffectDef(DamageEffectHandler.TypeName));
        }

        return new SkillDef(id, coeff, targetRule, effects, energyCost, cooldown);
    }

    private static EffectDef ParseEffect(JsonObject e)
    {
        string type = GetString(e, "effect_type");
        string? target = e["target"] is JsonNode t ? t.GetValue<string>() : null;

        var pars = new Dictionary<string, long>(StringComparer.Ordinal);
        if (e["params"] is JsonObject pObj)
        {
            foreach (KeyValuePair<string, JsonNode?> pp in pObj)
            {
                pars[pp.Key] = pp.Value!.GetValue<long>();
            }
        }

        return new EffectDef(type, pars, target);
    }

    private static JsonNode Require(JsonObject obj, string key) =>
        obj[key] ?? throw new InvalidDataException($"Vector thieu khoa '{key}'.");

    private static JsonObject RequireObject(JsonObject obj, string key) => Require(obj, key).AsObject();

    private static JsonArray RequireArray(JsonObject obj, string key) => Require(obj, key).AsArray();

    private static string GetString(JsonObject obj, string key) => Require(obj, key).GetValue<string>();

    private static int GetInt(JsonObject obj, string key) => Require(obj, key).GetValue<int>();
}
