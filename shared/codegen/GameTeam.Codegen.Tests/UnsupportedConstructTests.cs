using FluentAssertions;
using Xunit;

namespace GameTeam.Codegen.Tests;

/// <summary>Contract chứa cấu trúc chưa hỗ trợ PHẢI fail rõ ràng (không sinh model sai âm thầm).</summary>
public class UnsupportedConstructTests
{
    private static Action GenerateFrom(string fixture) =>
        () => CodegenRunner.Generate(TestPaths.ReadFixture(fixture), "test");

    [Fact]
    public void OneOf_throws_with_reason() =>
        GenerateFrom("unsupported-oneof.json").Should().Throw<CodegenException>()
            .Where(e => e.Reason.Contains("oneOf"));

    [Fact]
    public void Map_additionalProperties_throws() =>
        GenerateFrom("unsupported-map.json").Should().Throw<CodegenException>()
            .Where(e => e.Reason.Contains("map"));

    [Fact]
    public void Array_without_items_throws() =>
        GenerateFrom("array-no-items.json").Should().Throw<CodegenException>()
            .Where(e => e.Reason.Contains("items"));

    [Fact]
    public void Dangling_ref_throws() =>
        GenerateFrom("dangling-ref.json").Should().Throw<CodegenException>()
            .Where(e => e.Reason.Contains("treo"));

    [Fact]
    public void Enum_missing_x_enum_values_throws() =>
        GenerateFrom("enum-missing-values.json").Should().Throw<CodegenException>()
            .Where(e => e.Reason.Contains("x-enum-values"));

    [Fact]
    public void Codegen_exception_message_has_schema_property_reason_shape()
    {
        CodegenException ex = Assert.Throws<CodegenException>(
            () => CodegenRunner.Generate(TestPaths.ReadFixture("unsupported-oneof.json"), "test"));

        ex.Message.Should().Be("BadDto:either:cấu trúc OpenAPI chưa hỗ trợ 'oneOf'");
    }
}
