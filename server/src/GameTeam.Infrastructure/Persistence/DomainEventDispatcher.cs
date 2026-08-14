using System.Collections.Concurrent;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// Dispatch domain event thu từ aggregate sang MediatR. Mỗi <see cref="IDomainEvent"/> được bọc trong
/// <see cref="DomainEventNotification{TDomainEvent}"/> (đóng generic theo kiểu runtime) rồi publish —
/// nhờ vậy handler đăng ký theo kiểu event cụ thể vẫn nhận đúng (không cần <c>IDomainEvent</c> là INotification).
/// <para>
/// Gọi bởi <see cref="AppDbContext"/> <b>sau</b> khi persist, <b>trong</b> transaction đang mở (ADR-007) —
/// xem <see cref="AppDbContext.SaveChangesAsync(System.Threading.CancellationToken)"/>.
/// Handler tái nhập (re-entrant) / outbox bền vững nằm ngoài phạm vi Phase 11.
/// </para>
/// </summary>
public sealed class DomainEventDispatcher
{
    // Cache đóng-generic factory theo kiểu event để tránh reflection lặp mỗi lần publish.
    private static readonly ConcurrentDictionary<Type, Func<IDomainEvent, INotification>> WrapperFactories = new();

    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher) => _publisher = Guard.NotNull(publisher);

    /// <summary>Publish tuần tự danh sách domain event (đã thu thập trước khi clear).</summary>
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            INotification notification = Wrap(domainEvent);
            await _publisher.Publish(notification, cancellationToken);
        }
    }

    private static INotification Wrap(IDomainEvent domainEvent)
    {
        Func<IDomainEvent, INotification> factory = WrapperFactories.GetOrAdd(
            domainEvent.GetType(),
            static eventType =>
            {
                Type notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
                return e => (INotification)Activator.CreateInstance(notificationType, e)!;
            });

        return factory(domainEvent);
    }
}
