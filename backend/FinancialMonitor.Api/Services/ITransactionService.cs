using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Services;

public interface ITransactionService
{
    Task<Transaction> ProcessTransactionAsync(TransactionDto dto);
}
