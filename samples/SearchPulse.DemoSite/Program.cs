using System.Text;
using Microsoft.AspNetCore.DataProtection;
using SearchPulse.DemoSite;
using SearchPulse.Umbraco.Consent;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
var dataProtectionKeysPath = builder.Configuration["SearchPulseDemo:DataDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "SearchPulseDemo-DataProtection-Keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddSingleton<IAnalyticsConsentProvider, DemoAnalyticsConsentProvider>();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddNotificationAsyncHandler<Umbraco.Cms.Core.Notifications.UmbracoApplicationStartingNotification, DemoAdministratorSectionAccess>()
    .Build();

WebApplication app = builder.Build();
await app.BootUmbracoAsync();

app.MapPost("/demo/consent", static (HttpResponse response) =>
{
    response.Cookies.Append("SearchPulseDemoConsent", "yes", new CookieOptions
    {
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = true,
        Path = "/",
    });
    return Results.NoContent();
});

app.MapDelete("/demo/consent", static (HttpResponse response) =>
{
    response.Cookies.Delete("SearchPulseDemoConsent", new CookieOptions { Path = "/" });
    return Results.NoContent();
});

app.MapGet("/downloads/searchpulse-demo-guide.txt", static () => Results.File(
    Encoding.UTF8.GetBytes("SearchPulse demo guide\n\nThis local download demonstrates anonymous download tracking."),
    "text/plain",
    "searchpulse-demo-guide.txt"));

app.MapGet("/", static () => Results.Content(RenderPage(new DemoPage(
    "Home",
    "SearchPulse demo home",
    "A small Umbraco site for testing anonymous engagement signals without collecting visitor identity.",
    "book-consultation",
    "Book a consultation",
    "Use the navigation, choose an action, open the external resource, download the guide, then scroll to the end of a page.")), "text/html"));
app.MapGet("/services", static () => Results.Content(RenderPage(new DemoPage(
    "Services",
    "Services and SEO planning",
    "This page represents a service page where a visitor can request a pricing conversation.",
    "request-pricing",
    "Request pricing",
    "The action is a fixed anonymous label. No form values, email addresses, or visitor identifiers are sent to SearchPulse.")), "text/html"));
app.MapGet("/contact", static () => Results.Content(RenderPage(new DemoPage(
    "Contact",
    "Contact and newsletter",
    "This page models a marketing conversion without adding a form or capturing personal data.",
    "newsletter-signup",
    "Track newsletter interest",
    "The button records only the newsletter-signup action. A real site should send form submissions to its own consent-aware form handler.")), "text/html"));

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

static string RenderPage(DemoPage page)
{
    var content = string.Join(Environment.NewLine, Enumerable.Repeat(
        "<p>Scroll through this deliberately long demo content to record the 25%, 50%, and 75% reading milestones. SearchPulse records the milestone once per page view after the existing consent decision allows tracking.</p>",
        12));
    var navigation = string.Join(string.Empty, new[]
    {
        ("/", "Home"),
        ("/services", "Services"),
        ("/contact", "Contact"),
    }.Select(item => $"<a class=\"nav-link{(item.Item2 == page.Name ? " active" : string.Empty)}\" href=\"{item.Item1}\">{item.Item2}</a>"));

    return $$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{page.Name}} - SearchPulse demo</title>
    <style>
        :root { color-scheme: light; font-family: Inter, ui-sans-serif, system-ui, sans-serif; color: #162033; background: #f4f7fb; }
        body { margin: 0; }
        header { background: #162a5c; color: #fff; }
        .shell { width: min(100% - 40px, 980px); margin: 0 auto; }
        .brand { display: flex; justify-content: space-between; align-items: center; gap: 24px; padding: 22px 0; }
        .brand strong { font-size: 20px; }
        .nav { display: flex; flex-wrap: wrap; gap: 8px; padding-bottom: 18px; }
        .nav-link { color: #dce6ff; text-decoration: none; padding: 8px 12px; border-radius: 6px; }
        .nav-link:hover, .nav-link.active { background: #314b91; color: #fff; }
        main { margin: 36px auto; padding: 36px; border: 1px solid #dbe3ef; border-radius: 14px; background: #fff; box-shadow: 0 12px 36px rgb(20 35 70 / 8%); }
        h1 { margin-top: 0; font-size: clamp(30px, 5vw, 44px); letter-spacing: -.03em; }
        .lead { color: #53627d; font-size: 19px; }
        .consent { display: flex; justify-content: space-between; align-items: center; gap: 16px; margin: 26px 0; padding: 16px; border-radius: 10px; background: #f0f5ff; }
        .status { margin: 0; font-weight: 700; }
        .status.on { color: #18733f; }
        .actions { display: flex; flex-wrap: wrap; gap: 12px; margin: 28px 0; }
        button, .button-link { border: 1px solid #234ba4; border-radius: 7px; padding: 10px 14px; color: #173d91; background: #fff; font: inherit; font-weight: 700; text-decoration: none; cursor: pointer; }
        button.primary { color: #fff; background: #234ba4; }
        .signal-list { margin: 32px 0; padding: 18px 22px; border-left: 4px solid #7da0ff; background: #f7f9fd; }
        .long-content { margin-top: 38px; }
        footer { padding: 24px 0 56px; color: #65738a; font-size: 14px; }
        @media (max-width: 620px) { .shell { width: min(100% - 28px, 980px); } main { padding: 24px; } .brand, .consent { align-items: flex-start; flex-direction: column; } }
    </style>
</head>
<body>
    <header>
        <div class="shell">
            <div class="brand"><strong>SearchPulse demo</strong><span>Anonymous Umbraco engagement signals</span></div>
            <nav class="nav" aria-label="Primary navigation">{{navigation}}</nav>
        </div>
    </header>
    <main class="shell">
        <h1>{{page.Heading}}</h1>
        <p class="lead">{{page.Introduction}}</p>
        <section class="consent" aria-label="Analytics consent demo">
            <div><p id="consent-status" class="status"></p><small>Tracking is disabled until this local demo consent is granted.</small></div>
            <button id="consent-button" type="button"></button>
        </section>
        <div class="actions" aria-label="Tracked actions">
            <button class="primary" type="button" data-searchpulse-action="{{page.ActionName}}">{{page.ActionLabel}}</button>
            <a class="button-link" href="https://umbraco.com" target="_blank" rel="noopener">Open Umbraco.com</a>
            <a class="button-link" href="/downloads/searchpulse-demo-guide.txt" download>Download the demo guide</a>
        </div>
        <section class="signal-list">
            <strong>What this page demonstrates</strong><p>{{page.Detail}}</p>
            <ul>
                <li>Page view when consent is granted.</li>
                <li>Page exit when navigating away or closing the tab.</li>
                <li>Scroll milestones at 25%, 50%, and 75%.</li>
                <li>External-link click, download click, and the named <code>{{page.ActionName}}</code> local action.</li>
            </ul>
        </section>
        <section class="long-content"><h2>Reading-depth test area</h2>{{content}}</section>
    </main>
    <footer class="shell">Open <a href="/umbraco">Umbraco backoffice</a>, then select SearchPulse to review the recorded signals.</footer>
    <script>
        const hasConsent = document.cookie.split("; ").includes("SearchPulseDemoConsent=yes");
        window.SearchPulseConsent = hasConsent;
    </script>
    <script src="/App_Plugins/SearchPulse/searchpulse-tracker.js"></script>
    <script>
        const consentButton = document.getElementById("consent-button");
        const consentStatus = document.getElementById("consent-status");
        consentButton.textContent = hasConsent ? "Disable demo consent" : "Enable demo consent";
        consentStatus.textContent = hasConsent ? "Analytics consent is enabled. SearchPulse is tracking anonymous signals." : "Analytics consent is disabled.";
        consentStatus.classList.toggle("on", hasConsent);
        consentButton.addEventListener("click", async () => {
            await fetch("/demo/consent", { method: hasConsent ? "DELETE" : "POST", credentials: "same-origin" });
            location.reload();
        });
        document.querySelectorAll("[data-searchpulse-action]").forEach(element => {
            element.addEventListener("click", () => window.SearchPulse.trackAction(element.dataset.searchpulseAction));
        });
        if (hasConsent) {
            window.SearchPulse.start();
        }
    </script>
</body>
</html>
""";
}

internal sealed record DemoPage(
    string Name,
    string Heading,
    string Introduction,
    string ActionName,
    string ActionLabel,
    string Detail);