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
