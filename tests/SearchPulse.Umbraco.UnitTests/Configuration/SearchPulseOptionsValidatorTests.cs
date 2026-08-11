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
}
