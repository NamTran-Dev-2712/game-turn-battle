using FluentAssertions;
using Xunit;

namespace GameTeam.ConfigValidator.Tests;

public sealed class ConfigFileMapperTests
{
    [Theory]
    [InlineData("heroes", ConfigType.Hero)]
    [InlineData("skills", ConfigType.Skill)]
    [InlineData("stages", ConfigType.Stage)]
    [InlineData("gacha", ConfigType.Gacha)]
    [InlineData("shop", ConfigType.Shop)]
    [InlineData("rewards", ConfigType.Reward)]
    [InlineData("economy", ConfigType.Economy)]
    [InlineData("quests", ConfigType.Quest)]
    public void Plural_directory_maps_to_singular_type(string dir, ConfigType expected)
    {
        ConfigFileMapper.DirectoryToType[dir].Should().Be(expected);
    }

    [Theory]
    [InlineData(ConfigType.Hero, "hero.schema.json")]
    [InlineData(ConfigType.Reward, "reward.schema.json")]
    [InlineData(ConfigType.Quest, "quest.schema.json")]
    public void Type_maps_to_singular_schema_file(ConfigType type, string expected)
    {
        ConfigFileMapper.SchemaFileName(type).Should().Be(expected);
    }

    [Fact]
    public void All_eight_config_types_have_a_directory_mapping()
    {
        ConfigFileMapper.DirectoryToType.Values.Distinct().Should()
            .BeEquivalentTo(Enum.GetValues<ConfigType>());
    }

    [Fact]
    public void Metadata_directories_are_skipped()
    {
        ConfigFileMapper.SkippedDirectories.Should().Contain("liveops");
        ConfigFileMapper.SkippedDirectories.Should().Contain("_versions");
    }
}
