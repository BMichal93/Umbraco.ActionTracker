using Microsoft.AspNetCore.DataProtection;
using SearchPulse.Umbraco.Consent;
using SearchPulse.Umbraco.Integration;

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

app.MapGet("/searchpulse-review/{**path}", static () => Results.Content("""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>SearchPulse review site</title>
    <style>
        body { margin: 0; background: #f3f5f8; color: #172033; font: 16px/1.6 system-ui, sans-serif; }
        main { max-width: 820px; margin: 48px auto; padding: 32px; background: #fff; border: 1px solid #d9e0ea; border-radius: 12px; }
        h1 { margin-top: 0; } nav, .actions { display: flex; flex-wrap: wrap; gap: 12px; margin: 24px 0; }
        a, button { padding: 9px 14px; border: 1px solid #2b61d1; border-radius: 6px; color: #1749b5; background: #fff; font: inherit; text-decoration: none; cursor: pointer; }
        button.primary { color: #fff; background: #2b61d1; } .status { padding: 12px; border-radius: 6px; background: #fff7d6; }
        .status.active { background: #ddf8e6; color: #14532d; } .spacer { min-height: 85vh; }
    </style>
</head>
<body>
    <main>
        <h1>SearchPulse review site</h1>
        <p>This test-only page lets you generate anonymous signals for the SearchPulse overview.</p>
        <p id="consent-status" class="status"></p>
        <div class="actions"><button id="consent-button" class="primary"></button><button id="action-button">Track newsletter signup</button><a href="https://example.com">External link</a><a href="/searchpulse-review/brochure" download>Download link</a></div>
        <nav><a href="/searchpulse-review">Home</a><a href="/searchpulse-review/products">Products</a><a href="/searchpulse-review/contact">Contact</a></nav>
        <p>Navigate between the pages above, use the action button, and return to the SearchPulse backoffice overview. Scroll down to record the scroll milestones.</p>
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
        document.getElementById("action-button").addEventListener("click", () => window.SearchPulse.trackAction("newsletter-signup"));
    </script>
</body>
</html>
""", "text/html"));

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