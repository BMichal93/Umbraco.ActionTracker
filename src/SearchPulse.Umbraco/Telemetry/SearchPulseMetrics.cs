using System.Diagnostics.Metrics;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Standard .NET metrics for hosts that already collect Meter data through OpenTelemetry or a
/// compatible collector. The package does not require any telemetry service to use these values.
/// </summary>
internal static class SearchPulseMetrics
{
    private static readonly Meter Meter = new("SearchPulse", "1.0");
    private static readonly Counter<long> AcceptedEvents = Meter.CreateCounter<long>("searchpulse.events.accepted");
    private static readonly Counter<long> RejectedEvents = Meter.CreateCounter<long>("searchpulse.events.rejected");
    private static readonly Counter<long> ProcessedEvents = Meter.CreateCounter<long>("searchpulse.events.processed");
    private static readonly Counter<long> FailedBatches = Meter.CreateCounter<long>("searchpulse.queue.batch_failures");

    public static void RecordAcceptedEvent() => AcceptedEvents.Add(1);

    public static void RecordRejectedEvent() => RejectedEvents.Add(1, new KeyValuePair<string, object?>("reason", "queue_full"));

    public static void RecordProcessedEvents(int count)
    {
        if (count > 0)
        {
            ProcessedEvents.Add(count);
        }
    }

    public static void RecordFailedBatch() => FailedBatches.Add(1);
}