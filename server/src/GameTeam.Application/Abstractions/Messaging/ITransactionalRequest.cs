namespace GameTeam.Application.Abstractions.Messaging;

/// <summary>
/// Marker: a request (write command) that must run inside a transaction. <c>TransactionBehavior</c>
/// wraps ONLY requests carrying this marker in an <see cref="Persistence.IUnitOfWork"/> scope.
/// <para>
/// Transactionality is <b>explicit</b> — never inferred from a "Command" name. Queries omit this
/// marker and therefore never enter a command transaction.
/// </para>
/// </summary>
public interface ITransactionalRequest;
