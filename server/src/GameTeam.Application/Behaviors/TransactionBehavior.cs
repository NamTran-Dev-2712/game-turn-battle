using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Behaviors;

/// <summary>
/// Wraps write commands marked <see cref="ITransactionalRequest"/> in an <see cref="IUnitOfWork"/>
/// scope: begin → handler → commit on a successful <see cref="Result"/>, rollback on a failed
/// <see cref="Result"/> or a thrown exception (then rethrow). Atomicity per ADR-007.
/// <para>
/// The <c>TRequest : ITransactionalRequest</c> constraint means this behavior is only ever closed
/// for transactional requests — queries (no marker) never enter a transaction.
/// </para>
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalRequest
    where TResponse : Result
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            TResponse response = await next();

            if (response.IsSuccess)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
