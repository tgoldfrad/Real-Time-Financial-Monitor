using System.Text.Json.Serialization;
using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Middleware;
using FinancialMonitor.Api.Services;
using FinancialMonitor.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
builder.WebHost.UseUrls(urls);


builder.Services.AddSingleton<ITransactionStore, InMemoryTransactionStore>();

builder.Services.AddScoped<ITransactionService, TransactionService>();

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var corsOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:5173,http://localhost:3000")
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


app.UseMiddleware<GlobalErrorHandlingMiddleware>();
app.UseCors("AllowFrontend");

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<TransactionHub>("/hubs/transactions");

app.Run();

public partial class Program { }
