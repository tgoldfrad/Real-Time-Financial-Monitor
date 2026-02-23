var builder = WebApplication.CreateBuilder(args);

// Services will be registered here in Step 3+

var app = builder.Build();

// Middleware & endpoints will be configured here in Step 4+

app.Run();
