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

    private static readonly HashSet<string> ValidCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "ILS", "GBP", "JPY", "CHF", "CAD", "AUD"
    };

    public async Task<Transaction> ProcessTransactionAsync(TransactionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Currency) || dto.Currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(dto));

        if (!ValidCurrencies.Contains(dto.Currency))
            throw new ArgumentException($"Currency '{dto.Currency}' is not supported.", nameof(dto));

        if (!Enum.IsDefined(dto.Status))
            throw new ArgumentException($"Invalid transaction status: '{dto.Status}'.", nameof(dto));

        var transaction = dto.ToDomain();

        if (!_store.Add(transaction))
        {
            throw new InvalidOperationException(
                $"Transaction with ID '{transaction.TransactionId}' already exists.");
        }

        await _hubContext.Clients.All.SendAsync("ReceiveTransaction", TransactionDto.FromDomain(transaction));

        return transaction;
    }
}
