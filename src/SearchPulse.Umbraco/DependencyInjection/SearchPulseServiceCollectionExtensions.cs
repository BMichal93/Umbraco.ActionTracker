using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using SearchPulse.Umbraco.Consent;
using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Retention;
using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.DependencyInjection;

public static class SearchPulseServiceCollectionExtensions
{
    public static IServiceCollection AddSearchPulse(this IServiceCollection services)
    {
        services
            .AddOptions<SearchPulseOptions>()
            .BindConfiguration(SearchPulseOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SearchPulseOptions>, SearchPulseOptionsValidator>();
        services.TryAddSingleton<IAnalyticsConsentProvider, DenyAnalyticsConsentProvider>();
        services.TryAddScoped<ISearchPulseEventStore, SearchPulseEventStore>();
        services.TryAddScoped<ISearchPulseEventQueueProcessor, SearchPulseEventQueueProcessor>();
        services.TryAddScoped<ISearchPulseSettingsService, SearchPulseSettingsService>();
        services.TryAddScoped<ISearchPulseDataManagementService, SearchPulseDataManagementService>();
        services.TryAddScoped<ISearchPulseOverviewService, SearchPulseOverviewService>();
        services.TryAddScoped<ISearchPulseRetentionService, SearchPulseRetentionService>();
        services.AddHostedService<SearchPulseEventQueueHostedService>();
        services.AddHostedService<SearchPulseRetentionHostedService>();

        return services;
    }
}