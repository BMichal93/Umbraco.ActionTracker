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

        if (options.MaximumQueuedEvents is < 1_000 or > 1_000_000)
        {
            return ValidateOptionsResult.Fail("Maximum queued events must be between 1,000 and 1,000,000.");
        }

        if (options.EventProcessingBatchSize is < 10 or > 1_000)
        {
            return ValidateOptionsResult.Fail("Event processing batch size must be between 10 and 1,000.");
        }

        if (options.EventProcessingIntervalMilliseconds is < 100 or > 60_000)
        {
            return ValidateOptionsResult.Fail("Event processing interval must be between 100 and 60,000 milliseconds.");
        }

        if (options.ExcludedPaths.Any(path => string.IsNullOrWhiteSpace(path) || !path.StartsWith('/')))
        {
            return ValidateOptionsResult.Fail("Every excluded path must begin with '/'.");
        }

        return ValidateOptionsResult.Success;
    }
}