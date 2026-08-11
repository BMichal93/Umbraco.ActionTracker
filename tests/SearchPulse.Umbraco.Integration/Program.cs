
using SearchPulse.Umbraco.Consent;
using SearchPulse.Umbraco.Integration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IAnalyticsConsentProvider, TestAnalyticsConsentProvider>();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
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
