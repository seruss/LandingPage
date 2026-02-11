using LandingPage.Analytics;
using LandingPage.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();

// SurrealDB — uses connection string from appsettings (Production overrides via appsettings.Production.json)
var surrealConnectionString = builder.Configuration.GetConnectionString("SurrealDB");
if (!string.IsNullOrWhiteSpace(surrealConnectionString))
{
    builder.Services.AddSurreal(surrealConnectionString);
}

// Analytics event buffer (background service with rate limiting)
builder.Services.AddSingleton<EventBuffer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EventBuffer>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Analytics middleware — captures server-side request data
app.UseAnalytics();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

// Analytics API endpoints
app.MapAnalyticsEndpoints();

app.Run();
