using System.Text.Json.Serialization;
using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Services;
using FinancialMonitor.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────

// Storage — singleton so data lives for the app's lifetime
builder.Services.AddSingleton<ITransactionStore, InMemoryTransactionStore>();

// Business logic — scoped (one per request)
builder.Services.AddScoped<ITransactionService, TransactionService>();

// SignalR for real-time broadcasting
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Controllers + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// CORS — allow the React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────

app.UseCors("AllowFrontend");

app.MapControllers();
app.MapHub<TransactionHub>("/hubs/transactions");

app.Run();

// Make the auto-generated Program class accessible for integration tests
public partial class Program { }
