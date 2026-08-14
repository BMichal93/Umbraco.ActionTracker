using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.UnitTests.Overview;

public sealed class SearchPulseOverviewServiceTests
{
    [Fact]
    public void BuildPopularInteractionsFiltersUnsupportedSignalsSortsAndCapsAtFive()
    {
        var interactions = new[]
        {
            Count(SearchPulseEventType.PageView, "ignored", 20),
            Count(SearchPulseEventType.CustomAction, null, 19),
            Count(SearchPulseEventType.CustomAction, "newsletter-signup", 9),
            Count(SearchPulseEventType.ExternalLinkClick, "example.com", 8),
            Count(SearchPulseEventType.DownloadClick, "download", 7),
            Count(SearchPulseEventType.CustomAction, "book-demo", 6),
            Count(SearchPulseEventType.ExternalLinkClick, "partner.example", 5),
            Count(SearchPulseEventType.DownloadClick, "brochure", 4),
            Count(SearchPulseEventType.CustomAction, "contact", 3),
        };

        var popular = SearchPulseOverviewService.BuildPopularInteractions(interactions);

        Assert.Collection(
            popular,
            item => Assert.Equal(("CustomAction", "newsletter-signup", 9), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("ExternalLinkClick", "example.com", 8), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("DownloadClick", "download", 7), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("CustomAction", "book-demo", 6), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("ExternalLinkClick", "partner.example", 5), (item.EventType, item.Target, item.Interactions)));
    }

    [Fact]
    public void BuildPopularInteractionsCanSortByName()
    {
        var interactions = new[]
        {
            Count(SearchPulseEventType.DownloadClick, "zebra", 1),
            Count(SearchPulseEventType.CustomAction, "beta", 9),
            Count(SearchPulseEventType.CustomAction, "alpha", 2),
        };

        var popular = SearchPulseOverviewService.BuildPopularInteractions(interactions, SearchPulseOverviewSort.Name);

        Assert.Collection(
            popular,
            item => Assert.Equal(("CustomAction", "alpha"), (item.EventType, item.Target)),
            item => Assert.Equal(("CustomAction", "beta"), (item.EventType, item.Target)),
            item => Assert.Equal(("DownloadClick", "zebra"), (item.EventType, item.Target)));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(30, true)]
    [InlineData(90, true)]
    [InlineData(14, false)]
    public void IsSupportedRangeOnlyAcceptsTheDashboardRanges(int rangeDays, bool expected)
    {
        Assert.Equal(expected, SearchPulseOverviewService.IsSupportedRange(rangeDays));
    }

    [Fact]
    public void GetReportingStartUtcReturnsNullForAllTime()
    {
        var startUtc = SearchPulseOverviewService.GetReportingStartUtc(DateTime.UtcNow, 0);

        Assert.Null(startUtc);
    }

    private static SearchPulseOverviewService.SearchPulseInteractionCount Count(SearchPulseEventType eventType, string? target, int interactions) =>
        new() { EventType = eventType.ToString(), Target = target, Interactions = interactions };
}