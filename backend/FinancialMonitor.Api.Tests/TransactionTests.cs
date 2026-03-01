using FinancialMonitor.Api.Data;
using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Services;
using FinancialMonitor.Api.Storage;
using FinancialMonitor.Api.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinancialMonitor.Api.Tests;

// ════════════════════════════════════════════════════════════════════
// Storage Tests
// ════════════════════════════════════════════════════════════════════

[TestFixture]
public class SqliteTransactionStoreTests
{
    private AppDbContext _db = null!;
    private SqliteConnection _connection = null!;
    private SqliteTransactionStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        (_db, _connection) = TestDbHelper.CreateContext();
        _store = new SqliteTransactionStore(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task Add_PreservesAllFields()
    {
        var timestamp = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var tx = TestDbHelper.CreateTransaction(
            id: "fields-test", amount: 1500.50m, currency: "EUR",
            status: TransactionStatus.Completed, timestamp: timestamp);

        await _store.AddAsync(tx);
        var stored = (await _store.GetByIdAsync("fields-test"))!;

        stored.TransactionId.Should().Be("fields-test");
        stored.Amount.Should().Be(1500.50m);
        stored.Currency.Should().Be("EUR");
        stored.Status.Should().Be(TransactionStatus.Completed);
        stored.Timestamp.Should().Be(timestamp);
    }

    [Test]
    public async Task Add_DuplicateId_ReturnsFalseAndKeepsOriginal()
    {
        await _store.AddAsync(TestDbHelper.CreateTransaction(id: "dup-id", amount: 100m));
        (await _store.AddAsync(TestDbHelper.CreateTransaction(id: "dup-id", amount: 999m))).Should().BeFalse();

        (await _store.GetByIdAsync("dup-id"))!.Amount.Should().Be(100m,
            "original transaction should not be overwritten");
    }

    [Test]
    public async Task GetAll_ReturnsTransactionsOrderedByTimestampDescending()
    {
        await _store.AddAsync(TestDbHelper.CreateTransaction(id: "old", timestamp: DateTimeOffset.UtcNow.AddMinutes(-10)));
        await _store.AddAsync(TestDbHelper.CreateTransaction(id: "mid", timestamp: DateTimeOffset.UtcNow.AddMinutes(-5)));
        await _store.AddAsync(TestDbHelper.CreateTransaction(id: "new", timestamp: DateTimeOffset.UtcNow));

        var result = await _store.GetAllAsync();
        result[0].TransactionId.Should().Be("new");
        result[1].TransactionId.Should().Be("mid");
        result[2].TransactionId.Should().Be("old");
    }

    [Test]
    public async Task GetById_NonExistingId_ReturnsNull()
    {
        (await _store.GetByIdAsync("does-not-exist")).Should().BeNull();
    }
}

// ════════════════════════════════════════════════════════════════════
// Concurrency Tests
// ════════════════════════════════════════════════════════════════════

[TestFixture]
public class ConcurrencyTests
{
    private string _dbPath = null!;
    private string _connectionString = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";

        using var setupDb = CreateContext();
        setupDb.Database.EnsureCreated();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        if (File.Exists(_dbPath + "-wal")) File.Delete(_dbPath + "-wal");
        if (File.Exists(_dbPath + "-shm")) File.Delete(_dbPath + "-shm");
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Test]
    public async Task ConcurrentAdd_SameId_OnlyOneSucceeds()
    {
        const int threadCount = 10;
        var results = new bool[threadCount];

        await Parallel.ForAsync(0, threadCount, async (i, _) =>
        {
            using var db = CreateContext();
            var store = new SqliteTransactionStore(db);
            results[i] = await store.AddAsync(TestDbHelper.CreateTransaction(id: "concurrent-dup", amount: i));
        });

        results.Count(r => r).Should().Be(1, "exactly one thread should succeed");
    }

    [Test]
    public async Task ConcurrentAdd_UniqueIds_AllSucceed()
    {
        const int threadCount = 50;
        var results = new bool[threadCount];

        await Parallel.ForAsync(0, threadCount, async (i, _) =>
        {
            using var db = CreateContext();
            var store = new SqliteTransactionStore(db);
            results[i] = await store.AddAsync(TestDbHelper.CreateTransaction(id: $"unique-{i}"));
        });

        results.Should().AllSatisfy(r => r.Should().BeTrue());

        using var verifyDb = CreateContext();
        verifyDb.Transactions.Count().Should().Be(threadCount);
    }

