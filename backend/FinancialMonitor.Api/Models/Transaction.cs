namespace FinancialMonitor.Api.Models;

/// <summary>
/// Domain model representing a financial transaction.
/// </summary>
public sealed class Transaction
{
    public required string TransactionId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required TransactionStatus Status { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
