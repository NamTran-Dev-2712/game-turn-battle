using FluentAssertions;
using GameTeam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Kiểm chứng migration <c>Initial</c> chạy sạch trên PostgreSQL thật: <b>up</b> tạo bảng
/// <c>schema_metadata</c> + seed version=1; <b>down</b> (về "0") revert sạch. Dùng <see cref="AppDbContext"/>
/// production (không phải TestDbContext) để đúng model migration. Container riêng (class fixture) ⇒ DB sạch.
/// </summary>
public sealed class MigrationIntegrationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public MigrationIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    private AppDbContext NewAppContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_fixture.ConnectionString).Options,
            new DomainEventDispatcher(new NoOpPublisher()));

    private async Task<bool> TableExistsAsync(string table)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT to_regclass('public.{table}')::text", connection);
        object? result = await command.ExecuteScalarAsync();
        return result is not null and not DBNull;
    }

    [Fact]
    public async Task Initial_migration_up_creates_schema_and_seeds_version_then_down_reverts()
    {
        await using AppDbContext context = NewAppContext();
        IMigrator migrator = context.GetService<IMigrator>();

        // ── UP ────────────────────────────────────────────────────────────────────────────────
        await migrator.MigrateAsync();

        (await TableExistsAsync("schema_metadata")).Should().BeTrue("migration up phải tạo bảng schema_metadata.");

        SchemaMetadata seeded = await context.SchemaMetadata.AsNoTracking().SingleAsync();
        seeded.Id.Should().Be(SchemaMetadata.SingletonId);
        seeded.Version.Should().Be(1, "migration phải seed neo schema version = 1 (ADR-007).");

        // ── DOWN (về "0") ───────────────────────────────────────────────────────────────────────
        await migrator.MigrateAsync("0");

        (await TableExistsAsync("schema_metadata")).Should().BeFalse("migration down phải revert bảng schema_metadata.");
    }
}
