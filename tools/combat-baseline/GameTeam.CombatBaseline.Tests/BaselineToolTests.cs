using System.Text.Json;
using FluentAssertions;
using GameTeam.CombatBaseline;
using Xunit;

namespace GameTeam.CombatBaseline.Tests;

/// <summary>
/// Hop dong tool combat-baseline: generate sinh expected tu sim server; check phat hien drift
/// (chan "sua baseline am tham"); canonical idempotent.
/// </summary>
public sealed class BaselineToolTests : IDisposable
{
    // Vector toi thieu 1v1 luon trung / khong crit — CHI co input, expected de rong cho generate dien.
    private const string InputOnlyVector = """
        {
          "format_version": 1,
          "name": "vector_test_tool",
          "description": "input-only fixture cho tool test",
          "input": {
            "config_version": "config@v1",
            "seed": 12345,
            "stage": { "id": "stage_test", "max_rounds": 30 },
            "team_snapshot": {
              "ally": [
                { "actor_id": "u_ally_01", "hero_id": "h", "team": "ally", "slot": 0,
                  "stats": { "hp": 1000, "atk": 200, "def": 100, "spd": 120 } }
              ],
              "enemy": [
                { "actor_id": "u_enemy_01", "hero_id": "h", "team": "enemy", "slot": 0,
                  "stats": { "hp": 500, "atk": 150, "def": 80, "spd": 90 } }
              ]
            },
            "config_excerpt": {
              "skill_basic": { "coeff_fixed": 1000 },
              "combat_rules": {
                "def_constant_k": 300, "min_damage": 1, "crit_multiplier_fixed": 1500,
                "accuracy_bp": 10000, "crit_rate_bp": 0, "max_rounds": 30,
                "energy": { "initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100 }
              }
            }
          }
        }
        """;

    private readonly string _dir;

    public BaselineToolTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "combat-baseline-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Generate_fills_expected_from_server_sim()
    {
        string path = Write("vector_test_tool.json", InputOnlyVector);
        var tool = new BaselineTool(_dir);

        IReadOnlyList<VectorOutcome> results = tool.Generate();

        results.Should().ContainSingle().Which.Status.Should().Be(VectorStatus.Written);

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement expected = doc.RootElement.GetProperty("expected");
        expected.GetProperty("result").GetProperty("outcome").GetString().Should().Be("VICTORY");
        expected.GetProperty("event_log").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public void Generate_then_check_is_clean()
    {
        Write("vector_test_tool.json", InputOnlyVector);
        var tool = new BaselineTool(_dir);

        tool.Generate();
        IReadOnlyList<VectorOutcome> check = tool.Check();

        check.Should().OnlyContain(r => r.Status == VectorStatus.Match);
    }

    [Fact]
    public void Generate_is_idempotent()
    {
        string path = Write("vector_test_tool.json", InputOnlyVector);
        var tool = new BaselineTool(_dir);

        tool.Generate();
        string first = File.ReadAllText(path);
        IReadOnlyList<VectorOutcome> second = tool.Generate();

        second.Should().OnlyContain(r => r.Status == VectorStatus.Unchanged);
        File.ReadAllText(path).Should().Be(first);
    }

    [Fact]
    public void Check_detects_drift_when_expected_mutated()
    {
        string path = Write("vector_test_tool.json", InputOnlyVector);
        var tool = new BaselineTool(_dir);
        tool.Generate();

        // Sua nhe mot so trong expected (target_hp_after) — mo phong baseline bi drift.
        string mutated = File.ReadAllText(path).Replace("\"target_hp_after\": 342", "\"target_hp_after\": 999", StringComparison.Ordinal);
        mutated.Should().NotBe(File.ReadAllText(path), "phai co it nhat mot lan thay the de test co nghia");
        File.WriteAllText(path, mutated);

        IReadOnlyList<VectorOutcome> check = tool.Check();

        check.Should().ContainSingle().Which.Status.Should().Be(VectorStatus.Drift);
    }

    private string Write(string name, string content)
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }
}
