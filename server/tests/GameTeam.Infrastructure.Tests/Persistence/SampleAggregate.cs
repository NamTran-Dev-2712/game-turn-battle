using GameTeam.Domain.Common;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Aggregate MẪU chỉ dùng cho integration test persistence (giữ schema production sạch — không nằm ở
/// <c>GameTeam.Infrastructure</c>). Raise <see cref="SampleCreated"/> khi khởi tạo để kiểm chứng dispatch
/// domain event tại SaveChanges.
/// </summary>
public sealed class SampleAggregate : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    private SampleAggregate()
    {
    }

    public SampleAggregate(Guid id, string name)
        : base(id)
    {
        Name = Guard.NotNull(name);
        RaiseDomainEvent(new SampleCreated(id, name));
    }
}
