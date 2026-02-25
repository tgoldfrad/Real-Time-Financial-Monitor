using System.Collections.Concurrent;
using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Storage;

public class InMemoryTransactionStore : ITransactionStore
{
    private readonly ConcurrentDictionary<string, Transaction> _transactions = new();

    public bool Add(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return _transactions.TryAdd(transaction.TransactionId, transaction);
    }

    public IReadOnlyList<Transaction> GetAll()
    {
        return _transactions.Values
            .OrderByDescending(t => t.Timestamp)
            .ToList()
            .AsReadOnly();
    }

    public Transaction? GetById(string transactionId)
    {
        _transactions.TryGetValue(transactionId, out var transaction);
        return transaction;
    }
}
