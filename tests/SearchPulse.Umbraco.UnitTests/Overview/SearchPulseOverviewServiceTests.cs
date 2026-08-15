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
            item => Assert.Equal(("CustomAction", "newsletter-signup", 9L), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("ExternalLinkClick", "example.com", 8L), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("DownloadClick", "download", 7L), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("CustomAction", "book-demo", 6L), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("ExternalLinkClick", "partner.example", 5L), (item.EventType, item.Target, item.Interactions)));
    }

    [Fact]
    public void BuildPopularInteractionsCombinesDetailedAndArchivedRows()
    {
        var interactions = new[]
        {
            Count(SearchPulseEventType.CustomAction, "newsletter-signup", 9),
            Count(SearchPulseEventType.CustomAction, "newsletter-signup", 11),
            Count(SearchPulseEventType.DownloadClick, "guide", 12),
        };

        var popular = SearchPulseOverviewService.BuildPopularInteractions(interactions);

        Assert.Collection(
            popular,
            item => Assert.Equal(("CustomAction", "newsletter-signup", 20L), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("DownloadClick", "guide", 12L), (item.EventType, item.Target, item.Interactions)));
    }

    [Fact]
    public void BuildTopPagesCombinesDetailedAndArchivedRowsBeforeSelectingTheTopFive()
    {
        var pages = new[]
        {
            new SearchPulseOverviewService.SearchPulsePageCount { Path = "/services", PageViews = 4 },
            new SearchPulseOverviewService.SearchPulsePageCount { Path = "/services", PageViews = 9 },
            new SearchPulseOverviewService.SearchPulsePageCount { Path = "/contact", PageViews = 10 },
        };

        var topPages = SearchPulseOverviewService.BuildTopPages(pages);

        Assert.Collection(
            topPages,
            item => Assert.Equal(("/services", 13L), (item.Path, item.PageViews)),
            item => Assert.Equal(("/contact", 10L), (item.Path, item.PageViews)));
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

    private static SearchPulseOverviewService.SearchPulseInteractionCount Count(SearchPulseEventType eventType, string? target, long interactions) =>
        new() { EventType = eventType.ToString(), Target = target, Interactions = interactions };

    [Fact]
    public void BuildPopularInteractionsIncludesFormAndVideoSignals()
    {
        var interactions = new[]
        {
            Count(SearchPulseEventType.FormSubmit, "contact-enquiry", 6),
            Count(SearchPulseEventType.VideoPlay, "product-tour", 5),
        };

        var popular = SearchPulseOverviewService.BuildPopularInteractions(interactions);

        Assert.Collection(
            popular,
            item => Assert.Equal(("FormSubmit", "contact-enquiry", 6L), (item.EventType, item.Target, item.Interactions)),
            item => Assert.Equal(("VideoPlay", "product-tour", 5L), (item.EventType, item.Target, item.Interactions)));
    }
}