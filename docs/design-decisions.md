# Design decisions

## 2026-08-11: Start with a separate section and two views

**Decision:** SearchPulse has one top-level section with `Overview` and `Settings` views. It does not add a tree, query builder, or feature-specific dashboards.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. The feature is easy to find and has one clear place to answer “how is my content doing?”.

**Would I understand it as a business owner?** Yes. “Overview” and “Settings” are familiar and do not demand analytics expertise.

**Three improvements to make next:**

1. Replace the foundation messages with real status and simple page metrics.
2. Make the active/inactive state unmistakable without adding a setup wizard.
3. Add accessible empty states that explain the next action in one sentence.

**Three uncertainties to resolve next:**

1. How should the host website register its consent provider with the least implementation work?
2. Which minimal event fields let us calculate useful entries and exits without keeping individual journeys?
3. What data volume can a small Umbraco SQL database handle before rollups must run more frequently?

## 2026-08-11: Deny analytics consent by default

**Decision:** `IAnalyticsConsentProvider` defaults to deny. Collection cannot start merely because the NuGet package was installed.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. It prevents a package installation from creating an unexpected tracking obligation.

**Would I understand it as a business owner?** Yes. Tracking remains off until the website's existing consent choice allows it.

**Three improvements to make next:**

1. Supply a concise adapter example for common consent platforms.
2. Show a clear Settings warning when no consent provider is registered.
3. Test that no tracker endpoint accepts client events without consent.

**Three uncertainties to resolve next:**

1. How can the package distinguish a deliberately registered denial provider from the default provider?
2. Which server-only aggregate signals remain useful when a visitor declines consent?
3. What is the cleanest consent-provider registration pattern for Umbraco 17 and 18?

**Gaps addressed before moving on:** Added a unit test proving the default dependency-injection setup resolves to denial. We will only add a consent-provider sample after the collection endpoint exists, so it tests a real end-to-end boundary rather than an example in isolation.

## 2026-08-11: Offer one controlled retention range

**Decision:** Detailed data retention is limited to 30, 60, or 90 days. The validation layer enforces that range before the UI is built.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. It gives a meaningful privacy/storage decision without exposing database housekeeping details.

**Would I understand it as a business owner?** Yes. The question is simply how long the site needs recent detail; it is not a technical configuration exercise.

**Three improvements to make next:**

1. Make 30 days the visually recommended choice in Settings.
2. Explain that anonymous daily totals can remain after detailed data is purged.
3. Show the next scheduled purge date, rather than asking users to manage it.

**Three uncertainties to resolve next:**

1. Should detailed data be held for a shorter default period when a site has high traffic?
2. What is the least confusing way to describe the difference between detailed events and anonymous totals?
3. Should an administrator be allowed to select a custom retention period outside the UI through configuration?

**Gaps addressed before moving on:** The supported range and invalid-path handling are covered by unit tests. We will resolve presentation and high-volume behaviour alongside the event store, where we can measure the trade-off rather than guess.

## 2026-08-11: Fixed anonymous event payload

**Decision:** Accept only eight plain-language event types: page view, page exit, three scroll milestones, external-link click, download click, and custom action. Store only server time, page path, event type, and an optional short semantic target. Query strings, fragments, URLs, identifiers, user agents, IP addresses, and arbitrary properties are rejected or never accepted.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. The dashboard can explain every figure without making the owner choose a tracking schema.

**Would I understand it as a business owner?** Yes. "People viewed this page" and "people reached 75%" are clearer than opaque event property names.

**Three improvements to make next:**

1. Give implementers a concise client integration snippet rather than asking them to invent requests.
2. Present collection state clearly, including whether a consent provider is active.
3. Add a rolling retention job before there is meaningful traffic.

**Three uncertainties to resolve next:**

1. Whether a site uses a client-side router that changes pages without navigation.
2. Which local consent platforms package users will want to connect.
3. Whether custom actions need a more restrictive allow-list per site.

**Gaps addressed before moving on:** Validation tests now prove that the event vocabulary is fixed, query strings and fragments are rejected, and targets are allowed only where they make sense. The next feature supplies a small client script and a same-origin endpoint; custom-action allow-listing stays an explicit post-MVP decision.

## 2026-08-11: Explicit client start with a same-origin endpoint

**Decision:** Supply one lightweight browser script, but do not inject it automatically. The site owner adds a single layout include and starts the tracker after its existing consent mechanism allows analytics. The endpoint requires a matching `Origin` header, separately checks the server-side consent provider, accepts at most 1 KB of JSON, and is unavailable when tracking is off.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. The integration is visible in the layout and does not buffer, rewrite, or risk breaking page responses.

**Would I understand it as a business owner?** Yes. The only operational rule is "turn tracking on after the visitor accepts analytics"; the dashboard will describe the technical link without making it a workflow.

**Three improvements to make next:**

