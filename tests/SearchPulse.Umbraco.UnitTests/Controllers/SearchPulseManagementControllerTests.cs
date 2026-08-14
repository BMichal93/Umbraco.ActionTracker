using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using SearchPulse.Umbraco.Controllers;
using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.UnitTests.Controllers;

public sealed class SearchPulseManagementControllerTests
{
    [Fact]
    public void GetSettingsReturnsTheTrackingAndQueueState()
    {
        var controller = CreateController(new StubSettingsService(true));

        var result = controller.GetSettings();

        var objectResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SearchPulseSettingsResponse>(objectResult.Value);
        Assert.True(response.IsEnabled);
        Assert.Equal(12, response.PendingEvents);
        Assert.Equal(100_000, response.MaximumQueuedEvents);
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
        var controller = CreateController(new StubSettingsService(true), overviewService);

        var result = controller.GetOverview(7, "name");

        var objectResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(overview, objectResult.Value);
        Assert.Equal((7, SearchPulseOverviewSort.Name), overviewService.LastRequest);
    }

    [Fact]
    public void ClearDataClearsTheSelectedRangeFromSettings()
    {
        var dataManagement = new StubDataManagementService();
        var controller = CreateController(new StubSettingsService(true), dataManagementService: dataManagement);

        var result = controller.ClearData(90);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(90, dataManagement.ClearedRangeDays);
    }

    [Fact]
    public void GetOverviewRejectsUnsupportedRange()
    {
        var controller = CreateController(new StubSettingsService(true));

        var result = controller.GetOverview(14, "count");

        Assert.IsType<BadRequestResult>(result.Result);
    }

    private static SearchPulseManagementController CreateController(
        StubSettingsService settings,
        StubOverviewService? overviewService = null,
        StubDataManagementService? dataManagementService = null) =>
        new(
            settings,
            overviewService ?? new StubOverviewService(CreateOverview()),
            dataManagementService ?? new StubDataManagementService(),
            new StubEventStore(),
            new StubOptionsMonitor());

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

        public SearchPulseOverview GetOverview(int rangeDays, SearchPulseOverviewSort sort)
        {
            LastRequest = (rangeDays, sort);
            return overview;
        }
    }

    private sealed class StubDataManagementService : ISearchPulseDataManagementService
    {
        public int? ClearedRangeDays { get; private set; }

        public void Clear(int rangeDays) => ClearedRangeDays = rangeDays;
    }

    private sealed class StubEventStore : ISearchPulseEventStore
    {
        public Task<SearchPulseEventRecordResult> RecordAsync(
            SearchPulseEvent searchPulseEvent,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SearchPulseEventRecordResult.Accepted);

        public int GetPendingEventCount() => 12;
    }

    private sealed class StubOptionsMonitor : IOptionsMonitor<SearchPulseOptions>
    {
        public SearchPulseOptions CurrentValue { get; } = new();

        public SearchPulseOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<SearchPulseOptions, string?> listener) => null;
    }
}