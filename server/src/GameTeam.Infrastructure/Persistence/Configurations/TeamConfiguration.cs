using GameTeam.Domain.Profiles;
using GameTeam.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="Team"/> — bảng <c>teams</c> + owned collection <c>team_slots</c>, cột
/// <c>snake_case</c> tường minh. Id là <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>). <c>profile_id</c>
/// có khoá ngoại tới <c>player_profiles.id</c> (cascade) + ràng buộc <b>unique</b> (một đội / một profile —
/// idempotency tầng DB, MVP). Ô đội (<see cref="TeamSlot"/>) là owned entity, PK ghép
/// <c>(team_id, slot_index)</c>. Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasColumnName("schema_version")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Idempotency tầng DB: một đội hình cho một profile (MVP — preset/nhiều đội là Post-MVP).
        builder.HasIndex(x => x.ProfileId)
            .IsUnique()
            .HasDatabaseName("ix_teams_profile_id");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(x => x.Slots, slots =>
        {
            slots.ToTable("team_slots");

            slots.WithOwner().HasForeignKey("team_id");

            slots.Property<Guid>("team_id").HasColumnName("team_id");

            slots.Property(s => s.SlotIndex)
                .HasColumnName("slot_index")
                .ValueGeneratedNever()
                .IsRequired();

            slots.Property(s => s.HeroId)
                .HasColumnName("hero_id")
                .IsRequired();

            slots.HasKey("team_id", nameof(TeamSlot.SlotIndex));
        });

        builder.Navigation(x => x.Slots)
            .HasField("_slots")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
