# Production deployment

SearchPulse is self-contained at runtime. It uses only the database already configured for the Umbraco application and standard .NET hosting services. It does not require Azure, a queue broker, a SaaS account, or a separate telemetry database.

## Supported platform

- Umbraco 17.6 or later, using a package version in the declared NuGet range.
- Any database provider supported by the installed Umbraco version. SQLite is suitable for local development and a single low-traffic site. Use SQL Server or another production Umbraco database provider for production and multi-node deployments.
- One or more identical Umbraco application nodes sharing the same database.

Each node runs a worker. Queue rows are claimed with short database leases, so only one node writes an accepted event to reporting data. The event queue is durable before the visitor receives `202 Accepted`. A process crash before that response means the browser can retry. A crash after the response does not lose the accepted queue row.

No browser analytics package can guarantee an event the browser never sends, or a request lost before it reaches the application. SearchPulse guarantees that events accepted with HTTP 202 are durable in the package queue. If the configured queue capacity is reached, it responds with HTTP 503 and emits a warning and metric instead of discarding data silently.

## Required host integration

1. Register an `IAnalyticsConsentProvider` that reads the existing CMP or consent mechanism.
2. Include the tracker in the public layout only after the same browser consent decision is available.
3. Enable collection in the SearchPulse Settings view after testing the consent path.
4. Map the host health-check endpoint if the application does not already expose one:

```csharp
app.MapHealthChecks("/health");
```

The registered check is named `searchpulse` and tagged `searchpulse`. It reports pending events, capacity, queue percentage, oldest queued event, last completed worker batch, and failure count. The Settings view presents the same operational information to backoffice users.

## Configuration and sizing

```json
{
  "SearchPulse": {
    "Enabled": false,
    "DetailedDataRetentionDays": 30,
    "MaximumQueuedEvents": 100000,
    "QueueWarningThresholdPercent": 75,
    "EventProcessingBatchSize": 250,
    "EventProcessingIntervalMilliseconds": 1000
  }
}
```

Start with the defaults. Set the queue capacity above the largest outage backlog you intend to tolerate. Increase the batch size only after observing database load, lock duration, and queue age in the real environment. The warning threshold is an alert threshold, not a data-loss threshold.

The package emits standard .NET Meter instruments from `SearchPulse`: `searchpulse.events.accepted`, `searchpulse.events.rejected`, `searchpulse.events.processed`, and `searchpulse.queue.batch_failures`. Existing OpenTelemetry or Meter collection can export them without adding a SearchPulse dependency.

## Privacy and signal naming

SearchPulse does not record form values, visitor IDs, IP addresses, query strings, fragments, or arbitrary payload properties. Use stable, non-personal names such as `pricing-enquiry`, `brochure-download`, and `product-tour` for `data-searchpulse-action`, `data-searchpulse-form`, and `data-searchpulse-video`.

The tracker automatically records normal page views, SPA history navigation, page exit, reading depth, external links, downloads, data-marked form submissions, and data-marked video play events. It also exposes `trackAction`, `trackExternalLink`, `trackDownload`, `trackFormSubmit`, and `trackVideoPlay` for explicit integrations.

## Release checklist

- Confirm collection is disabled until the host consent provider allows it.
- Confirm the tracker is included once in the public layout.
- Confirm Settings reports a running worker and a healthy queue under normal traffic.
- Add an alert at the configured warning threshold and for failed batches.
- Load test the actual production database with the expected node count before enabling on a high-traffic site.
- Confirm retention and data-clearing behavior with the data-protection owner.
- Check the browser suite and the optional SQL Server multi-node test after upgrading Umbraco or the database provider.
## Optional SQL Server multi-node verification

The development suites use disposable SQLite databases. To verify the deployment model used by a multi-node production site, build the integration host and run this script against a fresh, disposable SQL Server database:

```powershell
dotnet build tests/SearchPulse.Umbraco.Integration/SearchPulse.Umbraco.Integration.csproj --configuration Release
powershell -NoProfile -File tests/SearchPulse.Umbraco.Integration/verify-sqlserver.ps1 -ConnectionString "Server=localhost;Database=SearchPulseVerify;Integrated Security=True;TrustServerCertificate=True"
```

It starts two independent Umbraco processes on the same database, alternates 200 consented collection requests across them, and requires the reporting table to contain exactly 200 events. Azure Pipelines runs the same check when the secret `SearchPulseSqlServerConnectionString` is supplied. The script leaves application migration tables in the database, so always use a database created solely for this verification.