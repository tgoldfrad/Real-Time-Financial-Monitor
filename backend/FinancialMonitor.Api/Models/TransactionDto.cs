using System.ComponentModel.DataAnnotations;

namespace FinancialMonitor.Api.Models;

public class TransactionDto
{
    public string? TransactionId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code.")]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(TransactionStatus), ErrorMessage = "Status must be Pending, Completed, or Failed.")]
    public TransactionStatus Status { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

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
