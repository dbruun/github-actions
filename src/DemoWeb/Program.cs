var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => "Hello from Azure Container Apps!");
app.MapHealthChecks("/health");

app.Run();
