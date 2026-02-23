using System.Collections.Concurrent;
using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Storage;

/// <summary>
/// Thread-safe in-memory transaction store backed by ConcurrentDictionary.
/// Registered as a Singleton so the data lives for the app's lifetime.
/// </summary>
public sealed class InMemoryTransactionStore : ITransactionStore
{
    private readonly ConcurrentDictionary<string, Transaction> _transactions = new();

    /// <inheritdoc />
    public bool Add(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return _transactions.TryAdd(transaction.TransactionId, transaction);
    }

    /// <inheritdoc />
    public IReadOnlyList<Transaction> GetAll()
    {
        return _transactions.Values
            .OrderByDescending(t => t.Timestamp)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public Transaction? GetById(string transactionId)
    {
        _transactions.TryGetValue(transactionId, out var transaction);
        return transaction;
    }
}
