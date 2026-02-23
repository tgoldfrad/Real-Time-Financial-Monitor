using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Services;

/// <summary>
/// Abstraction for transaction processing logic.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Validates the DTO, stores the transaction, and broadcasts it to all connected SignalR clients.
    /// Returns the persisted domain Transaction.
    /// </summary>
    Task<Transaction> ProcessTransactionAsync(TransactionDto dto);
}
