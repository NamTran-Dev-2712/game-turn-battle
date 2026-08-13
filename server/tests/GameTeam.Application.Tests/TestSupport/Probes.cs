using FluentValidation;
using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Tests.TestSupport;

// Probe requests routed through the REAL AddApplication pipeline. Their validators/handlers record
// each step into the shared ExecutionRecorder so tests can assert real behavior + ordering.

/// <summary>Transactional probe command (opts into TransactionBehavior).</summary>
public sealed record ProbeCommand(bool IsValid) : IRequest<Result>, ITransactionalRequest;

public sealed class ProbeCommandValidator : AbstractValidator<ProbeCommand>
{
    public ProbeCommandValidator(ExecutionRecorder recorder)
        => RuleFor(x => x.IsValid).Custom((value, context) =>
        {
            recorder.Add("validate");
            if (!value)
            {
                context.AddFailure("IsValid must be true.");
            }
        });
}

public sealed class ProbeCommandHandler(ExecutionRecorder recorder)
    : IRequestHandler<ProbeCommand, Result>
{
    public Task<Result> Handle(ProbeCommand request, CancellationToken cancellationToken)
    {
        recorder.Add("handler");
        return Task.FromResult(Result.Success());
    }
}

/// <summary>Cacheable probe query (opts into CachingBehavior).</summary>
public sealed record ProbeQuery : IRequest<Result<string>>, ICacheableQuery
{
    public string CacheKey => "probe";

    public TimeSpan CacheTtl => TimeSpan.FromSeconds(1);
}

public sealed class ProbeQueryValidator : AbstractValidator<ProbeQuery>
{
    public ProbeQueryValidator(ExecutionRecorder recorder)
        => RuleFor(x => x).Custom((_, _) => recorder.Add("validate"));
}

public sealed class ProbeQueryHandler(ExecutionRecorder recorder)
    : IRequestHandler<ProbeQuery, Result<string>>
{
    public Task<Result<string>> Handle(ProbeQuery request, CancellationToken cancellationToken)
    {
        recorder.Add("handler");
        return Task.FromResult(Result.Success("probe-value"));
    }
}
