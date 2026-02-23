using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Storage;

/// <summary>
/// Abstraction for transaction persistence.
/// </summary>
public interface ITransactionStore
{
    /// <summary>
    /// Adds a transaction to the store.
    /// Returns true if added, false if a transaction with the same ID already exists.
    /// </summary>
    bool Add(Transaction transaction);

    /// <summary>
    /// Returns all stored transactions, ordered by timestamp descending (newest first).
    /// </summary>
    IReadOnlyList<Transaction> GetAll();

    /// <summary>
    /// Returns a transaction by its ID, or null if not found.
    /// </summary>
    Transaction? GetById(string transactionId);
}
