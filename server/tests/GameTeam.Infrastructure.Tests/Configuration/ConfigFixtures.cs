using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Configuration;

namespace GameTeam.Infrastructure.Tests.Configuration;

/// <summary>Deterministic clock for config publish tests (no wall-clock).</summary>
internal sealed class TestClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}

/// <summary>
/// Helpers that author self-consistent temp config trees for Config Service integration tests. The real
/// <c>config/</c> tree is empty until the gameplay phases (27+), so tests write their own valid/invalid
/// fixtures and validate them against the real <c>shared/config-schema/</c> (resolved from the repo).
/// A valid tree needs a hero AND the skill it references (referential integrity, Phase 07).
/// </summary>
internal static class ConfigFixtures
{
    /// <summary>The real JSON Schema root, resolved from the repo (walks up from the test bin dir).</summary>
    public static string SchemaRoot => ConfigPathResolver.Resolve("shared/config-schema");

    public static string NewTempConfigDir()
        => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cfgtest-" + Guid.NewGuid().ToString("N"))).FullName;

    /// <summary>Author a valid config tree (hero + referenced skill). <paramref name="heroHp"/> lets a test vary a value.</summary>
    public static void WriteValidConfig(string root, int heroHp = 0)
    {
        string heroes = Directory.CreateDirectory(Path.Combine(root, "heroes")).FullName;
        File.WriteAllText(Path.Combine(heroes, "hero_sample.json"),
            $$"""
            {
              "schema_version": 1,
              "id": "hero_sample",
              "faction": "none",
              "class": "warrior",
              "element": "fire",
              "role": "tank",
              "rarity": 3,
              "base_stats": { "hp": {{heroHp}}, "atk": 0, "def": 0, "spd": 0 },
              "skills": ["skill_sample_basic"]
            }
            """);

        string skills = Directory.CreateDirectory(Path.Combine(root, "skills")).FullName;
        File.WriteAllText(Path.Combine(skills, "skill_sample_basic.json"),
            """
            {
              "schema_version": 1,
              "id": "skill_sample_basic",
              "target": "single_enemy",
              "trigger": { "type": "cooldown", "value": 0 },
              "effects": [ { "effect_type": "damage", "params": {} } ]
            }
            """);
    }

    /// <summary>Author an invalid config tree — <c>rarity: 99</c> is outside the schema enum ⇒ SCH001.</summary>
    public static void WriteInvalidConfig(string root)
    {
        string heroes = Directory.CreateDirectory(Path.Combine(root, "heroes")).FullName;
        File.WriteAllText(Path.Combine(heroes, "hero_bad.json"),
            """
            {
              "schema_version": 1,
              "id": "hero_bad",
              "faction": "none",
              "class": "warrior",
              "element": "fire",
              "role": "tank",
              "rarity": 99,
              "base_stats": { "hp": 0, "atk": 0, "def": 0, "spd": 0 },
              "skills": []
            }
            """);
    }
}
