using GameTeam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// DbContext dẫn xuất chỉ dùng cho test: thêm entity mẫu (<see cref="SampleAggregate"/>) để tập luyện
/// repository/UoW/domain-event dispatch mà <b>không</b> làm bẩn schema production. Kế thừa toàn bộ hành vi của
/// <see cref="AppDbContext"/> (đặc biệt override <c>SaveChangesAsync</c> dispatch domain event).
/// </summary>
public sealed class TestDbContext : AppDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options, DomainEventDispatcher dispatcher)
        : base(options, dispatcher)
    {
    }

    public DbSet<SampleAggregate> SampleAggregates => Set<SampleAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // base: nạp config của Infrastructure (schema_metadata).
        base.OnModelCreating(modelBuilder);

        // Bổ sung config entity mẫu (assembly test).
        modelBuilder.ApplyConfiguration(new SampleAggregateConfiguration());
    }
}
