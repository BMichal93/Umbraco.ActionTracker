# Contributing

Every feature must remain understandable to both a website owner/editor and a business owner. Before merging, record the decision in `docs/design-decisions.md`, including three improvements and three uncertainties, then address the material gaps.

Required checks:

```powershell
dotnet build SearchPulse.Umbraco.sln --configuration Release
dotnet test SearchPulse.Umbraco.sln --configuration Release --no-build
dotnet pack src/SearchPulse.Umbraco/SearchPulse.Umbraco.csproj --configuration Release --no-build
```

Do not add a visible setting unless it changes a decision a normal website owner can safely make.
