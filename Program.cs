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

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower().TrimEnd('/') ?? "";
    var method = context.Request.Method;

    bool allowed = method switch
    {
        // Your landing page & exact assets
        "GET" => path is "" or "/"
              || path == "/favicon.png"
              || path == "/_framework/blazor.web.js"
              || path == "/js/animations.js"
              || path == "/js/theme.js"
              || path == "/js/tracker.js"
              || (path.StartsWith("/app.") && path.EndsWith(".css")),

        // Analytics tracker endpoints
        "POST" => path == AnalyticsEndpoints.FullEnrichPath,

        _ => false
    };

    if (!allowed)
    {
        context.Response.StatusCode = 404;
        return; // ~0.01ms of your CPU, done
    }

    await next();
});

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
