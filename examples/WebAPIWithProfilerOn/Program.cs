var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddApplicationInsightsKubernetesEnricher(LogLevel.Information);

var app = builder.Build();

app.MapGet("/", () => new
{
    Text = "Well, 1 more guid wasted :-)",
    Value = Guid.NewGuid().ToString("D"),
});

app.Run();
