using FinancialMonitor.Api.Data;
using FinancialMonitor.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinancialMonitor.Api.Tests;

public static class TestDbHelper
{
    public static (AppDbContext db, SqliteConnection connection) CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        return (db, connection);
    }

    public static Transaction CreateTransaction(
        string? id = null,
        decimal amount = 100m,
        string currency = "USD",
        TransactionStatus status = TransactionStatus.Pending,
        DateTimeOffset? timestamp = null)
    {
        return new Transaction
        {
            TransactionId = id ?? Guid.NewGuid().ToString(),
            Amount = amount,
            Currency = currency,
            Status = status,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow
        };
    }

    public static TransactionDto CreateDto(
        string? id = null,
        decimal amount = 100m,
        string currency = "USD",
        TransactionStatus status = TransactionStatus.Pending,
        DateTimeOffset? timestamp = null)
    {
        return new TransactionDto
        {
            TransactionId = id ?? Guid.NewGuid().ToString(),
            Amount = amount,
            Currency = currency,
            Status = status,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow
        };
    }
}
