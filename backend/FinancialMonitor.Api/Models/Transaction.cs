namespace FinancialMonitor.Api.Models;

public class Transaction
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
