using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchPulse.Umbraco.Consent;
using SearchPulse.Umbraco.DependencyInjection;

namespace SearchPulse.Umbraco.UnitTests.Consent;

public sealed class DefaultConsentTests
{
    [Fact]
    public async Task AddSearchPulseDeniesClientTrackingUntilTheHostRegistersAProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSearchPulse();

        await using var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IAnalyticsConsentProvider>();

        var hasConsent = await provider.HasAnalyticsConsentAsync(new DefaultHttpContext());

        Assert.False(hasConsent);
    }
}
