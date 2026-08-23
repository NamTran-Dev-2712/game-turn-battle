using System;
using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Profiles;
using Xunit;

namespace GameTeam.Domain.Tests.Profiles;

/// <summary>
/// Phase 19 — the <see cref="PlayerProfile"/> aggregate: creation, identity/version stamping, event, and
/// the schema-version migration chain (<see cref="PlayerProfile.Upgrade"/>) that preserves data (ADR-007).
/// </summary>
public class PlayerProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateForAccount_sets_identity_account_defaults_and_current_version()
    {
        Guid id = Guid.NewGuid();
        Guid accountId = Guid.NewGuid();

        PlayerProfile profile = PlayerProfile.CreateForAccount(id, accountId, Now);

        profile.Id.Should().Be(id);
        profile.AccountId.Should().Be(accountId);
        profile.DisplayName.Should().Be(PlayerProfile.DefaultDisplayName);
        profile.Level.Should().Be(PlayerProfile.InitialLevel);
        profile.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);
        profile.CreatedAt.Should().Be(Now);
        profile.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void CreateForAccount_raises_PlayerProfileCreated_event()
    {
        Guid id = Guid.NewGuid();
        Guid accountId = Guid.NewGuid();

        PlayerProfile profile = PlayerProfile.CreateForAccount(id, accountId, Now);

        profile.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PlayerProfileCreated>();
        var created = (PlayerProfileCreated)profile.DomainEvents.Single();
        created.ProfileId.Should().Be(id);
        created.AccountId.Should().Be(accountId);
    }

    [Fact]
    public void CreateForAccount_rejects_empty_ids()
    {
        Action emptyId = () => PlayerProfile.CreateForAccount(Guid.Empty, Guid.NewGuid(), Now);
        Action emptyAccount = () => PlayerProfile.CreateForAccount(Guid.NewGuid(), Guid.Empty, Now);

        emptyId.Should().Throw<ArgumentException>();
        emptyAccount.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Upgrade_migrates_legacy_v0_to_current_and_preserves_data()
    {
        // A legacy v0 record: no display name yet, but a meaningful Level that MUST survive the migration.
        const int preservedLevel = 7;
        PlayerProfile legacy = PlayerProfile.Restore(
            Guid.NewGuid(),
            Guid.NewGuid(),
            displayName: string.Empty,
            level: preservedLevel,
            schemaVersion: 0,
            createdAt: Now,
            updatedAt: Now);

        bool changed = legacy.Upgrade(Later);

        changed.Should().BeTrue();
        legacy.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);
        legacy.Level.Should().Be(preservedLevel, "migration must not lose existing player data");
        legacy.DisplayName.Should().NotBeNullOrWhiteSpace("v0→v1 back-fills a missing display name");
        legacy.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void Upgrade_is_noop_when_already_current()
    {
        PlayerProfile current = PlayerProfile.CreateForAccount(Guid.NewGuid(), Guid.NewGuid(), Now);

        bool changed = current.Upgrade(Later);

        changed.Should().BeFalse();
        current.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);
        current.UpdatedAt.Should().Be(Now, "a no-op upgrade must not bump the timestamp");
    }
}
