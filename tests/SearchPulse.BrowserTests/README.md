# SearchPulse browser tests

These tests start the real Umbraco integration host with a disposable SQLite database and exercise Chromium through Playwright. They cover consent, browser-collected events, authenticated backoffice Overview and Settings, queue processing, clearing data, and concurrent collector load.

Build the package and integration host first, then run:

```powershell
dotnet restore tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj
dotnet build tests/SearchPulse.Umbraco.Integration/SearchPulse.Umbraco.Integration.csproj --configuration Release
dotnet build tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj --configuration Release
pwsh tests/SearchPulse.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj --configuration Release --no-build
```
The concurrent coverage includes a 300-request stress test and a 120-request performance test at 20 concurrent requests. Both verify that the durable reporting store contains exactly every accepted event; the performance test also requires a collector p95 below two seconds.
