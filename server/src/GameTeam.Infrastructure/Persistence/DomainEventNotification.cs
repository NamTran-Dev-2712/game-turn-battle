using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// Cầu nối <see cref="IDomainEvent"/> (marker thuần của Domain — KHÔNG là <see cref="INotification"/> để giữ
/// Domain package-free) sang MediatR. Bọc mỗi domain event trong wrapper generic này rồi
/// <see cref="IPublisher.Publish"/>; handler nghiệp vụ (phase 19+) đăng ký
/// <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c> theo đúng kiểu event của nó.
/// </summary>
/// <typeparam name="TDomainEvent">Kiểu domain event cụ thể.</typeparam>
public sealed class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public DomainEventNotification(TDomainEvent domainEvent) => DomainEvent = domainEvent;

    /// <summary>Domain event được bọc.</summary>
    public TDomainEvent DomainEvent { get; }
}
