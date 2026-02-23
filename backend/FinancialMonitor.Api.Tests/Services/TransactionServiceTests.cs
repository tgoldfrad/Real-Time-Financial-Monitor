using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Services;
using FinancialMonitor.Api.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FinancialMonitor.Api.Tests.Services;

public class TransactionServiceTests
{
    private readonly InMemoryTransactionStore _store = new();
    private readonly Mock<IHubContext<TransactionHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<TransactionHub>>();
        _clientProxyMock = new Mock<IClientProxy>();

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        _service = new TransactionService(_store, _hubContextMock.Object);
    }

    private static TransactionDto CreateValidDto(decimal amount = 150.50m) => new()
    {
        TransactionId = Guid.NewGuid().ToString(),
        Amount = amount,
        Currency = "USD",
        Status = TransactionStatus.Pending,
        Timestamp = DateTimeOffset.UtcNow
    };

    // ── ProcessTransaction ───────────────────────────────

    [Fact]
    public async Task ProcessTransaction_ValidDto_StoresTransaction()
    {
        var dto = CreateValidDto();

        var result = await _service.ProcessTransactionAsync(dto);

        result.Should().NotBeNull();
        _store.GetById(dto.TransactionId!).Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessTransaction_ValidDto_BroadcastsViaSignalR()
    {
        var dto = CreateValidDto();

        await _service.ProcessTransactionAsync(dto);

        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "ReceiveTransaction",
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_ValidDto_ReturnsStoredTransaction()
    {
        var dto = CreateValidDto(amount: 999.99m);

        var result = await _service.ProcessTransactionAsync(dto);

        result.TransactionId.Should().Be(dto.TransactionId);
        result.Amount.Should().Be(999.99m);
        result.Currency.Should().Be("USD");
        result.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task ProcessTransaction_NoId_GeneratesGuid()
    {
        var dto = CreateValidDto();
        dto.TransactionId = null;

        var result = await _service.ProcessTransactionAsync(dto);

        result.TransactionId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(result.TransactionId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessTransaction_NoTimestamp_SetsUtcNow()
    {
        var dto = CreateValidDto();
        dto.Timestamp = null;
        var before = DateTimeOffset.UtcNow;

        var result = await _service.ProcessTransactionAsync(dto);

        result.Timestamp.Should().BeOnOrAfter(before);
        result.Timestamp.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ProcessTransaction_LowercaseCurrency_NormalizesToUpper()
    {
        var dto = CreateValidDto();
        dto.Currency = "eur";

        var result = await _service.ProcessTransactionAsync(dto);

        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task ProcessTransaction_DuplicateId_ThrowsInvalidOperation()
    {
        var dto = CreateValidDto();
        await _service.ProcessTransactionAsync(dto);

        var act = () => _service.ProcessTransactionAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // ── Validation ───────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999.99)]
    public async Task ProcessTransaction_InvalidAmount_ThrowsArgumentException(decimal amount)
    {
        var dto = CreateValidDto(amount: amount);

        var act = () => _service.ProcessTransactionAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Amount must be greater than zero*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ABCD")]
    public async Task ProcessTransaction_InvalidCurrencyLength_ThrowsArgumentException(string currency)
    {
        var dto = CreateValidDto();
        dto.Currency = currency;

        var act = () => _service.ProcessTransactionAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Currency must be a 3-letter ISO code*");
    }

    [Fact]
    public async Task ProcessTransaction_UnsupportedCurrency_ThrowsArgumentException()
    {
        var dto = CreateValidDto();
        dto.Currency = "XYZ";

        var act = () => _service.ProcessTransactionAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not supported*");
    }

    // ── Concurrency ──────────────────────────────────────

    [Fact]
    public async Task ConcurrentProcesses_AllUniqueIds_AllSucceed()
    {
        const int count = 100;
        var dtos = Enumerable.Range(0, count)
            .Select(i => CreateValidDto())
            .ToList();

        var tasks = dtos.Select(dto => _service.ProcessTransactionAsync(dto));
        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(count);
        _store.GetAll().Should().HaveCount(count);
    }
}
