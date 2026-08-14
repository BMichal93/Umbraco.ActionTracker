using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.UnitTests.Telemetry;

public sealed class SearchPulseDailyAggregateDtoTests
{
    [Fact]
    public void CreateBucketKeyIsStableForTheSameDailySignal()
    {
        var date = new DateTime(2026, 8, 14, 17, 32, 00, DateTimeKind.Utc);

        var first = SearchPulseDailyAggregateDto.CreateBucketKey(date, "PageView", "/services", null);
        var second = SearchPulseDailyAggregateDto.CreateBucketKey(date.Date, "PageView", "/services", string.Empty);

        Assert.Equal(first, second);
        Assert.Matches("^[a-f0-9]{64}$", first);
    }

    [Fact]
    public void CreateBucketKeyChangesWhenAnyReportingDimensionChanges()
    {
        var date = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc);
        var expected = SearchPulseDailyAggregateDto.CreateBucketKey(date, "CustomAction", "/services", "book-demo");

        Assert.NotEqual(expected, SearchPulseDailyAggregateDto.CreateBucketKey(date.AddDays(1), "CustomAction", "/services", "book-demo"));
        Assert.NotEqual(expected, SearchPulseDailyAggregateDto.CreateBucketKey(date, "CustomAction", "/contact", "book-demo"));
        Assert.NotEqual(expected, SearchPulseDailyAggregateDto.CreateBucketKey(date, "CustomAction", "/services", "newsletter-signup"));
    }
}