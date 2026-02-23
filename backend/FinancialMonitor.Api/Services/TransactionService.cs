using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Storage;
using Microsoft.AspNetCore.SignalR;

namespace FinancialMonitor.Api.Services;

/// <summary>
/// Processes incoming transactions: maps DTO → domain, stores, and broadcasts via SignalR.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionStore _store;
    private readonly IHubContext<TransactionHub> _hubContext;

    public TransactionService(ITransactionStore store, IHubContext<TransactionHub> hubContext)
    {
        _store = store;
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task<Transaction> ProcessTransactionAsync(TransactionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var transaction = dto.ToDomain();

        if (!_store.Add(transaction))
        {
            throw new InvalidOperationException(
                $"Transaction with ID '{transaction.TransactionId}' already exists.");
        }

        // Broadcast to all connected SignalR clients
        await _hubContext.Clients.All.SendAsync("ReceiveTransaction", TransactionDto.FromDomain(transaction));

        return transaction;
    }
}
