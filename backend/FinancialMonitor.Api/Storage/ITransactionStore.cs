using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Storage;

public interface ITransactionStore
{
    Task<bool> AddAsync(Transaction transaction);

    Task<IReadOnlyList<Transaction>> GetAllAsync();

    Task<Transaction?> GetByIdAsync(string transactionId);
}
