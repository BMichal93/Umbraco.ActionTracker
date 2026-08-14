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
    public void GetOverviewReturnsTheSelectedReport()
    {
        var overview = CreateOverview();
        var overviewService = new StubOverviewService(overview);
        var controller = new SearchPulseManagementController(
            new StubSettingsService(true),
            overviewService);

        var result = controller.GetOverview(7, "name");

        var objectResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(overview, objectResult.Value);
        Assert.Equal((7, SearchPulseOverviewSort.Name), overviewService.LastRequest);
    }

    [Fact]
    public void ClearOverviewClearsTheSelectedRange()
    {
        var overviewService = new StubOverviewService(CreateOverview());
        var controller = new SearchPulseManagementController(
            new StubSettingsService(true),
            overviewService);

        var result = controller.ClearOverview(90);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(90, overviewService.ClearedRangeDays);
    }

    [Fact]
    public void GetOverviewRejectsUnsupportedRange()
    {
        var controller = CreateController(new StubSettingsService(true));

        var result = controller.GetOverview(14, "count");

        Assert.IsType<BadRequestResult>(result.Result);
    }

    private static SearchPulseManagementController CreateController(StubSettingsService settings) =>
        new(settings, new StubOverviewService(CreateOverview()));

    private static SearchPulseOverview CreateOverview() =>
        new(
            true,
            30,
            "Last 30 days",
            new SearchPulseOverviewTotals(24, 9, 17, 12, 8),
            [new SearchPulsePageSummary("/services/seo", 8)],
            [new SearchPulseInteractionSummary("CustomAction", "newsletter-signup", 6)],
            new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc));

    private sealed class StubSettingsService(bool enabled) : ISearchPulseSettingsService
    {
        public bool IsEnabled() => enabled;

        public void SetEnabled(bool value) => enabled = value;
    }

    private sealed class StubOverviewService(SearchPulseOverview overview) : ISearchPulseOverviewService
    {
        public (int RangeDays, SearchPulseOverviewSort Sort)? LastRequest { get; private set; }

        public int? ClearedRangeDays { get; private set; }

        public SearchPulseOverview GetOverview(int rangeDays, SearchPulseOverviewSort sort)
        {
            LastRequest = (rangeDays, sort);
            return overview;
        }

        public void Clear(int rangeDays) => ClearedRangeDays = rangeDays;
    }
}