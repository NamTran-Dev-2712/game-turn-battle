using System.Text.Json;
using FluentAssertions;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Serialization;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

/// <summary>
/// Sim server phải khớp golden vector <b>từng sự kiện + kết quả</b> (combat-framework.md §20/§22).
/// TỰ KHÁM PHÁ mọi vector trong <c>shared/combat-vectors/</c> (thêm vector = KHÔNG sửa code test).
/// Đây là hợp đồng: KHÔNG sửa vector để test xanh — nếu lệch, sửa implementation cho đúng spec
/// (hoặc regenerate baseline CÓ CHỦ ĐÍCH qua <c>tools/combat-baseline</c> — xem README).
/// </summary>
public class GoldenVectorTests
{
    /// <summary>Nguồn dữ liệu Theory: mọi file <c>*.json</c> trong thư mục vector (sắp xếp ordinal).</summary>
    public static IEnumerable<object[]> VectorFiles()
    {
        return Directory.EnumerateFiles(RepoPaths.CombatVectorsDir, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new object[] { name! });
    }

    [Theory]
    [MemberData(nameof(VectorFiles))]
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
