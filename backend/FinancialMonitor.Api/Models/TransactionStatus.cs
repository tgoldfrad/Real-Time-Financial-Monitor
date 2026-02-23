using System.Text.Json.Serialization;

namespace FinancialMonitor.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionStatus
{
    Pending,
    Completed,
    Failed
}
