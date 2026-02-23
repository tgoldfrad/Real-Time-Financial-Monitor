using System.ComponentModel.DataAnnotations;

namespace FinancialMonitor.Api.Models;

/// <summary>
/// Data transfer object for incoming/outgoing transaction payloads.
/// Used for API request validation and JSON serialization.
/// </summary>
public sealed class TransactionDto
{
    /// <summary>
    /// Unique identifier (GUID) for the transaction.
    /// Auto-generated if not provided.
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Monetary amount. Must be greater than zero.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code (e.g. USD, EUR, ILS).
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code.")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Transaction status: Pending, Completed, or Failed.
    /// </summary>
    [Required]
    [EnumDataType(typeof(TransactionStatus), ErrorMessage = "Status must be Pending, Completed, or Failed.")]
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// UTC timestamp of the transaction.
    /// Auto-set to now if not provided.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Maps this DTO to a domain Transaction, filling in defaults for optional fields.
    /// </summary>
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

    /// <summary>
    /// Creates a DTO from a domain Transaction.
    /// </summary>
    public static TransactionDto FromDomain(Transaction transaction) => new()
    {
        TransactionId = transaction.TransactionId,
        Amount = transaction.Amount,
        Currency = transaction.Currency,
        Status = transaction.Status,
        Timestamp = transaction.Timestamp
    };
}
