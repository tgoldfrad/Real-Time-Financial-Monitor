using FinancialMonitor.Api.Data;
using FinancialMonitor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialMonitor.Api.Storage;

public class SqliteTransactionStore : ITransactionStore
{
    private readonly AppDbContext _db;

    public SqliteTransactionStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> AddAsync(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        try
        {
            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
        catch (InvalidOperationException)
        {
            // ChangeTracker already tracks an entity with the same PK
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync()
    {
        var list = await _db.Transactions
            .AsNoTracking()
            .ToListAsync();

        return list
            .OrderByDescending(t => t.Timestamp)
            .ToList()
            .AsReadOnly();
    }

    public async Task<Transaction?> GetByIdAsync(string transactionId)
    {
        return await _db.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
    }
}
