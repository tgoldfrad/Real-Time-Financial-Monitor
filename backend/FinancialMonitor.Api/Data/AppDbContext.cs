using FinancialMonitor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialMonitor.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.TransactionId);

            entity.Property(t => t.TransactionId)
                  .HasMaxLength(64);

            entity.Property(t => t.Amount)
                  .HasColumnType("TEXT");

            entity.Property(t => t.Currency)
                  .HasMaxLength(3)
                  .IsRequired();

            entity.Property(t => t.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(t => t.Timestamp)
                  .IsRequired();
        });
    }
}
