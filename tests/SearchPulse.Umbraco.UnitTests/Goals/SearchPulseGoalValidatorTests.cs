using SearchPulse.Umbraco.Goals;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.UnitTests.Goals;

public sealed class SearchPulseGoalValidatorTests
{
    [Theory]
    [InlineData("FormSuccess", "contact")]
    [InlineData("CustomAction", "newsletter-signup")]
    [InlineData("DownloadClick", "/downloads/guide.pdf")]
    [InlineData("ExternalLinkClick", "partner.example")]
    [InlineData("SiteSearch", "products")]
    public void AcceptsSupportedGoalSignals(string eventType, string target)
    {
        Assert.True(SearchPulseGoalValidator.TryValidate("Qualified lead", eventType, target, out var parsed));
        Assert.Equal(eventType, parsed.ToString());
    }

    [Theory]
    [InlineData("PageView", "home")]
    [InlineData("FormSuccess", "contains spaces")]
    [InlineData("FormSuccess", "person@example.test")]
    [InlineData("Unknown", "contact")]
    public void RejectsUnsupportedOrFreeFormGoalSignals(string eventType, string target)
    {
        Assert.False(SearchPulseGoalValidator.TryValidate("Goal", eventType, target, out _));
    }

    [Fact]
    public void RejectsOversizedGoalFields()
    {
        Assert.False(SearchPulseGoalValidator.TryValidate(new string('a', 81), "FormSuccess", "contact", out _));
        Assert.False(SearchPulseGoalValidator.TryValidate("Goal", "FormSuccess", new string('a', 81), out _));
    }
}