1. Add a copy-ready consent-provider adapter example for the most common cookie-based setup.
2. Make the backoffice show whether collection is switched on and whether any events have arrived.
3. Harden the script for client-side route changes and unusual click targets.

**Three uncertainties to resolve next:**

1. Whether every hosted Umbraco deployment supplies an `Origin` header for every page-exit request.
2. Whether a small site needs automatic download detection beyond links marked with `download`.
3. How the package should be enabled safely when app settings and a backoffice toggle disagree.

**Gaps addressed before moving on:** Automated endpoint tests cover disabled, consent-denied, accepted, and cross-origin cases. The endpoint intentionally fails closed if `Origin` is missing. The next feature adds one persisted backoffice switch so the status is not inferred from configuration alone.

## 2026-08-11: One-toggle dashboard with a 30-day content summary

**Decision:** Keep the backoffice to two views. Overview presents just five 30-day totals and up to five most-viewed pages in one surface. Settings contains one switch: `Turn on SearchPulse tracking`. The switch persists through Umbraco's key-value store; app settings provide only the safe initial default.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. I can find the switch and immediately see whether useful signals are arriving, without configuring reports, dimensions, or cards.

**Would I understand it as a business owner?** Yes. The language says page views, exits, and reading milestones—not sessions, funnels, or anonymous identifiers.

**Three improvements to make next:**

1. Add a visible last-updated time so an owner can distinguish an empty site from a loading issue.
2. Add a link from the empty overview to the concise consent and layout steps.
3. Add automatic expiry of detailed event rows to honor the selected retention period.

**Three uncertainties to resolve next:**

1. Whether backoffice users need a narrower permission than general SearchPulse section access.
2. Whether a 30-day fixed window should later gain one simple 7/30-day choice.
3. How many page groups can be queried efficiently before a daily rollup table is necessary.

**Gaps addressed before moving on:** Management API tests prove the overview response and the one toggle's persistence boundary. The package remains intentionally limited to a 30-day view; event retention and database-scale behavior are the next data responsibility before adding more reports.

## 2026-08-11: Automatic detailed-event retention

**Decision:** A hosted service purges detailed event rows on startup and every day using the configured 30, 60, or 90-day limit. This never appears as another dashboard control.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. Storage and privacy remain bounded without an editor scheduling database work or learning a maintenance interface.

**Would I understand it as a business owner?** Yes. The product promises recent, anonymous content signals; it does not ask the owner to make a technical retention decision each day.

**Three improvements to make next:**

1. Preserve anonymized daily rollups after detailed rows expire.
2. Show the configured retention period in a short Settings sentence, without adding another control.
3. Add an integration test against both supported SQL providers to exercise migration and purging together.

**Three uncertainties to resolve next:**

1. Whether installation-time migration always completes before hosted-service startup in every Umbraco hosting mode.
2. Which rollup schema best serves sites that grow beyond a small-team data volume.
3. Whether a user-facing manual purge is needed for privacy requests despite having no visitor identifier.

**Gaps addressed before moving on:** The scheduler catches and logs cleanup failures so it cannot stop the website, and retries daily. Configuration validation already prevents an unsafe retention range. SQL-provider integration coverage is deliberately listed as the next release-quality requirement because it needs a real Umbraco test host.

## 2026-08-11: Consent integration as a copy-ready host example

**Decision:** Supply a cookie-based `IAnalyticsConsentProvider` example for developers to adapt, rather than guessing a universal consent cookie or adding another package-owned banner. Custom business events have one small API: `window.SearchPulse.trackAction("meaningful-action")`.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. The developer receives a clear integration starting point without SearchPulse taking ownership of the site's legal consent experience.

**Would I understand it as a business owner?** Yes. The product explains that it uses the website's existing analytics choice and can measure a named local action such as a newsletter signup.

**Three improvements to make next:**

1. Publish adapters for the consent platforms most used by package adopters.
2. Provide a small integration-test website that demonstrates a real consent transition.
3. Let the Settings view link directly to the short setup instructions.

**Three uncertainties to resolve next:**

1. Which consent platforms package users actually use most often.
2. Whether action naming needs a host-defined allow-list in addition to the fixed character rule.
3. Whether a consent provider needs an explicit diagnostic status separate from its privacy decision.

**Gaps addressed before moving on:** The example uses a developer-supplied cookie name and value, so SearchPulse cannot accidentally enable itself on an unrelated site. The current client and server both require consent; the remaining real-Umbraco installation check is preserved as a release gate rather than simulated incompletely.

## 2026-08-11: Verify through a clean Umbraco 17 host

**Decision:** Add an isolated Umbraco 17 SQLite integration host that consumes the generated NuGet package. It performs unattended installation using test-only credentials and uses a test-only consent cookie; it is never a production template or a consent recommendation.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. It catches migration and asset-installation errors where they actually occur, without making a website owner configure a test framework.

