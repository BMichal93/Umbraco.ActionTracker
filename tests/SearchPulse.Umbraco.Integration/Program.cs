using Microsoft.AspNetCore.DataProtection;
using SearchPulse.Umbraco.Consent;
using SearchPulse.Umbraco.Integration;
using SearchPulse.Umbraco.Retention;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Scoping;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "SearchPulseIntegration-DataProtection-Keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddSingleton<IAnalyticsConsentProvider, TestAnalyticsConsentProvider>();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddNotificationAsyncHandler<Umbraco.Cms.Core.Notifications.UmbracoApplicationStartingNotification, ReviewAdministratorSectionAccess>()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.MapPost("/searchpulse-review/consent", static (HttpResponse response) =>
{
    response.Cookies.Append("SearchPulseIntegrationConsent", "yes", new CookieOptions
    {
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = true,
        Path = "/",
    });

    return Results.NoContent();
});

app.MapDelete("/searchpulse-review/consent", static (HttpResponse response) =>
{
    response.Cookies.Delete("SearchPulseIntegrationConsent", new CookieOptions
    {
        Path = "/",
    });

    return Results.NoContent();
});

// These endpoints exist only in the local integration host so browser tests can exercise hosted behavior deterministically.
app.MapPost("/searchpulse-test/stop", static (IHostApplicationLifetime applicationLifetime) =>
{
    applicationLifetime.StopApplication();
    return Results.Accepted();
});
// This endpoint exists only in the local integration host so browser tests can exercise retention deterministically.
app.MapPost("/searchpulse-test/purge", static (ISearchPulseRetentionService retentionService) =>
{
    retentionService.PurgeExpiredEvents();
    return Results.NoContent();
});
// This endpoint is used only by the optional SQL Server multi-node verification script.
app.MapGet("/searchpulse-test/event-count", static (IScopeProvider scopeProvider) =>
{
    using var scope = scopeProvider.CreateScope();
    var count = scope.Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM {SearchPulseEventDto.TableName}");
    scope.Complete();
    return Results.Ok(count);
});
app.MapGet("/searchpulse-test/searchpulse-schema", static (IScopeProvider scopeProvider) =>
{
    using var scope = scopeProvider.CreateScope();
    var contextCount = scope.Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM {SearchPulseEventDto.TableName} WHERE contentKey IS NOT NULL");
    var goalCount = scope.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM searchPulseGoal");
    scope.Complete();
    return Results.Ok(new { ContextEvents = contextCount, GoalRows = goalCount });
});
app.MapGet("/searchpulse-review/{**path}", (string? path) =>
{
    var slug = string.IsNullOrWhiteSpace(path) ? "home" : path.Trim('/').Split('/')[0].ToLowerInvariant();
    var page = slug switch
    {
        "products" => (Title: "Products", ContentKey: "review-products", Introduction: "Explore the products page. Click the CTA, scroll the page, and return to the overview to see this page attributed separately."),
        "contact" => (Title: "Contact", ContentKey: "review-contact", Introduction: "This is the contact page. Use the form-success button to record a meaningful outcome."),
        "resources" => (Title: "Resources", ContentKey: "review-resources", Introduction: "Browse the resources page and try the download link to create an interaction signal."),
        "pricing" => (Title: "Pricing", ContentKey: "review-pricing", Introduction: "Review the pricing page and use the local action to simulate a purchase journey."),
        _ => (Title: "Home", ContentKey: "review-home", Introduction: "Start here, then click through the demo pages to generate page views, exits, scroll depth, and interactions.")
    };

    return Results.Content("""
<!doctype html>
<html lang="en" data-searchpulse-content-key="__CONTENT_KEY__">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>SearchPulse test site - __PAGE_TITLE__</title>
    <style>
        body { margin: 0; background: #f3f5f8; color: #172033; font: 16px/1.6 system-ui, sans-serif; }
        main { max-width: 820px; margin: 48px auto; padding: 32px; background: #fff; border: 1px solid #d9e0ea; border-radius: 12px; }
        h1 { margin-top: 0; } nav, .actions { display: flex; flex-wrap: wrap; gap: 12px; margin: 24px 0; }
        a, button { padding: 9px 14px; border: 1px solid #2b61d1; border-radius: 6px; color: #1749b5; background: #fff; font: inherit; text-decoration: none; cursor: pointer; }
        button.primary { color: #fff; background: #2b61d1; } .status { padding: 12px; border-radius: 6px; background: #fff7d6; }
        .status.active { background: #ddf8e6; color: #14532d; } .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 12px; margin: 24px 0; } .cards a { display: grid; gap: 4px; } .cards span { color: #536174; font-size: 14px; } .spacer { min-height: 85vh; }
    </style>
</head>
<body>
    <main>
        <h1>SearchPulse test site - __PAGE_TITLE__</h1>
        <p>__INTRODUCTION__</p>
        <p id="consent-status" class="status"></p>
        <div class="actions"><button id="consent-button" class="primary"></button><button id="action-button">Track newsletter signup</button><button id="form-success">Track form success</button><button id="site-search">Track site search</button><a href="https://example.com">External link</a><a href="/searchpulse-review/brochure" download>Download link</a></div>
        <nav><a href="/searchpulse-review">Home</a><a href="/searchpulse-review/products">Products</a><a href="/searchpulse-review/contact">Contact</a><a href="/searchpulse-review/resources">Resources</a><a href="/searchpulse-review/pricing">Pricing</a></nav>
        <section class="cards"><a href="/searchpulse-review/products?source=card"><strong>Products</strong><span>Product overview and CTA test.</span></a><a href="/searchpulse-review/contact?source=card"><strong>Contact</strong><span>Form-success test.</span></a><a href="/searchpulse-review/resources?source=card"><strong>Resources</strong><span>Download test.</span></a><a href="/searchpulse-review/pricing?source=card"><strong>Pricing</strong><span>Conversion journey test.</span></a></section><p>Use the controls above, click between pages, and return to the SearchPulse backoffice overview. Scroll down to record the scroll milestones.</p>
        <div class="spacer"></div>
        <p>End of the review page.</p>
    </main>
    <script>
        const hasConsent = document.cookie.split("; ").includes("SearchPulseIntegrationConsent=yes");
        window.SearchPulseConsent = hasConsent;
        const consentButton = document.getElementById("consent-button");
        const consentStatus = document.getElementById("consent-status");
        consentButton.textContent = hasConsent ? "Disable analytics consent" : "Enable analytics consent";
        consentStatus.textContent = hasConsent ? "Analytics consent is enabled. This page records anonymous test signals." : "Analytics consent is disabled. Enable it to record test signals.";
        consentStatus.classList.toggle("active", hasConsent);
        consentButton.addEventListener("click", async () => {
            await fetch("/searchpulse-review/consent", { method: hasConsent ? "DELETE" : "POST", credentials: "same-origin" });
            location.reload();
        });
    </script>
    <script src="/App_Plugins/SearchPulse/searchpulse-tracker.js"></script>
    <script>
        document.getElementById("form-success").addEventListener("click", () => window.SearchPulse.trackFormSuccess("contact"));
        document.getElementById("site-search").addEventListener("click", () => window.SearchPulse.trackSiteSearch("products"));
        document.getElementById("action-button").addEventListener("click", () => window.SearchPulse.trackAction("newsletter-signup"));
    </script>
</body>
</html>
""".Replace("__PAGE_TITLE__", page.Title, StringComparison.Ordinal)
   .Replace("__CONTENT_KEY__", page.ContentKey, StringComparison.Ordinal)
   .Replace("__INTRODUCTION__", page.Introduction, StringComparison.Ordinal), "text/html");
});

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
