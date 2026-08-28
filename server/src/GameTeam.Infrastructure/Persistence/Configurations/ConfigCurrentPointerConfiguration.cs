using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping for <see cref="ConfigCurrentPointer"/> — table <c>config_current</c>, one singleton row
/// (Phase 21). No seed: the pointer row is created by the first successful publish (there is no valid
/// "current" until a bundle exists), so <c>config_current</c> being empty means "nothing published yet".
/// </summary>
public sealed class ConfigCurrentPointerConfiguration : IEntityTypeConfiguration<ConfigCurrentPointer>
{
    public void Configure(EntityTypeBuilder<ConfigCurrentPointer> builder)
    {
        builder.ToTable("config_current");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CurrentVersion)
            .HasColumnName("current_version")
            .IsRequired();
    }
}
