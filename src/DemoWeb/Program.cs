var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", DemoWeb.HomeEndpoint.GetGreeting);
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
