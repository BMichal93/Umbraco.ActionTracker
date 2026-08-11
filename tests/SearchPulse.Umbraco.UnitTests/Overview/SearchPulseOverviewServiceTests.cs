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
    public void GetReportingStartUtcUsesAnInclusiveThirtyDayWindow()
    {
        var generatedAtUtc = new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

        var startUtc = SearchPulseOverviewService.GetReportingStartUtc(generatedAtUtc);

        Assert.Equal(new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc), startUtc);
    }

    private static SearchPulseOverviewService.SearchPulseInteractionCount Count(SearchPulseEventType eventType, string? target, int interactions) =>
        new() { EventType = eventType.ToString(), Target = target, Interactions = interactions };
}
