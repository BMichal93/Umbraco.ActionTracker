# SearchPulse browser tests

These tests start the real Umbraco integration host with a disposable SQLite database and exercise Chromium through Playwright. They cover consent, browser-collected events, authenticated backoffice Overview and Settings, queue processing, and clearing data.

Build the package and integration host first, then run:

```powershell
dotnet restore tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj
dotnet build tests/SearchPulse.Umbraco.Integration/SearchPulse.Umbraco.Integration.csproj --configuration Release
dotnet build tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj --configuration Release
pwsh tests/SearchPulse.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test tests/SearchPulse.BrowserTests/SearchPulse.BrowserTests.csproj --configuration Release --no-build
```