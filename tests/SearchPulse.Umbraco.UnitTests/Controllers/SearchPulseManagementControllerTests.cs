using Microsoft.AspNetCore.Mvc;
using SearchPulse.Umbraco.Controllers;
using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Settings;

namespace SearchPulse.Umbraco.UnitTests.Controllers;

public sealed class SearchPulseManagementControllerTests
{
    [Fact]
    public void GetSettingsReturnsTheOnlyPersistedControl()
    {
        var settings = new StubSettingsService(true);
        var controller = CreateController(settings);

        var result = controller.GetSettings();

        var objectResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SearchPulseSettingsResponse>(objectResult.Value);
        Assert.True(response.IsEnabled);
    }

    [Fact]
    public void UpdateSettingsPersistsTheToggleAndReturnsNoContent()
    {
        var settings = new StubSettingsService(false);
        var controller = CreateController(settings);

        var result = controller.UpdateSettings(new SearchPulseSettingsRequest(true));

        Assert.IsType<NoContentResult>(result);
        Assert.True(settings.IsEnabled());
    }

    [Fact]
    public void GetOverviewReturnsSimpleAggregateData()
    {
        var overview = new SearchPulseOverview(
            true,
            new SearchPulseOverviewTotals(24, 9, 17, 12, 8),
            [new SearchPulsePageSummary("/services/seo", 8)],
            [new SearchPulseInteractionSummary("CustomAction", "newsletter-signup", 6)],
            new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc));
        var controller = new SearchPulseManagementController(
            new StubSettingsService(true),
            new StubOverviewService(overview));

        var result = controller.GetOverview();

        var objectResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(overview, objectResult.Value);
    }

    private static SearchPulseManagementController CreateController(StubSettingsService settings) =>
        new(
            settings,
            new StubOverviewService(
                new SearchPulseOverview(
                    settings.IsEnabled(),
                    new SearchPulseOverviewTotals(0, 0, 0, 0, 0),
                    [],
                    [],
                    DateTime.UnixEpoch)));

    private sealed class StubSettingsService(bool enabled) : ISearchPulseSettingsService
    {
        public bool IsEnabled() => enabled;

        public void SetEnabled(bool value) => enabled = value;
    }

    private sealed class StubOverviewService(SearchPulseOverview overview) : ISearchPulseOverviewService
    {
        public SearchPulseOverview GetLastThirtyDays() => overview;
    }
}
