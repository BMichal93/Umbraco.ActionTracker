# SearchPulse demo site

A disposable three-page Umbraco site for manually reviewing SearchPulse. It uses a local SQLite database and the package built in this repository's `artifacts` directory.

The demo seeds two document types - **SearchPulse Demo Home** and **SearchPulse Demo Page** - plus published Home, Services, and Contact nodes. The public pages render through normal Umbraco routing, so their titles, copy, and tracked action labels can be edited in the Content section.

## Run

```powershell
dotnet pack src/SearchPulse.Umbraco/SearchPulse.Umbraco.csproj --configuration Release
dotnet restore samples/SearchPulse.DemoSite/SearchPulse.DemoSite.csproj --no-cache
dotnet run --project samples/SearchPulse.DemoSite/SearchPulse.DemoSite.csproj --launch-profile https
```

Open `https://localhost:44415`. Enable demo consent, use the page-specific action, open the external resource, download the guide, navigate through Home, Services, and Contact, then scroll through a page.

Open `https://localhost:44415/umbraco` to edit the seeded content or review signals in SearchPulse.

- Email: `demo@searchpulse.local`
- Password: `SearchPulse-Demo-2026!`

The demo sends only the package's supported anonymous signal dimensions: page path, fixed event type, and fixed action/download/external-link target. Delete `umbraco/Data/Umbraco.sqlite.db` and `umbraco/Data/SearchPulseDemo-DataProtection-Keys` to reset the demo completely.
