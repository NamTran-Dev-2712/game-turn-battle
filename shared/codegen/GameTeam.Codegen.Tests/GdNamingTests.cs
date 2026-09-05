using FluentAssertions;
using Xunit;

namespace GameTeam.Codegen.Tests;

public class GdNamingTests
{
    [Theory]
    [InlineData("deviceId", "device_id")]
    [InlineData("expiresInSeconds", "expires_in_seconds")]
    [InlineData("schemaVersion", "schema_version")]
    [InlineData("playerId", "player_id")]
    [InlineData("ProfileDto", "profile_dto")]
    [InlineData("AuthGuestRequest", "auth_guest_request")]
    [InlineData("Class", "class")]
    [InlineData("status", "status")]
    public void ToSnakeCase_maps_camel_and_pascal(string input, string expected) =>
        GdNaming.ToSnakeCase(input).Should().Be(expected);

    [Theory]
    [InlineData("None", "NONE")]
    [InlineData("Warrior", "WARRIOR")]
    [InlineData("Dps", "DPS")]
    [InlineData("Three", "THREE")]
    public void ToConstantCase_maps_enum_members(string input, string expected) =>
        GdNaming.ToConstantCase(input).Should().Be(expected);

    [Theory]
    [InlineData("Rarity", "rarity.gd")]
    [InlineData("ConfigBundleDto", "config_bundle_dto.gd")]
    [InlineData("HealthResponse", "health_response.gd")]
    public void ToFileName_is_snake_case_gd(string input, string expected) =>
        GdNaming.ToFileName(input).Should().Be(expected);

    [Theory]
    [InlineData("class", "class_")]   // wire "class" trùng từ khoá GDScript → escape (HeroDefinitionDto).
    [InlineData("func", "func_")]
    [InlineData("var", "var_")]
    [InlineData("signal", "signal_")]
    [InlineData("heroId", "hero_id")] // không phải từ khoá → snake_case bình thường.
    [InlineData("def", "def")]        // "def" KHÔNG là từ khoá GDScript → giữ nguyên.
    public void ToFieldName_escapes_gdscript_reserved_words(string input, string expected) =>
        GdNaming.ToFieldName(input).Should().Be(expected);
}
