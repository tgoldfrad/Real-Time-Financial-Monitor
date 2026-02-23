using System.Net;
using System.Net.Http.Json;
using FinancialMonitor.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialMonitor.Api.Tests.Controllers;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static TransactionDto CreateValidDto() => new()
    {
        TransactionId = Guid.NewGuid().ToString(),
        Amount = 250.00m,
        Currency = "USD",
        Status = TransactionStatus.Completed,
        Timestamp = DateTimeOffset.UtcNow
    };

    // ── POST /api/transactions ───────────────────────────

    [Fact]
    public async Task Post_ValidPayload_Returns201()
    {
        var dto = CreateValidDto();

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_ValidPayload_ReturnsTransactionInBody()
    {
        var dto = CreateValidDto();

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);
        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();

        result.Should().NotBeNull();
        result!.TransactionId.Should().Be(dto.TransactionId);
        result.Amount.Should().Be(250.00m);
    }

    [Fact]
    public async Task Post_ZeroAmount_Returns400()
    {
        var dto = CreateValidDto();
        dto.Amount = 0;

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NegativeAmount_Returns400()
    {
        var dto = CreateValidDto();
        dto.Amount = -10;

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyCurrency_Returns400()
    {
        var dto = CreateValidDto();
        dto.Currency = "";

        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_DuplicateId_Returns409Conflict()
    {
        var dto = CreateValidDto();

        await _client.PostAsJsonAsync("/api/transactions", dto);
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── GET /api/transactions ────────────────────────────

    [Fact]
    public async Task Get_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_AfterPost_ContainsPostedTransaction()
    {
        var dto = CreateValidDto();
        await _client.PostAsJsonAsync("/api/transactions", dto);

        var response = await _client.GetAsync("/api/transactions");
        var result = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();

        result.Should().Contain(t => t.TransactionId == dto.TransactionId);
    }

    // ── Concurrent POSTs ─────────────────────────────────

    [Fact]
    public async Task ConcurrentPosts_AllValid_AllReturn201()
    {
        const int count = 50;
        var tasks = Enumerable.Range(0, count)
            .Select(_ => _client.PostAsJsonAsync("/api/transactions", CreateValidDto()));

        var responses = await Task.WhenAll(tasks);

        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.Created));
    }
}
