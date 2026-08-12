using FluentAssertions;
using Xunit;

namespace GameTeam.Codegen.Tests;

public class EmitterTests
{
    private static IReadOnlyList<GeneratedFile> Gen(string fixture) =>
        CodegenRunner.Generate(TestPaths.ReadFixture(fixture), "test");

    private static string Content(IReadOnlyList<GeneratedFile> files, string name) =>
        files.Single(f => f.FileName == name).Content;

    [Fact]
    public void Enum_preserves_gapped_numeric_values()
    {
        string gd = Content(Gen("enum-gapped.json"), "rarity.gd");

        gd.Should().StartWith("# AUTO-GENERATED — DO NOT EDIT.");
        gd.Should().Contain("# Source: test (schema: Rarity)");
        gd.Should().Contain("class_name Rarity");
        gd.Should().Contain("extends RefCounted");
        gd.Should().Contain("enum {");
        gd.Should().Contain("\tNONE = 0,");
        gd.Should().Contain("\tTHREE = 3,"); // "khoảng trống" 1,2 giữ đúng số C#.
        gd.Should().Contain("\tFOUR = 4,");
        gd.Should().Contain("\tFIVE = 5,");
        gd.Should().EndWith("}\n");
    }

    [Fact]
    public void Dto_maps_primitives_ref_enum_array_and_nullable()
    {
        string gd = Content(Gen("dtos.json"), "sample_dto.gd");

        gd.Should().Contain("class_name SampleDto");
        gd.Should().Contain("extends Resource");
        gd.Should().Contain("var player_name: String");
        gd.Should().Contain("var level: int");
        gd.Should().Contain("var ratio: float");
        gd.Should().Contain("var is_active: bool");
        gd.Should().Contain("var tags: Array[String]");

        // Nested DTO $ref → kiểu class.
        gd.Should().Contain("var version: ConfigVersion");

        // Enum $ref → int + ghi chú wire.
        gd.Should().Contain("## wire: rarity | enum: Rarity (wire: string)");
        gd.Should().Contain("var rarity: int");

        // Nullable primitive → untyped (Variant) + ghi chú.
        gd.Should().Contain("## wire: deviceId | nullable");
        gd.Should().Contain("var device_id\n");

        // Nullable + $ref DTO → giữ chú kiểu (Resource nhận null sẵn).
        gd.Should().Contain("## wire: nestedVersion | nullable");
        gd.Should().Contain("var nested_version: ConfigVersion");
    }

    [Fact]
    public void Every_property_carries_wire_key_doc()
    {
        string gd = Content(Gen("dtos.json"), "sample_dto.gd");
        gd.Should().Contain("## wire: playerName");
        gd.Should().Contain("## wire: level");
    }
}
