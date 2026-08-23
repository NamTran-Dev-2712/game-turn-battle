using System.Reflection;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// DbContext gốc của backend (nguồn sự thật PostgreSQL — ADR-007). Mapping tách theo
/// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> (nạp qua
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, System.Func{Type, bool})"/>).
/// <para>
/// <b>Domain event dispatch:</b> <see cref="SaveChangesAsync(CancellationToken)"/> được override để —
/// sau khi persist — publish event thu từ mọi aggregate đang track (<see cref="IHasDomainEvents"/>) qua
/// <see cref="DomainEventDispatcher"/>, rồi clear. Vì <see cref="UnitOfWork.CommitAsync"/> gọi SaveChanges
/// bên trong transaction đang mở, dispatch xảy ra <b>sau persist, trước commit</b> ⇒ cùng transaction (ADR-007).
/// </para>
/// Phase 11 chỉ chứa neo schema version (<see cref="SchemaMetadata"/>); bảng nghiệp vụ ở phase 19+.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly DomainEventDispatcher _dispatcher;

    public AppDbContext(DbContextOptions<AppDbContext> options, DomainEventDispatcher dispatcher)
        : this((DbContextOptions)options, dispatcher)
    {
    }

    /// <summary>
    /// Ctor không generic cho <b>DbContext dẫn xuất</b> (vd context test bổ sung entity mẫu) truyền
    /// <c>DbContextOptions&lt;TDerived&gt;</c> của chính nó. Runtime luôn dùng ctor generic ở trên.
    /// </summary>
    protected AppDbContext(DbContextOptions options, DomainEventDispatcher dispatcher)
        : base(options)
    {
        _dispatcher = Guard.NotNull(dispatcher);
    }

    /// <summary>Neo schema version của persistence (ADR-007).</summary>
    public DbSet<SchemaMetadata> SchemaMetadata => Set<SchemaMetadata>();

    /// <summary>Tài khoản người chơi — ranh giới định danh state server-authoritative (Phase 18).</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Hồ sơ người chơi — gốc save server-authoritative, gắn 1-1 với Account (Phase 19, ADR-007).</summary>
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Nạp mọi IEntityTypeConfiguration<T> trong assembly Infrastructure (một file/entity, snake_case tường minh).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Persist thay đổi rồi dispatch domain event thu từ aggregate (sau persist, cùng transaction).
    /// Thu thập event <b>trước</b> khi <c>base.SaveChangesAsync</c> có thể reset trạng thái tracking, rồi clear
    /// sau khi dispatch để không bắn lại ở lần lưu kế.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        List<IHasDomainEvents> aggregatesWithEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        List<IDomainEvent> domainEvents = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        int result = await base.SaveChangesAsync(cancellationToken);

        if (domainEvents.Count > 0)
        {
            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);

            foreach (IHasDomainEvents aggregate in aggregatesWithEvents)
            {
                aggregate.ClearDomainEvents();
            }
        }

        return result;
    }
}
