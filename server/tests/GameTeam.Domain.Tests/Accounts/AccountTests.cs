using System;
using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Accounts;
using Xunit;

namespace GameTeam.Domain.Tests.Accounts;

/// <summary>Phase 18 — the <see cref="Account"/> guest aggregate: identity, type, created-at, event.</summary>
public class AccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateGuest_sets_identity_type_and_created_at()
    {
        Guid id = Guid.NewGuid();

        Account account = Account.CreateGuest(id, Now);

        account.Id.Should().Be(id);
        account.Type.Should().Be(AccountType.Guest);
        account.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void CreateGuest_raises_AccountCreated_event()
    {
        Guid id = Guid.NewGuid();

        Account account = Account.CreateGuest(id, Now);

        account.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<AccountCreated>();
        var created = (AccountCreated)account.DomainEvents.Single();
        created.AccountId.Should().Be(id);
        created.Type.Should().Be(AccountType.Guest);
    }

    [Fact]
    public void CreateGuest_rejects_empty_id()
    {
        Action act = () => Account.CreateGuest(Guid.Empty, Now);

        act.Should().Throw<ArgumentException>();
    }
}
