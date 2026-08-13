using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Behaviors;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Domain.Common;
using MediatR;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Behaviors;

/// <summary>
/// TransactionBehavior: begins a transaction for a transactional command, commits on success, rolls
/// back on a failed <see cref="Result"/> or a thrown exception; a non-transactional request never
/// begins a transaction.
/// </summary>
public sealed class TransactionBehaviorTests
{
    private static readonly Error SomeError = new("SOME_FAILURE", "boom");

    private static TransactionBehavior<ProbeCommand, Result> BehaviorFor(IUnitOfWork unitOfWork)
        => new(unitOfWork);

    [Fact]
    public async Task Begins_and_commits_on_success()
    {
        IUnitOfWork uow = Substitute.For<IUnitOfWork>();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        Result result = await BehaviorFor(uow).Handle(new ProbeCommand(true), next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rolls_back_on_result_failure()
    {
        IUnitOfWork uow = Substitute.For<IUnitOfWork>();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Failure(SomeError));

        Result result = await BehaviorFor(uow).Handle(new ProbeCommand(true), next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await uow.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rolls_back_and_rethrows_on_exception()
    {
        IUnitOfWork uow = Substitute.For<IUnitOfWork>();
        RequestHandlerDelegate<Result> next = () => throw new InvalidOperationException("kaboom");

        Func<Task> act = () => BehaviorFor(uow).Handle(new ProbeCommand(true), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_transactional_request_never_begins_a_transaction()
    {
        // ProbeQuery is NOT ITransactionalRequest → TransactionBehavior is never applied.
        using TestHost host = TestHost.Create();

        await host.Mediator.Send(new ProbeQuery());

        host.Recorder.Steps.Should().NotContain("tx:begin");
        host.Recorder.Steps.Should().NotContain("tx:commit");
    }
}
