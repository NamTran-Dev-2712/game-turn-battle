using GameTeam.Domain.Common;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>Domain event mẫu — bắn khi <see cref="SampleAggregate"/> được tạo.</summary>
public sealed record SampleCreated(Guid Id, string Name) : IDomainEvent;
