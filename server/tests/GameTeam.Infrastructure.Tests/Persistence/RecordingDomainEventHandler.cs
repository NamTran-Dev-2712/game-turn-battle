using System.Collections.Concurrent;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using MediatR;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>Thu thập domain event đã dispatch để test kiểm chứng (đăng ký singleton).</summary>
public sealed class DispatchedEventsCollector
{
    private readonly ConcurrentQueue<IDomainEvent> _events = new();

    public void Add(IDomainEvent domainEvent) => _events.Enqueue(domainEvent);

    public IReadOnlyCollection<IDomainEvent> Events => _events.ToArray();
}

/// <summary>
/// Handler MediatR nhận <see cref="DomainEventNotification{TDomainEvent}"/> của <see cref="SampleCreated"/> —
/// chứng minh domain event được dispatch qua đúng kiểu event cụ thể sau SaveChanges.
/// </summary>
public sealed class RecordingDomainEventHandler
    : INotificationHandler<DomainEventNotification<SampleCreated>>
{
    private readonly DispatchedEventsCollector _collector;

    public RecordingDomainEventHandler(DispatchedEventsCollector collector) => _collector = collector;

    public Task Handle(DomainEventNotification<SampleCreated> notification, CancellationToken cancellationToken)
    {
        _collector.Add(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler cho <see cref="DomainEventNotification{TDomainEvent}"/> của <see cref="AccountCreated"/> (Phase 18) —
/// chứng minh event của aggregate nghiệp vụ Account cũng dispatch qua đúng kiểu cụ thể sau SaveChanges.
/// </summary>
public sealed class RecordingAccountCreatedHandler
    : INotificationHandler<DomainEventNotification<AccountCreated>>
{
    private readonly DispatchedEventsCollector _collector;

    public RecordingAccountCreatedHandler(DispatchedEventsCollector collector) => _collector = collector;

    public Task Handle(DomainEventNotification<AccountCreated> notification, CancellationToken cancellationToken)
    {
        _collector.Add(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler cho <see cref="DomainEventNotification{TDomainEvent}"/> của <see cref="PlayerProfileCreated"/> (Phase 19) —
/// để <see cref="PlayerProfilePersistenceTests.Saving_new_profile_dispatches_PlayerProfileCreated"/> ghi nhận được
/// event khi dispatch qua đúng kiểu cụ thể sau SaveChanges (không có handler này thì collector rỗng).
/// </summary>
public sealed class RecordingPlayerProfileCreatedHandler
    : INotificationHandler<DomainEventNotification<PlayerProfileCreated>>
{
    private readonly DispatchedEventsCollector _collector;

    public RecordingPlayerProfileCreatedHandler(DispatchedEventsCollector collector) => _collector = collector;

    public Task Handle(DomainEventNotification<PlayerProfileCreated> notification, CancellationToken cancellationToken)
    {
        _collector.Add(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler cho <see cref="DomainEventNotification{TDomainEvent}"/> của <see cref="OwnedHeroGranted"/> (Phase 27) —
/// chứng minh event khi cấp hero cũng dispatch qua đúng kiểu cụ thể sau SaveChanges.
/// </summary>
public sealed class RecordingOwnedHeroGrantedHandler
    : INotificationHandler<DomainEventNotification<OwnedHeroGranted>>
{
    private readonly DispatchedEventsCollector _collector;

    public RecordingOwnedHeroGrantedHandler(DispatchedEventsCollector collector) => _collector = collector;

    public Task Handle(DomainEventNotification<OwnedHeroGranted> notification, CancellationToken cancellationToken)
    {
        _collector.Add(notification.DomainEvent);
        return Task.CompletedTask;
    }
}
