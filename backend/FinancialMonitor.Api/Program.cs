using System.Text.Json.Serialization;
using FinancialMonitor.Api.Data;
using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Middleware;
using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Services;
using FinancialMonitor.Api.Storage;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=financialmonitor.db;Cache=Shared";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<ITransactionStore, SqliteTransactionStore>();

builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var signalRBuilder = builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var redisConnection = builder.Configuration["REDIS_CONNECTION"];
if (!string.IsNullOrEmpty(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix =
            StackExchange.Redis.RedisChannel.Literal("FinancialMonitor");
    });
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var corsOriginsRaw = builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:5173,http://localhost:3000";
var corsOrigins = corsOriginsRaw
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
}

app.UseMiddleware<GlobalErrorHandlingMiddleware>();
app.UseCors("AllowFrontend");

app.MapHealthChecks("/health");
app.MapHub<TransactionHub>("/hubs/transactions");

app.MapPost("/api/transactions", async (
    TransactionDto dto,
    IValidator<TransactionDto> validator,
    ITransactionService service) =>
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
        return Results.BadRequest(new { errors });
    }

    var transaction = await service.ProcessTransactionAsync(dto);
    var result = TransactionDto.FromDomain(transaction);
    return Results.Created($"/api/transactions/{transaction.TransactionId}", result);
});

app.MapGet("/api/transactions", async (ITransactionStore store) =>
{
    var transactions = (await store.GetAllAsync())
        .Select(TransactionDto.FromDomain)
        .ToList();
    return Results.Ok(transactions);
});

app.MapGet("/api/transactions/{id}", async (string id, ITransactionStore store) =>
{
    var transaction = await store.GetByIdAsync(id);
    return transaction is null
        ? Results.NotFound()
        : Results.Ok(TransactionDto.FromDomain(transaction));
});

app.Run();
