using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>EF mapping cho aggregate mẫu (snake_case tường minh) — chỉ dùng ở <see cref="TestDbContext"/>.</summary>
public sealed class SampleAggregateConfiguration : IEntityTypeConfiguration<SampleAggregate>
{
    public void Configure(EntityTypeBuilder<SampleAggregate> builder)
    {
        builder.ToTable("sample_aggregate");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        // Domain event không được map thành cột (chỉ tồn tại trong bộ nhớ để dispatch).
        builder.Ignore(x => x.DomainEvents);
    }
}
