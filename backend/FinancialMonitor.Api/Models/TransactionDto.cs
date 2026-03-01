namespace FinancialMonitor.Api.Models;

public record TransactionDto
{
    public string? TransactionId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public TransactionStatus Status { get; init; }

    public DateTimeOffset? Timestamp { get; init; }

    public Transaction ToDomain() => new()
    {
        TransactionId = string.IsNullOrWhiteSpace(TransactionId)
            ? Guid.NewGuid().ToString()
            : TransactionId,
        Amount = Amount,
        Currency = Currency.ToUpperInvariant(),
        Status = Status,
        Timestamp = Timestamp ?? DateTimeOffset.UtcNow
    };

    public static TransactionDto FromDomain(Transaction transaction) => new()
    {
        TransactionId = transaction.TransactionId,
        Amount = transaction.Amount,
        Currency = transaction.Currency,
        Status = transaction.Status,
        Timestamp = transaction.Timestamp
    };
}
