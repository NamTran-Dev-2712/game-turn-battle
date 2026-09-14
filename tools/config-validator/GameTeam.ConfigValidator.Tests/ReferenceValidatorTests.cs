using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace GameTeam.ConfigValidator.Tests;

/// <summary>
/// Referential integrity trực tiếp cho các loại thêm sau (Phase 34: <c>chapter.stages[] → stage</c>).
/// Kiểm bằng <see cref="ReferenceValidator.Validate"/> + <see cref="IdIndex"/> in-memory (không cần cây fixture).
/// </summary>
public sealed class ReferenceValidatorTests
{
    private static ConfigEntity Chapter(string id, params string[] stages)
    {
        var arr = new JsonArray();
        foreach (string s in stages)
        {
            arr.Add(s);
        }

        var obj = new JsonObject { ["id"] = id, ["stages"] = arr };
        return new ConfigEntity($"chapters/{id}.json", ConfigType.Chapter, obj);
    }

    [Fact]
    public void Chapter_referencing_existing_stages_has_no_error()
    {
        IdIndex index = IdIndex.Build([
            new ConfigEntity("stages/s1.json", ConfigType.Stage, JsonNode.Parse("""{ "id": "stage_ch01_01" }""")),
            new ConfigEntity("stages/s2.json", ConfigType.Stage, JsonNode.Parse("""{ "id": "stage_ch01_02" }""")),
        ]);

        ReferenceValidator.Validate(Chapter("chapter_01", "stage_ch01_01", "stage_ch01_02"), index)
            .Should().BeEmpty();
    }

    [Fact]
    public void Chapter_referencing_missing_stage_fails_with_ref001_at_correct_path()
    {
        IdIndex index = IdIndex.Build([
            new ConfigEntity("stages/s1.json", ConfigType.Stage, JsonNode.Parse("""{ "id": "stage_ch01_01" }""")),
        ]);

        ReferenceValidator.Validate(Chapter("chapter_01", "stage_ch01_01", "stage_ghost"), index)
            .Should().ContainSingle()
            .Which.Should().Match<ValidationError>(e =>
                e.Code == ErrorCode.Ref001Missing && e.Path == "/stages/1");
    }
}
