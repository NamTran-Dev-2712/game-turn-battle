using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace GameTeam.ConfigValidator.Tests;

public sealed class IdIndexTests
{
    private static ConfigEntity Entity(ConfigType type, string id) =>
        new($"{type}/{id}.json", type, JsonNode.Parse($$"""{ "id": "{{id}}" }"""));

    [Fact]
    public void Build_indexes_ids_by_type_for_lookup()
    {
        IdIndex index = IdIndex.Build([
            Entity(ConfigType.Hero, "hero_a"),
            Entity(ConfigType.Skill, "skill_a"),
        ]);

        index.Contains(ConfigType.Hero, "hero_a").Should().BeTrue();
        index.Contains(ConfigType.Skill, "skill_a").Should().BeTrue();
    }

    [Fact]
    public void Lookup_is_type_scoped_same_id_different_type_does_not_leak()
    {
        IdIndex index = IdIndex.Build([Entity(ConfigType.Hero, "shared_id")]);

        index.Contains(ConfigType.Hero, "shared_id").Should().BeTrue();
        index.Contains(ConfigType.Skill, "shared_id").Should().BeFalse();
    }

    [Fact]
    public void Missing_id_is_not_contained()
    {
        IdIndex index = IdIndex.Build([Entity(ConfigType.Hero, "hero_a")]);

        index.Contains(ConfigType.Hero, "hero_ghost").Should().BeFalse();
    }

    [Fact]
    public void Entities_without_id_are_ignored()
    {
        ConfigEntity noId = new("heroes/x.json", ConfigType.Hero, JsonNode.Parse("""{ "name": "x" }"""));
        IdIndex index = IdIndex.Build([noId]);

        index.Contains(ConfigType.Hero, "x").Should().BeFalse();
    }
}
