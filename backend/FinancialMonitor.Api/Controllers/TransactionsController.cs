using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Services;
using FinancialMonitor.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace FinancialMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;
    private readonly ITransactionStore _store;

    public TransactionsController(ITransactionService service, ITransactionStore store)
    {
        _service = service;
        _store = store;
    }

    /// <summary>
    /// POST /api/transactions — ingest a new transaction.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var transaction = await _service.ProcessTransactionAsync(dto);
        var result = TransactionDto.FromDomain(transaction);
        return CreatedAtAction(nameof(GetById), new { id = transaction.TransactionId }, result);
    }

    /// <summary>
    /// GET /api/transactions — returns all stored transactions (newest first).
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var transactions = _store.GetAll()
            .Select(TransactionDto.FromDomain)
            .ToList();

        return Ok(transactions);
    }

    /// <summary>
    /// GET /api/transactions/{id} — returns a single transaction by ID.
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var transaction = _store.GetById(id);
        if (transaction is null)
            return NotFound();

        return Ok(TransactionDto.FromDomain(transaction));
    }
}
