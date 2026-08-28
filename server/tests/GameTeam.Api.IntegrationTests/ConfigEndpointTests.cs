using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Contracts.Common;
using GameTeam.Contracts.Config;
using GameTeam.Infrastructure.Configuration;
using GameTeam.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 21 — Configuration Service endpoints end-to-end over the real HTTP host + real PostgreSQL
/// (Testcontainers). The deploy-time publish (startup hosted service) publishes <c>config@v1</c> from a
/// temp config tree; then <c>GET /api/v1/config/current</c> + <c>/config/bundle</c> are served PUBLICLY
/// (no token) and an unknown version returns a 404 <see cref="ErrorEnvelope"/>. Requires Docker.
/// </summary>
public sealed class ConfigEndpointTests : IClassFixture<ConfigApiFactory>
{
    private readonly ConfigApiFactory _factory;

    public ConfigEndpointTests(ConfigApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Current_is_public_and_reports_the_deploy_published_version()
    {
        // No Authorization header — config endpoints are anonymous.
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/config/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ConfigBundleDto dto = (await response.Content.ReadFromJsonAsync<ConfigBundleDto>())!;
        dto.Version.Bundle.Should().Be(1);
        dto.Version.Schema.Should().Be(1);
    }

    [Fact]
    public async Task Bundle_by_version_returns_the_immutable_document_publicly()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/config/bundle?bundleVersion=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        JsonNode doc = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        doc["config_version"]!.GetValue<string>().Should().Be("config@v1");
        doc["schema_version"]!.GetValue<int>().Should().Be(1);
        doc["checksum"]!.GetValue<string>().Should().NotBeNullOrEmpty();
        doc["data"]!["hero"]!["hero_sample"]!["base_stats"]!["hp"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Bundle_without_version_returns_the_current_bundle()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/config/bundle");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonNode doc = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        doc["config_version"]!.GetValue<string>().Should().Be("config@v1");
    }

    [Fact]
    public async Task Unknown_version_returns_404_error_envelope()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/config/bundle?bundleVersion=999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ErrorEnvelope envelope = (await response.Content.ReadFromJsonAsync<ErrorEnvelope>())!;
        envelope.Error.Code.Should().Be("CONFIG_BUNDLE_NOT_FOUND");
    }
}

/// <summary>
/// Real-host factory backed by a Testcontainers PostgreSQL instance, with a temp config tree the
/// deploy-time publish (startup hosted service) publishes as <c>config@v1</c>. Schema is pre-created
/// before the host boots so the startup publish finds its tables; Redis is stubbed
/// (<see cref="NoOpCacheService"/> ⇒ the bundle store falls back to the DB). JWT config matches
/// <see cref="ApiTestFactory"/>.
/// </summary>
public sealed class ConfigApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    private readonly string _configDir =
        Path.Combine(Path.GetTempPath(), "cfgapi-" + Guid.NewGuid().ToString("N"));

    private static string SchemaRoot => ConfigPathResolver.Resolve("shared/config-schema");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", ApiTestFactory.JwtSigningKey);
        builder.UseSetting("Jwt:Issuer", ApiTestFactory.JwtIssuer);
        builder.UseSetting("Jwt:Audience", ApiTestFactory.JwtAudience);
        builder.UseSetting("Jwt:AccessTokenMinutes", ApiTestFactory.JwtAccessTokenMinutes.ToString());
        builder.UseSetting("ConnectionStrings:Postgres", _container.GetConnectionString());
        builder.UseSetting($"{ConfigServiceOptions.SectionName}:ConfigRoot", _configDir);
        builder.UseSetting($"{ConfigServiceOptions.SectionName}:SchemaRoot", SchemaRoot);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();
        });
    }

    public async Task InitializeAsync()
    {
        WriteValidConfig(_configDir);
        await _container.StartAsync();

        // Pre-create the schema WITHOUT booting the host, so the startup publish finds its tables.
        DbContextOptions<AppDbContext> options =
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_container.GetConnectionString()).Options;
        await using (AppDbContext db = new(options, new DomainEventDispatcher(new NoOpApiPublisher())))
        {
            await db.Database.EnsureCreatedAsync();
        }

        // Boot the host ⇒ ConfigPublishHostedService runs the deploy-time publish (config@v1).
        _ = Services;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
        try
        {
            Directory.Delete(_configDir, recursive: true);
        }
        catch (IOException)
        {
            // best-effort temp cleanup
        }
    }

    private static void WriteValidConfig(string root)
    {
        string heroes = Directory.CreateDirectory(Path.Combine(root, "heroes")).FullName;
        File.WriteAllText(Path.Combine(heroes, "hero_sample.json"),
            """
            {
              "schema_version": 1,
              "id": "hero_sample",
              "faction": "none",
              "class": "warrior",
              "element": "fire",
              "role": "tank",
              "rarity": 3,
              "base_stats": { "hp": 0, "atk": 0, "def": 0, "spd": 0 },
              "skills": ["skill_sample_basic"]
            }
            """);

        string skills = Directory.CreateDirectory(Path.Combine(root, "skills")).FullName;
        File.WriteAllText(Path.Combine(skills, "skill_sample_basic.json"),
            """
            {
              "schema_version": 1,
              "id": "skill_sample_basic",
              "target": "single_enemy",
              "trigger": { "type": "cooldown", "value": 0 },
              "effects": [ { "effect_type": "damage", "params": {} } ]
            }
            """);
    }
}

/// <summary>Empty MediatR publisher for pre-creating the schema (no domain events involved).</summary>
internal sealed class NoOpApiPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}
