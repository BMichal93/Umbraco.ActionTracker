using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.UnitTests.Telemetry;

public sealed class SearchPulseEventRequestValidatorTests
{
    [Theory]
    [InlineData("page-view")]
    [InlineData("page-exit")]
    [InlineData("scroll-25")]
    [InlineData("scroll-50")]
    [InlineData("scroll-75")]
    [InlineData("external-link-click")]
    [InlineData("download-click")]
    [InlineData("custom-action")]
    public void TryValidateAcceptsSupportedEventWithSafePath(string eventType)
    {
        var request = new SearchPulseEventRequest
        {
            Type = eventType,
            Path = "/services/seo",
        };

        var isValid = SearchPulseEventRequestValidator.TryValidate(request, out var searchPulseEvent);

        Assert.True(isValid);
        Assert.NotNull(searchPulseEvent);
        Assert.Equal("/services/seo", searchPulseEvent.Path);
    }

    [Theory]
    [InlineData("page-view", "/offers?email=person@example.com")]
    [InlineData("page-view", "/offers#enquiry")]
    [InlineData("page-view", "https://example.com/offers")]
    [InlineData("page-view", "/\\private")]
    [InlineData("unknown", "/offers")]
    public void TryValidateRejectsPathsOrTypesThatCouldCarryUnexpectedData(string eventType, string path)
    {
        var request = new SearchPulseEventRequest
        {
            Type = eventType,
            Path = path,
        };

        var isValid = SearchPulseEventRequestValidator.TryValidate(request, out var searchPulseEvent);

        Assert.False(isValid);
        Assert.Null(searchPulseEvent);
    }

    [Fact]
    public void TryValidateOnlyAcceptsTargetForTheThreeRelevantEventTypes()
    {
        var nonClickRequest = new SearchPulseEventRequest
        {
            Type = "page-view",
            Path = "/offers",
            Target = "newsletter",
        };
        var clickRequest = new SearchPulseEventRequest
        {
            Type = "custom-action",
            Path = "/offers",
            Target = "newsletter-signup",
        };

        var invalid = SearchPulseEventRequestValidator.TryValidate(nonClickRequest, out _);
        var valid = SearchPulseEventRequestValidator.TryValidate(clickRequest, out var searchPulseEvent);

        Assert.False(invalid);
        Assert.True(valid);
        Assert.Equal("newsletter-signup", searchPulseEvent!.Target);
    }
}
