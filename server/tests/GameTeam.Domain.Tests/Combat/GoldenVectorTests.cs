using System.Text.Json;
using FluentAssertions;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Serialization;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

/// <summary>
/// Sim server phải khớp golden vector phase 23 <b>từng sự kiện + kết quả</b> (combat-framework.md §20).
/// Đây là hợp đồng: KHÔNG sửa vector để test xanh — nếu lệch, sửa implementation cho đúng spec.
/// </summary>
public class GoldenVectorTests
{
    [Theory]
    [InlineData("vector_01_basic_hit.json")]
    [InlineData("vector_02_crit_ko.json")]
    public void Simulate_matches_golden_vector(string fileName)
    {
        LoadedVector vector = GoldenVectorLoader.Load(fileName);

        BattleOutput output = new BattleSimulator().Simulate(vector.Input);

        string actualJson = CombatEventSerializer.Serialize(output);
        using JsonDocument actualDoc = JsonDocument.Parse(actualJson);

        string? diff = JsonStructuralComparer.FirstDifference(vector.Expected, actualDoc.RootElement);
        diff.Should().BeNull($"sim phải khớp golden {fileName}; khác biệt đầu tiên: {diff}");
    }
}
