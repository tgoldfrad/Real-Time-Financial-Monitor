namespace FinancialMonitor.Api.Models;

public record Transaction
{
    public string TransactionId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public TransactionStatus Status { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
