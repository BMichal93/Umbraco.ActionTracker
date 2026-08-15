using System.Text;
using Microsoft.AspNetCore.DataProtection;
using SearchPulse.DemoSite;
using SearchPulse.Umbraco.Consent;
using Umbraco.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
var dataProtectionKeysPath = builder.Configuration["SearchPulseDemo:DataDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "SearchPulseDemo-DataProtection-Keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddSingleton<IAnalyticsConsentProvider, DemoAnalyticsConsentProvider>();

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddNotificationAsyncHandler<Umbraco.Cms.Core.Notifications.UmbracoApplicationStartingNotification, DemoAdministratorSectionAccess>();
umbracoBuilder.PackageMigrationPlans().Add<DemoContentSeedPlan>();
umbracoBuilder.Build();

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
