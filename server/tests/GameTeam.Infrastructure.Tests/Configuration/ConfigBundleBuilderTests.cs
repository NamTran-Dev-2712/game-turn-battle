using System.Text.Json.Nodes;
using FluentAssertions;
using GameTeam.ConfigValidator;
using GameTeam.Infrastructure.Configuration;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Configuration;

/// <summary>
/// Unit tests for the bundle checksum (Phase 21) — determinism is the contract: the checksum drives
/// publish dedup + integrity, so it must be stable regardless of file discovery order or JSON key order,
/// and must change when any value changes. No Docker required.
/// </summary>
public sealed class ConfigBundleBuilderTests
{
    private static ConfigEntity Hero(string id, string json)
        => new($"config/heroes/{id}.json", ConfigType.Hero, JsonNode.Parse(json));

    private static string ChecksumOf(params ConfigEntity[] entities)
        => ConfigBundleBuilder.ComputeChecksum(1, ConfigBundleBuilder.BuildData(entities));

    [Fact]
    public void Checksum_is_deterministic_for_the_same_content()
    {
        ChecksumOf(Hero("hero_a", """{"id":"hero_a","hp":1}"""))
            .Should().Be(ChecksumOf(Hero("hero_a", """{"id":"hero_a","hp":1}""")));
    }

    [Fact]
    public void Checksum_is_independent_of_json_key_order()
    {
        ChecksumOf(Hero("hero_a", """{"id":"hero_a","hp":1,"atk":2}"""))
            .Should().Be(ChecksumOf(Hero("hero_a", """{"atk":2,"hp":1,"id":"hero_a"}""")));
    }

    [Fact]
    public void Checksum_is_independent_of_entity_discovery_order()
    {
        string forward = ChecksumOf(
            Hero("hero_a", """{"id":"hero_a"}"""),
            Hero("hero_b", """{"id":"hero_b"}"""));
        string reverse = ChecksumOf(
            Hero("hero_b", """{"id":"hero_b"}"""),
            Hero("hero_a", """{"id":"hero_a"}"""));

        forward.Should().Be(reverse);
    }

    [Fact]
    public void Checksum_changes_when_a_value_changes()
    {
        ChecksumOf(Hero("hero_a", """{"id":"hero_a","hp":1}"""))
            .Should().NotBe(ChecksumOf(Hero("hero_a", """{"id":"hero_a","hp":2}""")));
    }

    [Fact]
    public void ComposeBundleJson_embeds_metadata_and_keeps_checksum_stable_across_generated_at()
    {
        JsonObject data = ConfigBundleBuilder.BuildData([Hero("hero_a", """{"id":"hero_a","hp":9}""")]);
        string checksum = ConfigBundleBuilder.ComputeChecksum(1, data);

        string early = ConfigBundleBuilder.ComposeBundleJson(1, "config@v1", checksum, DateTimeOffset.UnixEpoch, data);
        string later = ConfigBundleBuilder.ComposeBundleJson(1, "config@v1", checksum, DateTimeOffset.UnixEpoch.AddHours(5), data);

        JsonNode earlyDoc = JsonNode.Parse(early)!;
        earlyDoc["config_version"]!.GetValue<string>().Should().Be("config@v1");
        earlyDoc["schema_version"]!.GetValue<int>().Should().Be(1);
        earlyDoc["checksum"]!.GetValue<string>().Should().Be(checksum);
        earlyDoc["data"]!["hero"]!["hero_a"]!["hp"]!.GetValue<int>().Should().Be(9);

        // generated_at is metadata only ⇒ different documents, same content checksum.
        early.Should().NotBe(later);
        JsonNode.Parse(later)!["checksum"]!.GetValue<string>().Should().Be(checksum);
    }

    [Fact]
    public void BuildData_always_includes_all_eight_type_keys()
    {
        JsonObject data = ConfigBundleBuilder.BuildData([Hero("hero_a", """{"id":"hero_a"}""")]);

        foreach (ConfigType type in Enum.GetValues<ConfigType>())
        {
            data.ContainsKey(ConfigBundleBuilder.TypeKey(type)).Should().BeTrue();
        }

        (data["hero"] as JsonObject)!.ContainsKey("hero_a").Should().BeTrue();
        (data["skill"] as JsonObject)!.Count.Should().Be(0);
    }
}
