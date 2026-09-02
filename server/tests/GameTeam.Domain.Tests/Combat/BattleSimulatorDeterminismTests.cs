using FluentAssertions;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Serialization;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

/// <summary>
/// Hợp đồng cốt lõi phase 24 (ADR-011): cùng input × N lần ⇒ output <b>byte-đồng nhất</b>. N đủ lớn để
/// bắt rò rỉ state/RNG global/static khả biến.
/// </summary>
public class BattleSimulatorDeterminismTests
{
    private const int Runs = 200;

    [Theory]
    [InlineData("vector_01_basic_hit.json")]
    [InlineData("vector_02_crit_ko.json")]
    public void Same_input_produces_byte_identical_output_across_n_runs(string fileName)
    {
        LoadedVector vector = GoldenVectorLoader.Load(fileName);

        string baseline = CombatEventSerializer.Serialize(new BattleSimulator().Simulate(vector.Input));

        for (int i = 0; i < Runs; i++)
        {
            // Simulator + RNG mới mỗi lần — không chia sẻ state.
            string actual = CombatEventSerializer.Serialize(new BattleSimulator().Simulate(vector.Input));
            actual.Should().Be(baseline, $"lần chạy {i} phải trùng byte với lần đầu");
        }
    }

    [Fact]
    public void Different_seed_changes_output()
    {
        LoadedVector vector = GoldenVectorLoader.Load("vector_01_basic_hit.json");
        var otherSeed = vector.Input with { Seed = vector.Input.Seed + 1 };

        string a = CombatEventSerializer.Serialize(new BattleSimulator().Simulate(vector.Input));
        string b = CombatEventSerializer.Serialize(new BattleSimulator().Simulate(otherSeed));

        a.Should().NotBe(b, "seed khác nên stream RNG khác ⇒ log khác");
    }
}
