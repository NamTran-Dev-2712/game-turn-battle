using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Auth.Commands;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
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

        var handler = new CreateGuestAccountCommandHandler(repository, tokenService, Clock);

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
