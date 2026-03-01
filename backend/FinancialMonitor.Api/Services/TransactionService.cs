using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Storage;
using Microsoft.AspNetCore.SignalR;

namespace FinancialMonitor.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionStore _store;
    private readonly IHubContext<TransactionHub> _hubContext;

    public TransactionService(ITransactionStore store, IHubContext<TransactionHub> hubContext)
    {
        _store = store;
        _hubContext = hubContext;
    }

    public async Task<Transaction> ProcessTransactionAsync(TransactionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var transaction = dto.ToDomain();

        if (!await _store.AddAsync(transaction))
        {
            throw new InvalidOperationException(
                $"Transaction with ID '{transaction.TransactionId}' already exists.");
        }

        await _hubContext.Clients.All.SendAsync("ReceiveTransaction", TransactionDto.FromDomain(transaction));

        return transaction;
    }
}
