using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTeam.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF mapping cho <see cref="CurrencyTransaction"/> — bảng <c>currency_transactions</c> (append-only), cột
/// <c>snake_case</c> tường minh. Id là <c>uuid</c> sinh ở code (<c>ValueGeneratedNever</c>).
/// <c>idempotency_key</c> có ràng buộc <b>unique</b> — bảo đảm idempotency tầng DB (ADR-007): một key = một
/// giao dịch (chống double-grant/double-spend khi race). Có index theo <c>profile_id</c> để truy vấn/audit.
/// FK tới <c>player_profiles</c> (cascade). Domain event KHÔNG map (Ignore).
/// </summary>
public sealed class CurrencyTransactionConfiguration : IEntityTypeConfiguration<CurrencyTransaction>
{
    public void Configure(EntityTypeBuilder<CurrencyTransaction> builder)
    {
        builder.ToTable("currency_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").IsRequired();
        builder.Property(x => x.Delta).HasColumnName("delta").IsRequired();
        builder.Property(x => x.BalanceAfter).HasColumnName("balance_after").IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        // Idempotency tầng DB (ADR-007): một idempotency_key = một giao dịch (backstop chống race double).
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ix_currency_transactions_idempotency_key");

        // Truy vấn/audit theo profile.
        builder.HasIndex(x => x.ProfileId)
            .HasDatabaseName("ix_currency_transactions_profile_id");

        builder.HasOne<PlayerProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
