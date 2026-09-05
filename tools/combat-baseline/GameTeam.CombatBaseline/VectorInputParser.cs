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

        JsonObject team = RequireObject(input, "team_snapshot");
        IReadOnlyList<UnitSnapshot> ally = ParseUnits(RequireArray(team, "ally"));
        IReadOnlyList<UnitSnapshot> enemy = ParseUnits(RequireArray(team, "enemy"));

        JsonObject excerpt = RequireObject(input, "config_excerpt");
        int coeff = GetInt(RequireObject(excerpt, "skill_basic"), "coeff_fixed");

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

    private static List<UnitSnapshot> ParseUnits(JsonArray array)
    {
        var list = new List<UnitSnapshot>();
        foreach (JsonNode? node in array)
        {
            JsonObject u = (node ?? throw new InvalidDataException("Unit null trong team_snapshot.")).AsObject();
            JsonObject stats = RequireObject(u, "stats");
            list.Add(new UnitSnapshot(
                GetString(u, "actor_id"),
                GetString(u, "hero_id"),
                GetString(u, "team"),
                GetInt(u, "slot"),
                new UnitStats(
                    GetInt(stats, "hp"),
                    GetInt(stats, "atk"),
                    GetInt(stats, "def"),
                    GetInt(stats, "spd"))));
        }

        return list;
    }

    private static JsonNode Require(JsonObject obj, string key) =>
        obj[key] ?? throw new InvalidDataException($"Vector thieu khoa '{key}'.");

    private static JsonObject RequireObject(JsonObject obj, string key) => Require(obj, key).AsObject();

    private static JsonArray RequireArray(JsonObject obj, string key) => Require(obj, key).AsArray();

    private static string GetString(JsonObject obj, string key) => Require(obj, key).GetValue<string>();

    private static int GetInt(JsonObject obj, string key) => Require(obj, key).GetValue<int>();
}
