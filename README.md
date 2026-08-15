# SearchPulse for Umbraco

SearchPulse is a free, self-hosted Umbraco package for small teams that need simple content engagement signals: page views, reading depth, page exits, important clicks, form/search outcomes, acquisition dimensions, and content attribution.

## Status

Early development. The package is intentionally disabled by default and does not assume a visitor has granted analytics consent.

## Product principles

- One analytics screen, one settings screen.
- Aggregate content signals before individual visitor detail.
- No third-party analytics service or SaaS account.
- No form capture, session replay, raw IP retention, or cross-site tracking.
- No client-side tracking until the host website supplies an analytics-consent provider.

## Local development

```powershell
dotnet restore SearchPulse.Umbraco.sln
dotnet build SearchPulse.Umbraco.sln --configuration Release
dotnet pack src/SearchPulse.Umbraco/SearchPulse.Umbraco.csproj --configuration Release --no-build
```

## Demo site

A standalone three-page manual review site is available at [samples/SearchPulse.DemoSite/README.md](samples/SearchPulse.DemoSite/README.md). It includes consent-gated examples of every supported signal.

## Verification

SearchPulse has no runtime infrastructure beyond the Umbraco application's existing database. The unit suite covers validation, configuration, overview composition, and aggregate-key stability:

```powershell
dotnet test SearchPulse.Umbraco.sln --configuration Release
```

The browser suite starts a clean local Umbraco host with SQLite and checks consent-gated collection, the backoffice views, data clearing, retention aggregation, all-time reporting, concurrent collection, collector latency, and graceful worker shutdown:

```powershell
dotnet pack src/SearchPulse.Umbraco/SearchPulse.Umbraco.csproj --configuration Release
dotnet restore tests/SearchPulse.Umbraco.Integration/SearchPulse.Umbraco.Integration.csproj --no-cache
dotnet build tests/SearchPulse.Umbraco.Integration/SearchPulse.Umbraco.Integration.csproj --configuration Release --no-restore
dotnet build tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj --configuration Release
dotnet test tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj --configuration Release --no-build
```

The browser suite is development-only and uses Playwright's local browser runtime. It does not add a package dependency to an installed Umbraco website. For a production-database, two-node verification, see the opt-in [SQL Server multi-node script](docs/production.md#optional-sql-server-multi-node-verification).

## Installation and setup

Install `SearchPulse.Umbraco` from NuGet in an Umbraco 17.6+ web project. SearchPulse is off by default. Add this optional retention configuration to `appsettings.json` (30 days is the default):

```json
{
  "SearchPulse": {
    "DetailedDataRetentionDays": 30,
    "MaximumQueuedEvents": 100000,
    "QueueWarningThresholdPercent": 75,
    "EventProcessingBatchSize": 250
  }
}
```

Register an `IAnalyticsConsentProvider` in the host site's startup code. It must check the site's existing consent mechanism; SearchPulse deliberately has no universal cookie name or consent UI.

```csharp
builder.Services.AddSingleton<IAnalyticsConsentProvider, MySiteAnalyticsConsentProvider>();
```

The package's default provider always denies consent, so skipping this step means no event is stored.

A cookie-based example is available at [samples/ConsentProvider/CookieAnalyticsConsentProvider.cs.example](samples/ConsentProvider/CookieAnalyticsConsentProvider.cs.example). Replace the example cookie name and value with those used by the existing consent-management platform; do not add a second consent banner just for SearchPulse.

Include the tracker in the public layout and start it only after the same consent decision is available in the browser:

```html
<script src="/App_Plugins/SearchPulse/searchpulse-tracker.js" defer></script>
<script>
  // Run this only after the visitor has accepted analytics in your existing CMP.
  window.SearchPulseConsent = true;
  window.SearchPulse?.start();
</script>
```

The server independently checks `IAnalyticsConsentProvider`; the browser flag alone never enables collection. The client sends only the current path and a fixed event type. Page-view counts are not unique visitors or sessions, and page-exit counts are browser lifecycle signals rather than an exit-rate calculation. It excludes query strings, fragments, visitor identifiers, IP addresses, and arbitrary properties.
Accepted events are first written to a package-owned durable database queue. A hosted Umbraco service processes bounded batches into reporting data, so reporting work does not run on the visitor request. The queue defaults to 100,000 events as a site-protection limit and uses short leases to coordinate multiple Umbraco nodes. If that limit is reached, SearchPulse returns a retryable HTTP 503 response rather than allowing analytics to exhaust the site database. The Settings view and the registered `searchpulse` health check report the queue age, worker heartbeat, and failed batch count. See [production deployment guidance](docs/production.md) for sizing, metrics, multi-node behavior, and the release checklist.

To record a meaningful local business action after tracking has started, call `window.SearchPulse.trackAction("newsletter-signup")`. Use `trackFormSuccess("contact")` for a confirmed form outcome and `trackSiteSearch("products")` for an internal search outcome. The collector also records bounded UTM source/medium/campaign values, an external referrer domain, and an optional content key from `data-searchpulse-content-key` or `window.SearchPulseContentKey`; these values are lower-cased, length-limited, and never include query strings or identifiers. Set `window.SearchPulseDataLayerExport = true` only when the host explicitly wants the approved event shape copied to its existing `dataLayer`. Single-page applications are tracked automatically when they use the History API; manually call `window.SearchPulse.trackPageView()` only for non-standard routing. Use `data-searchpulse-form` and `data-searchpulse-video` for anonymous form-submit and media-play signals, or use `trackAction`, `trackExternalLink`, `trackDownload`, `trackFormSubmit`, and `trackVideoPlay` for explicit integrations. Downloads are recorded by their same-origin path without query strings. Action names can contain lowercase letters, digits, dots, and hyphens only; this prevents the client from turning events into a free-form data channel.

After the consent integration and layout include are ready, open the **SearchPulse** section in Umbraco, then use its single switch to turn tracking on. The Overview shows page views, exits, reading milestones, up to five most-viewed pages, up to five popular anonymous interactions, goal completions, acquisition dimensions, and content attribution. Goals are created and removed in Settings; they match a fixed event type and bounded target. Its 1, 7, 30, and 90-day ranges use exact detailed rows. The All time range also includes compact UTC daily aggregates created before detailed rows expire. Clearing All SearchPulse data removes both detailed and aggregated records.

## Privacy boundary

SearchPulse is not a replacement for legal advice or a consent-management platform. It is designed to minimize data: no session replay, form capture, raw IP retention, user-agent storage, third-party transfer, or person-level journey reporting. The host remains responsible for choosing and documenting a lawful consent basis.

## Licence

[MIT](LICENSE)
