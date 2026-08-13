using FluentValidation;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameTeam.Application.Tests.TestSupport;

/// <summary>
/// Builds a real DI container wired via <see cref="GameTeam.Application.DependencyInjection.AddApplication"/>
/// (the actual behaviors + order), plus the test-assembly probe handlers/validators and recording
/// port fakes. Requests are sent through the real <see cref="IMediator"/> — behaviors are never
/// bypassed.
/// </summary>
public sealed class TestHost : IDisposable
{
    private TestHost(ServiceProvider services, ExecutionRecorder recorder, RecordingLoggerProvider logger)
    {
        Services = services;
        Recorder = recorder;
        Logger = logger;
    }

    public ServiceProvider Services { get; }

    public ExecutionRecorder Recorder { get; }

    public RecordingLoggerProvider Logger { get; }

    public IMediator Mediator => Services.GetRequiredService<IMediator>();

    /// <param name="feedLoggerToRecorder">
    /// When true the recording logger forwards LoggingBehavior's start/end markers into
    /// <see cref="Recorder"/> so pipeline order includes the logging boundary.
    /// </param>
    public static TestHost Create(
        bool feedLoggerToRecorder = false,
        DateTimeOffset? clockUtcNow = null,
        ConfigVersion? configVersion = null)
    {
        var recorder = new ExecutionRecorder();
        var loggerProvider = new RecordingLoggerProvider(feedLoggerToRecorder ? recorder : null);

        var services = new ServiceCollection();

        // Real Application composition (MediatR + FluentValidation + the 4 ordered behaviors).
        services.AddApplication();

        // Probe handlers/validators live in the test assembly — register them too.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TestHost).Assembly));
        services.AddValidatorsFromAssembly(typeof(TestHost).Assembly);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(loggerProvider);
        });

        // Recording port fakes (Infrastructure implements the real ones in later phases).
        services.AddSingleton(recorder);
        services.AddSingleton<IUnitOfWork>(new RecordingUnitOfWork(recorder));
        services.AddSingleton<ICacheService>(new RecordingCacheService(recorder));
        services.AddSingleton<IConfigProvider>(
            new FixedConfigProvider(configVersion ?? new ConfigVersion(7, 1)));
        services.AddSingleton<IClock>(
            new FixedClock(clockUtcNow ?? new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero)));

        return new TestHost(services.BuildServiceProvider(), recorder, loggerProvider);
    }

    public void Dispose() => Services.Dispose();
}
