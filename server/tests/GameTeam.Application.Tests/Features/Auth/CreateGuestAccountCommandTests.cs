using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Auth.Commands;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Features.Auth;

/// <summary>
/// Phase 18 — the guest-login command creates a guest account, stages it for persistence, and issues
/// tokens through the <see cref="ITokenService"/> port (no JWT knowledge in Application).
/// </summary>
public sealed class CreateGuestAccountCommandTests
{
    private static readonly FixedClock Clock =
        new(new DateTimeOffset(2026, 8, 21, 9, 0, 0, TimeSpan.Zero));

    private static readonly string[] SeededHeroIds = ["hero_sample"];

    [Fact]
    public async Task Handler_creates_guest_account_and_returns_token_from_port()
    {
        var repository = Substitute.For<IRepository<Account, Guid>>();
        var tokenService = Substitute.For<ITokenService>();
        tokenService.CreateTokens(Arg.Any<Guid>(), AccountType.Guest)
            .Returns(new TokenBundle("access-token", "refresh-token", 3600));

        // Capture the staged account (configured BEFORE the call, so the Do callback runs on invoke).
        Account? added = null;
        repository.AddAsync(Arg.Do<Account>(a => added = a), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Phase 19: the guest-login command also creates the player profile in the same transaction.
        var profiles = Substitute.For<IPlayerProfileRepository>();
        PlayerProfile? addedProfile = null;
        profiles.AddAsync(Arg.Do<PlayerProfile>(p => addedProfile = p), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Phase 27: the guest-login command also seeds owned heroes from config (temporary, until phase 33).
        var ownedHeroes = Substitute.For<IOwnedHeroRepository>();
        OwnedHero? addedHero = null;
        ownedHeroes.AddAsync(Arg.Do<OwnedHero>(h => addedHero = h), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var config = Substitute.For<IConfigProvider>();
        config.GetIds("hero").Returns(SeededHeroIds);

        var handler = new CreateGuestAccountCommandHandler(
            repository, profiles, ownedHeroes, config, tokenService, Clock);

        Result<AuthGuestResponse> result = await handler.Handle(
            new CreateGuestAccountCommand("device-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresInSeconds.Should().Be(3600);

        // A guest account was staged for persistence, and the SAME id was used to mint the token.
        await repository.Received(1).AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        added.Should().NotBeNull();
        added!.Type.Should().Be(AccountType.Guest);
        added.CreatedAt.Should().Be(Clock.UtcNow);
        tokenService.Received(1).CreateTokens(added.Id, AccountType.Guest);

        // A profile was staged for the SAME account, at the current schema version.
        await profiles.Received(1).AddAsync(Arg.Any<PlayerProfile>(), Arg.Any<CancellationToken>());
        addedProfile.Should().NotBeNull();
        addedProfile!.AccountId.Should().Be(added.Id);
        addedProfile.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);

        // An owned hero was seeded from config for the SAME profile (data-driven seed, Phase 27).
        await ownedHeroes.Received(1).AddAsync(Arg.Any<OwnedHero>(), Arg.Any<CancellationToken>());
        addedHero.Should().NotBeNull();
        addedHero!.ProfileId.Should().Be(addedProfile.Id);
        addedHero.HeroId.Should().Be("hero_sample");
        addedHero.Level.Should().Be(OwnedHero.InitialLevel);
        addedHero.Stars.Should().Be(OwnedHero.InitialStars);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("device-abc")]
    public void Validator_accepts_missing_or_bounded_device_id(string? deviceId)
    {
        var validator = new CreateGuestAccountCommandValidator();

        validator.Validate(new CreateGuestAccountCommand(deviceId)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_rejects_overlong_device_id()
    {
        var validator = new CreateGuestAccountCommandValidator();
        string tooLong = new('x', CreateGuestAccountCommandValidator.MaxDeviceIdLength + 1);

        validator.Validate(new CreateGuestAccountCommand(tooLong)).IsValid.Should().BeFalse();
    }
}
