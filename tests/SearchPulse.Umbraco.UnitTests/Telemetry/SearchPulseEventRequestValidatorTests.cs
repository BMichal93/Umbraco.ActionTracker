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
    [InlineData("form-submit")]
    [InlineData("form-success")]
    [InlineData("video-play")]
    [InlineData("site-search")]
    [InlineData("active-engagement")]
    [InlineData("low-engagement-exit")]
    public void TryValidateAcceptsSupportedEventWithSafePath(string eventType)
    {
        var request = new SearchPulseEventRequest { Type = eventType, Path = "/services/seo" };
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
        var request = new SearchPulseEventRequest { Type = eventType, Path = path };
        var isValid = SearchPulseEventRequestValidator.TryValidate(request, out var searchPulseEvent);
        Assert.False(isValid);
        Assert.Null(searchPulseEvent);
    }

    [Fact]
    public void TryValidateOnlyAcceptsTargetForRelevantEventTypes()
    {
        var nonClickRequest = new SearchPulseEventRequest { Type = "page-view", Path = "/offers", Target = "newsletter" };
        var clickRequest = new SearchPulseEventRequest { Type = "custom-action", Path = "/offers", Target = "newsletter-signup" };
        Assert.False(SearchPulseEventRequestValidator.TryValidate(nonClickRequest, out _));
        Assert.True(SearchPulseEventRequestValidator.TryValidate(clickRequest, out var searchPulseEvent));
        Assert.Equal("newsletter-signup", searchPulseEvent!.Target);
    }

    [Fact]
    public void TryValidateAcceptsSafeLocalDownloadPath()
    {
        var request = new SearchPulseEventRequest { Type = "download-click", Path = "/resources", Target = "/downloads/searchpulse-guide.pdf" };
        Assert.True(SearchPulseEventRequestValidator.TryValidate(request, out var searchPulseEvent));
        Assert.Equal("/downloads/searchpulse-guide.pdf", searchPulseEvent!.Target);
    }

    [Theory]
    [InlineData("form-submit", "contact-enquiry")]
    [InlineData("form-success", "contact-enquiry")]
    [InlineData("video-play", "product-tour")]
    [InlineData("site-search", "products")]
    public void TryValidateAcceptsAnonymousInteractionTargets(string eventType, string target)
    {
        var request = new SearchPulseEventRequest { Type = eventType, Path = "/offers", Target = target };
        Assert.True(SearchPulseEventRequestValidator.TryValidate(request, out var searchPulseEvent));
        Assert.Equal(target, searchPulseEvent!.Target);
    }

    [Fact]
    public void TryValidateAcceptsContextDimensions()
    {
        var request = new SearchPulseEventRequest { Type = "form-success", Path = "/offers", Target = "contact", ContentKey = "home", ReferrerDomain = "partner.example", UtmSource = "newsletter", UtmMedium = "email", UtmCampaign = "spring" };
        Assert.True(SearchPulseEventRequestValidator.TryValidate(request, out var result));
        Assert.Equal("partner.example", result!.ReferrerDomain);
        Assert.Equal("spring", result.UtmCampaign);
    }

    [Fact]
    public void TryValidateRejectsUnboundedContextValues()
    {
        var request = new SearchPulseEventRequest { Type = "page-view", Path = "/offers", ContentKey = "contains spaces" };
        Assert.False(SearchPulseEventRequestValidator.TryValidate(request, out _));
    }

    [Theory]
    [InlineData("form-submit", "contact enquiry")]
    [InlineData("video-play", "ProductTour")]
    [InlineData("form-submit", "person@example.test")]
    [InlineData("download-click", "/downloads/guide.pdf?email=person@example.test")]
    public void TryValidateRejectsTargetsThatCouldBecomePersonalOrFreeFormData(string eventType, string target)
    {
        var request = new SearchPulseEventRequest { Type = eventType, Path = "/offers", Target = target };
        Assert.False(SearchPulseEventRequestValidator.TryValidate(request, out var searchPulseEvent));
        Assert.Null(searchPulseEvent);
    }
}