    [Test]
    public async Task ConcurrentReadWrite_NoExceptions()
    {
        using (var db = CreateContext())
        {
            var store = new SqliteTransactionStore(db);
            for (int i = 0; i < 20; i++)
                await store.AddAsync(TestDbHelper.CreateTransaction(id: $"pre-{i}"));
        }

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 30; i++)
        {
            var idx = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var db = CreateContext();
                    var store = new SqliteTransactionStore(db);
                    await store.AddAsync(TestDbHelper.CreateTransaction(id: $"write-{idx}"));
                }
                catch (Exception ex) { exceptions.Add(ex); }
            }));
        }

        for (int i = 0; i < 30; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var db = CreateContext();
                    var store = new SqliteTransactionStore(db);
                    await store.GetAllAsync();
                }
                catch (Exception ex) { exceptions.Add(ex); }
            }));
        }

        await Task.WhenAll(tasks);
        exceptions.Should().BeEmpty("concurrent reads and writes should not throw");
    }
}

// ════════════════════════════════════════════════════════════════════
// Service Tests (Transaction Processing)
// ════════════════════════════════════════════════════════════════════

[TestFixture]
public class TransactionServiceTests
{
    private Mock<ITransactionStore> _storeMock = null!;
    private Mock<IHubContext<TransactionHub>> _hubMock = null!;
    private Mock<IClientProxy> _clientProxy = null!;
    private TransactionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _storeMock = new Mock<ITransactionStore>();
        _hubMock = new Mock<IHubContext<TransactionHub>>();
        _clientProxy = new Mock<IClientProxy>();

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.All).Returns(_clientProxy.Object);
        _hubMock.Setup(h => h.Clients).Returns(hubClients.Object);

        _service = new TransactionService(_storeMock.Object, _hubMock.Object);
    }

    [Test]
    public async Task ProcessTransaction_ValidDto_StoresAndBroadcasts()
    {
        var dto = TestDbHelper.CreateDto(id: "svc-test", amount: 250m);
        _storeMock.Setup(s => s.AddAsync(It.IsAny<Transaction>())).ReturnsAsync(true);

        var result = await _service.ProcessTransactionAsync(dto);

        result.Should().NotBeNull();
        result.Amount.Should().Be(250m);
        _storeMock.Verify(s => s.AddAsync(It.IsAny<Transaction>()), Times.Once);
        _clientProxy.Verify(
            c => c.SendCoreAsync("ReceiveTransaction",
                It.Is<object?[]>(args => args.Length == 1), default),
            Times.Once);
    }

    [Test]
    public async Task ProcessTransaction_DuplicateId_ThrowsAndDoesNotBroadcast()
    {
        _storeMock.Setup(s => s.AddAsync(It.IsAny<Transaction>())).ReturnsAsync(false);

        var act = () => _service.ProcessTransactionAsync(TestDbHelper.CreateDto(id: "dup"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
        _clientProxy.Verify(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Test]
    public async Task ProcessTransaction_MapsFieldsCorrectly()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var dto = TestDbHelper.CreateDto(
            id: "map-test", amount: 1500.50m, currency: "eur",
            status: TransactionStatus.Completed, timestamp: timestamp);

        Transaction? captured = null;
        _storeMock.Setup(s => s.AddAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => captured = t).ReturnsAsync(true);

        await _service.ProcessTransactionAsync(dto);

        captured.Should().NotBeNull();
        captured!.TransactionId.Should().Be("map-test");
        captured.Amount.Should().Be(1500.50m);
        captured.Currency.Should().Be("EUR", "currency should be uppercased via ToDomain");
        captured.Status.Should().Be(TransactionStatus.Completed);
        captured.Timestamp.Should().Be(timestamp);
    }

    [Test]
    public async Task ProcessTransaction_MissingId_GeneratesGuid()
    {
        var dto = new TransactionDto
        {
            Amount = 100m, Currency = "USD",
            Status = TransactionStatus.Pending, Timestamp = DateTimeOffset.UtcNow
        };

        Transaction? captured = null;
        _storeMock.Setup(s => s.AddAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => captured = t).ReturnsAsync(true);

        await _service.ProcessTransactionAsync(dto);

        captured!.TransactionId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(captured.TransactionId, out _).Should().BeTrue();
    }
}

// ════════════════════════════════════════════════════════════════════
// Validator Tests
// ════════════════════════════════════════════════════════════════════

[TestFixture]
public class TransactionDtoValidatorTests
{
    private TransactionDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new TransactionDtoValidator();

    [Test]
    public void Validate_ValidDto_Passes()
    {
        var dto = new TransactionDto
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 100m, Currency = "USD",
            Status = TransactionStatus.Pending, Timestamp = DateTimeOffset.UtcNow
        };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NegativeAmount_Fails()
    {
        var dto = new TransactionDto { Amount = -50m, Currency = "USD", Status = TransactionStatus.Pending };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void Validate_UnsupportedCurrency_Fails()
    {
        var dto = new TransactionDto { Amount = 100m, Currency = "XYZ", Status = TransactionStatus.Pending };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Test]
    public void Validate_InvalidStatusValue_Fails()
    {
        var dto = new TransactionDto { Amount = 100m, Currency = "USD", Status = (TransactionStatus)999 };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Status);
    }
}
