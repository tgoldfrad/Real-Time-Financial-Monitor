using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Storage;

public interface ITransactionStore
{
    bool Add(Transaction transaction);

    IReadOnlyList<Transaction> GetAll();

    Transaction? GetById(string transactionId);
}