**Would I understand it as a business owner?** Yes. This is invisible release-quality work: it protects the promise that installing the package does not break the website.

**Three improvements to make next:**

1. Automate the three endpoint assertions in CI rather than running them manually against the local host.
2. Add the same integration flow for SQL Server.
3. Authenticate a test backoffice user and verify the section, overview, and toggle in a browser.

**Three uncertainties to resolve next:**

1. Whether a package migration should include a separate schema-version test for upgrade paths.
2. Whether the authenticated Management API route requires a package-specific authorization policy.
3. How best to run browser-level Umbraco tests in a public CI environment without storing test credentials.

**Gaps addressed before moving on:** The real host completed unattended Umbraco installation, registered the packaged static assets, ran the SearchPulse migration, and created `target` as nullable. Live endpoint checks returned 204 without consent, 202 with the test consent cookie, and 400 for an untrusted Origin. The nullable-target migration defect found during this test was fixed and released as `0.1.0-alpha.2` before the verification was repeated.

## 2026-08-11: Repeatable local-host verification

**Decision:** Add a PowerShell verification script that starts a clean local Umbraco host, verifies the three privacy-critical collection outcomes, checks the migration log, and always stops the host. Run it in CI after package, package-consumer, and integration-host builds.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. A small package needs evidence that its central promise works in a real Umbraco application, not only unit tests.

**Would I understand it as a business owner?** Yes. It verifies the simple promise: no consent means no collection; consent allows collection; other websites cannot submit data.

**Three improvements to make next:**

1. Avoid depending on the machine's default `dotnet` path by allowing an explicit executable path.
2. Make reruns reliable by optionally removing only the integration fixture database.
3. Keep the checks readable by naming expected HTTP outcomes in the script output.

**Three uncertainties to resolve next:**

1. CI and developer machines can use different PowerShell versions.
2. An Umbraco application has a slow first boot.
3. Migration behaviour can regress without changing endpoint code.

**Gaps addressed before moving on:** The script uses broadly available `Invoke-WebRequest` rather than a runtime-specific HTTP client type, retries until the collector returns the expected no-consent result, and checks the generated migration log for the nullable `target` column. It remains entirely local and disposable.

## 2026-08-11: Fresh overview feedback and package-level verification

**Decision:** Show when the 30-day overview was generated and link its empty state to the concise setup instructions. Exercise the packaged backoffice asset and its authenticated API boundary in the clean Umbraco host, and publish the increment as a new alpha package version.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. An owner can tell a successful refresh from an empty dashboard and has one direct next step when no signals are present.

**Would I understand it as a business owner?** Yes. The page says when its simple figures were updated and points to setup rather than exposing technical diagnostics.

**Three improvements to make next:**

1. Add a fully automated login-and-render browser test for the backoffice section.
2. Run the retention and aggregation checks against SQL Server as well as SQLite.
3. Add a simple release workflow that publishes a chosen pre-release package version.

**Three uncertainties to resolve next:**

1. Which headless browser runtime is reliable across the supported CI environments.
2. Whether a separate package version should be required for every documentation-only change.
3. Whether a package-specific authorization policy is needed beyond Umbraco's management API boundary.

**Gaps addressed before moving on:** The overview now exposes its generation time, has a setup link when empty, ranks and caps interaction summaries under unit test, and the clean host verifies the installed overview asset plus unauthenticated management API rejection. The remaining interactive login check requires a headless browser runtime.

## 2026-08-13: SQL Server package-host verification

**Decision:** Run the clean package-host verification against a disposable SQL Server 2022 service in CI as well as the existing SQLite fixture. Keep database reset deletion SQLite-only; SQL Server receives an isolated database created by Umbraco's unattended install.

**Would I use it as a website owner/editor/Umbraco expert?** Yes. Package installation is verified against the two database engines a small Umbraco site is most likely to encounter, without adding anything to the editor experience.

**Would I understand it as a business owner?** Yes. This is a reliability check: the package continues to work when the website uses SQL Server rather than its local test database.

**Three improvements to make next:**

1. Exercise retention with an expired row against both provider fixtures.
2. Add an authenticated browser test when a reliable headless runtime is available.
3. Consolidate repeated package-build steps in CI once the release workflow is introduced.

**Three uncertainties to resolve next:**

1. Whether the SQL Server container image keeps the current `sqlcmd` health-check path across future tags.
2. Whether CI runtime remains acceptable once browser coverage is added.
3. Whether a separate upgrade-path fixture is needed before the first stable release.

**Gaps addressed before moving on:** The tracked integration configuration now contains its own test-only SQLite defaults, SQL Server overrides only the connection string in CI, and the same migration, consent, origin, backoffice-asset, and authorization checks run for each provider. Local verification continues to cover SQLite; the SQL Server service is provisioned by CI.
