using System.Text.Json.Serialization;
using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Middleware;
using FinancialMonitor.Api.Services;
using FinancialMonitor.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// ── Kestrel ──────────────────────────────────────────────
// Use ASPNETCORE_URLS env var in production; fall back to localhost:5000 for dev
var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
builder.WebHost.UseUrls(urls);

// ── Services ────────────────────────────────────────────

// Storage — singleton so data lives for the app's lifetime
builder.Services.AddSingleton<ITransactionStore, InMemoryTransactionStore>();

// Business logic — scoped (one per request)
builder.Services.AddScoped<ITransactionService, TransactionService>();

// SignalR for real-time broadcasting
var signalRBuilder = builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ── Redis Backplane (Cloud-Native) ──────────────────────
// When REDIS_CONNECTION is set, SignalR uses a Redis backplane so that
// broadcasts are shared across all pods / replicas.
var redisConnection = builder.Configuration["REDIS_CONNECTION"];
if (!string.IsNullOrEmpty(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix =
            StackExchange.Redis.RedisChannel.Literal("FinancialMonitor");
    });
}

// Controllers + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// CORS — configurable via CORS_ORIGINS env var; defaults to dev servers
var corsOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:5173,http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

// Health checks (for K8s readiness / liveness probes)
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────

app.UseMiddleware<GlobalErrorHandlingMiddleware>();
app.UseCors("AllowFrontend");

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<TransactionHub>("/hubs/transactions");

app.Run();

// Make the auto-generated Program class accessible for integration tests
public partial class Program { }
