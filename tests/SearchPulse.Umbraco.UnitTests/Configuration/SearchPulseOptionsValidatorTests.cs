using SearchPulse.Umbraco.Configuration;

namespace SearchPulse.Umbraco.UnitTests.Configuration;

public sealed class SearchPulseOptionsValidatorTests
{
    private readonly SearchPulseOptionsValidator _validator = new();

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    public void ValidateAcceptsAClearRetentionChoice(int retentionDays)
    {
        var options = new SearchPulseOptions
        {
            DetailedDataRetentionDays = retentionDays,
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(91)]
    public void ValidateRejectsRetentionOutsideTheSimpleSupportedRange(int retentionDays)
    {
        var options = new SearchPulseOptions
        {
            DetailedDataRetentionDays = retentionDays,
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(1_000_001)]
    public void ValidateRejectsQueueCapacityOutsideTheSafeRange(int maximumQueuedEvents)
    {
        var options = new SearchPulseOptions
        {
            MaximumQueuedEvents = maximumQueuedEvents,
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void ValidateRejectsWarningThresholdOutsideTheSupportedRange(int threshold)
    {
        var result = _validator.Validate(null, new SearchPulseOptions { QueueWarningThresholdPercent = threshold });

        Assert.False(result.Succeeded);
    }
    [Fact]
    public void ValidateRejectsAnExcludedPathThatIsNotAPath()
    {
        var options = new SearchPulseOptions
        {
            ExcludedPaths = ["umbraco"],
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void ValidateAcceptsWarningThresholdAtBothSupportedBoundaries(int threshold)
    {
        var result = _validator.Validate(null, new SearchPulseOptions { QueueWarningThresholdPercent = threshold });

        Assert.True(result.Succeeded);
    }
}