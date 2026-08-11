using Microsoft.Extensions.Options;

namespace SearchPulse.Umbraco.Configuration;

public sealed class SearchPulseOptionsValidator : IValidateOptions<SearchPulseOptions>
{
    public ValidateOptionsResult Validate(string? name, SearchPulseOptions options)
    {
        if (options.DetailedDataRetentionDays is < 30 or > 90)
        {
            return ValidateOptionsResult.Fail("Detailed data retention must be between 30 and 90 days.");
        }

        if (options.ExcludedPaths.Any(path => string.IsNullOrWhiteSpace(path) || !path.StartsWith('/')))
        {
            return ValidateOptionsResult.Fail("Every excluded path must begin with '/'.");
        }

        return ValidateOptionsResult.Success;
    }
}
