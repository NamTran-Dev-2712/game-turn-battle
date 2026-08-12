using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace GameTeam.ConfigValidator.Tests;

/// <summary>
/// Tái dùng fixture Phase 06 (shared/config-schema/fixtures/*.valid|invalid.json) làm test vector cho
/// tầng schema + version: mọi <c>*.valid.json</c> phải sạch; mọi <c>*.invalid.json</c> phải sinh lỗi.
/// </summary>
public sealed class SharedFixtureTests
{
    private static readonly SchemaSet Schemas = SchemaSet.Build(TestPaths.SchemaDir);

    public static IEnumerable<object[]> ValidFixtures() => Fixtures("*.valid.json");

    public static IEnumerable<object[]> InvalidFixtures() => Fixtures("*.invalid.json");

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public void Valid_shared_fixture_has_no_schema_or_version_error(string file)
    {
        IReadOnlyList<ValidationError> errors = Evaluate(file);
        errors.Should().BeEmpty(because: string.Join("\n", errors));
    }

    [Theory]
    [MemberData(nameof(InvalidFixtures))]
    public void Invalid_shared_fixture_produces_at_least_one_error(string file)
    {
        Evaluate(file).Should().NotBeEmpty();
    }

    private static IReadOnlyList<ValidationError> Evaluate(string file)
    {
        ConfigType type = Enum.Parse<ConfigType>(Path.GetFileName(file).Split('.')[0], ignoreCase: true);
        ConfigEntity entity = new(file, type, JsonNode.Parse(File.ReadAllText(file)));

        return [.. SchemaValidator.Validate(entity, Schemas), .. VersionValidator.Validate(entity)];
    }

    private static IEnumerable<object[]> Fixtures(string pattern) =>
        Directory.EnumerateFiles(TestPaths.SharedFixtures, pattern)
            .OrderBy(static p => p, StringComparer.Ordinal)
            .Select(static p => new object[] { p });
}
