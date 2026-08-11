# Repository and release plan

## Remote repository

Create the public GitHub repository at `https://github.com/BMichal93/searchpulse-for-umbraco` with the default branch named `main`. Use the MIT licence, enable GitHub Actions, and protect `main` with the Build and test workflow required for pull requests.

Before the first public release, update the package metadata if ownership moves to an organization. The NuGet package ID remains `SearchPulse.Umbraco`; namespace, assembly, route, and extension aliases all retain the `SearchPulse` prefix.

## Versioning and release

Use SemVer:

- `0.x` while the package contract and data schema are still evolving.
- `1.0.0` only after migration, SQL-provider, and backoffice integration tests are in place.
- Release notes state data-schema and consent-boundary changes plainly.

Tag releases as `vX.Y.Z`, publish the signed `.nupkg` to NuGet.org from a protected release workflow, and keep symbol packages available for diagnostics. Never put NuGet credentials in source control or a client-side manifest.

## Quality gates

Every pull request must restore, build with warnings treated as errors, run tests, and pack the NuGet artifact. The package-smoke project restores the generated package and compiles its static-web-asset imports. Before a release, inspect the package contents and test installation in a clean Umbraco 17 site; repeat on Umbraco 18 before widening its supported dependency range.

The automated package-host smoke test and a clean Umbraco 17 integration host are present. The integration host uses SQLite, an unattended test-only administrator, and a consent provider that accepts only the `SearchPulseIntegrationConsent=yes` cookie. Its endpoint checks must prove 204 without consent, 202 with the test cookie, and 400 for an untrusted Origin before a release candidate is published.
