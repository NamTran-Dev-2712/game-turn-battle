using GameTeam.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="Account"/> — bảng <c>accounts</c>, cột <c>snake_case</c> tường minh
/// (một file/entity). Id là <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>). Domain event KHÔNG map
/// (Ignore) — chỉ dùng để dispatch, không persist. Chưa có bảng liên kết provider (Post-MVP, ADR-006).
/// </summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Domain event chỉ để dispatch (Phase 11) — không phải cột.
        builder.Ignore(x => x.DomainEvents);
    }
}
