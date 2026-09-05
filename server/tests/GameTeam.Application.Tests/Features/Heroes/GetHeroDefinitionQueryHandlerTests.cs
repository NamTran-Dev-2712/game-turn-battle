using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Features.Heroes.Queries;
using GameTeam.Application.Tests.Combat;
using GameTeam.Contracts.Enums;
using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Application.Tests.Features.Heroes;

/// <summary>
/// Phase 27 — <see cref="GetHeroDefinitionQueryHandler"/> reads the hero definition from config through
/// <c>IConfigProvider</c> (data-driven, ADR-004): (A) reads the right values, (B) changing a config value
/// changes the result WITHOUT any code change, (D) missing id → HERO_DEFINITION_NOT_FOUND.
/// </summary>
public sealed class GetHeroDefinitionQueryHandlerTests
{
    private const string HeroJsonTemplate = """
        {
          "schema_version": 1,
          "id": "hero_ignis",
          "faction": "none",
          "class": "mage",
          "element": "fire",
          "role": "dps",
          "rarity": 5,
          "base_stats": { "hp": 900, "atk": {ATK}, "def": 60, "spd": 110 },
          "skills": ["skill_ignis_strike"],
          "art": "res://assets/heroes/hero_ignis.png"
        }
        """;

    [Fact]
    public async Task Reads_definition_from_config()
    {
        var config = new FakeConfigProvider().Set("hero", "hero_ignis", HeroJsonTemplate.Replace("{ATK}", "220"));
        var handler = new GetHeroDefinitionQueryHandler(config);

        Result<HeroDefinitionDto> result =
            await handler.Handle(new GetHeroDefinitionQuery("hero_ignis"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        HeroDefinitionDto dto = result.Value;
        dto.HeroId.Should().Be("hero_ignis");
        dto.Faction.Should().Be("none");
        dto.Class.Should().Be(Class.Mage);
        dto.Element.Should().Be(Element.Fire);
        dto.Role.Should().Be(Role.Dps);
        dto.Rarity.Should().Be(Rarity.Five);
        dto.BaseStats.Atk.Should().Be(220);
        dto.BaseStats.Hp.Should().Be(900);
        dto.Skills.Should().ContainSingle().Which.Should().Be("skill_ignis_strike");
        dto.Art.Should().Be("res://assets/heroes/hero_ignis.png");
    }

    [Fact]
    public async Task Changing_config_value_changes_result_without_code_change()
    {
        var handler = new GetHeroDefinitionQueryHandler(
            new FakeConfigProvider().Set("hero", "hero_ignis", HeroJsonTemplate.Replace("{ATK}", "220")));
        Result<HeroDefinitionDto> before =
            await handler.Handle(new GetHeroDefinitionQuery("hero_ignis"), CancellationToken.None);

        // SAME handler code — only the config value changed.
        var handlerAfter = new GetHeroDefinitionQueryHandler(
            new FakeConfigProvider().Set("hero", "hero_ignis", HeroJsonTemplate.Replace("{ATK}", "999")));
        Result<HeroDefinitionDto> after =
            await handlerAfter.Handle(new GetHeroDefinitionQuery("hero_ignis"), CancellationToken.None);

        before.Value.BaseStats.Atk.Should().Be(220);
        after.Value.BaseStats.Atk.Should().Be(999);
    }

    [Fact]
    public async Task Returns_not_found_when_definition_absent()
    {
        var handler = new GetHeroDefinitionQueryHandler(new FakeConfigProvider());

        Result<HeroDefinitionDto> result =
            await handler.Handle(new GetHeroDefinitionQuery("hero_missing"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("HERO_DEFINITION_NOT_FOUND");
    }
}
