using MediatR;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>Publisher rỗng cho test không quan tâm dispatch (CRUD/rollback/migration).</summary>
internal sealed class NoOpPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}
