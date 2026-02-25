using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Storage;
using FluentAssertions;

namespace FinancialMonitor.Api.Tests.Storage;

public class InMemoryTransactionStoreTests
{
    private readonly InMemoryTransactionStore _store = new();

    private static Transaction CreateTransaction(string? id = null, decimal amount = 100m, DateTimeOffset? timestamp = null) => new()
    {
        TransactionId = id ?? Guid.NewGuid().ToString(),
        Amount = amount,
        Currency = "USD",
        Status = TransactionStatus.Pending,
        Timestamp = timestamp ?? DateTimeOffset.UtcNow
    };


    [Fact]
    public void Add_ValidTransaction_ReturnsTrue()
    {
        var tx = CreateTransaction();

        var result = _store.Add(tx);

        result.Should().BeTrue();
    }

    [Fact]
    public void Add_DuplicateId_ReturnsFalse()
    {
        var tx = CreateTransaction(id: "dup-id");
        _store.Add(tx);

        var duplicate = CreateTransaction(id: "dup-id");
        var result = _store.Add(duplicate);

        result.Should().BeFalse();
    }

    [Fact]
    public void Add_NullTransaction_ThrowsArgumentNullException()
    {
        var act = () => _store.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }


    [Fact]
    public void GetAll_EmptyStore_ReturnsEmptyList()
    {
        var result = _store.GetAll();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAll_AfterAdds_ReturnsAllStoredTransactions()
    {
        var tx1 = CreateTransaction();
        var tx2 = CreateTransaction();
        _store.Add(tx1);
        _store.Add(tx2);

        var result = _store.GetAll();

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.TransactionId == tx1.TransactionId);
        result.Should().Contain(t => t.TransactionId == tx2.TransactionId);
    }

    [Fact]
    public void GetAll_ReturnsOrderedByTimestampDescending()
    {
        var older = CreateTransaction(timestamp: DateTimeOffset.UtcNow.AddMinutes(-5));
        var newer = CreateTransaction(timestamp: DateTimeOffset.UtcNow);
        _store.Add(older);
        _store.Add(newer);

        var result = _store.GetAll();

        result.First().TransactionId.Should().Be(newer.TransactionId);
        result.Last().TransactionId.Should().Be(older.TransactionId);
    }


    [Fact]
    public void GetById_ExistingId_ReturnsTransaction()
    {
        var tx = CreateTransaction(id: "find-me");
        _store.Add(tx);

        var result = _store.GetById("find-me");

        result.Should().NotBeNull();
        result!.TransactionId.Should().Be("find-me");
        result.Amount.Should().Be(tx.Amount);
    }

    [Fact]
    public void GetById_NonExistingId_ReturnsNull()
    {
        var result = _store.GetById("does-not-exist");

        result.Should().BeNull();
    }


    [Fact]
    public async Task ConcurrentAdds_AllUniqueIds_AllSucceed()
    {
        const int count = 200;
        var transactions = Enumerable.Range(0, count)
            .Select(i => CreateTransaction(id: $"concurrent-{i}"))
            .ToList();

        var tasks = transactions.Select(tx => Task.Run(() => _store.Add(tx)));
        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.Should().BeTrue());
        _store.GetAll().Should().HaveCount(count);
    }

    [Fact]
    public async Task ConcurrentAdds_DuplicateIds_OnlyOneSucceeds()
    {
        const int count = 50;
        var tasks = Enumerable.Range(0, count)
            .Select(_ => Task.Run(() => _store.Add(CreateTransaction(id: "same-id"))));

        var results = await Task.WhenAll(tasks);

        results.Count(r => r).Should().Be(1, "only the first add of a duplicate ID should succeed");
        results.Count(r => !r).Should().Be(count - 1);
        _store.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_NoExceptions()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var writeTask = Task.Run(async () =>
        {
            for (int i = 0; !cts.Token.IsCancellationRequested; i++)
            {
                _store.Add(CreateTransaction(id: $"rw-{i}"));
                await Task.Delay(1, cts.Token).ConfigureAwait(false);
            }
        }, cts.Token);

        var readTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                _ = _store.GetAll();
                _ = _store.GetById("rw-0");
                await Task.Delay(1, cts.Token).ConfigureAwait(false);
            }
        }, cts.Token);

        Func<Task> act = async () =>
        {
            try { await Task.WhenAll(writeTask, readTask); }
            catch (OperationCanceledException) { /* expected */ }
        };

        await act.Should().NotThrowAsync("concurrent reads and writes must be thread-safe");
    }
}
