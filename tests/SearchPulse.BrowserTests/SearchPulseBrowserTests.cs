using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace SearchPulse.BrowserTests;

public sealed class SearchPulseBrowserTests : IAsyncLifetime
{
    private readonly SearchPulseBrowserHost _host = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        await _host.StartAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        await _host.StopAsync();
    }

    [Fact]
    public async Task ConsentTrackerBackofficeAndDataManagementWorkEndToEnd()
    {
        var browser = Assert.IsAssignableFrom<IBrowser>(_browser);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_host.ReviewUrl);
        await Expect(page.GetByText("Analytics consent is disabled.")).ToBeVisibleAsync();
        await page.Locator("#consent-button").ClickAsync();
        await page.WaitForURLAsync("**/searchpulse-review");
        await Expect(page.GetByText("Analytics consent is enabled.")).ToBeVisibleAsync();

        await page.Locator("#action-button").ClickAsync();
        await page.Locator("a[href='/searchpulse-review/products']").ClickAsync();
        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await page.WaitForTimeoutAsync(1500);

        await OpenBackofficeAsync(page);
        await page.GotoAsync(_host.OverviewUrl);
        try
        {
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "SearchPulse" })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        }
        catch (PlaywrightException exception)
        {
            var body = await page.Locator("body").InnerTextAsync();
            throw new InvalidOperationException($"SearchPulse overview did not render. URL={page.Url}. Body={body}", exception);
        }
        await Expect(page.GetByText("Most viewed pages")).ToBeVisibleAsync();
        await Expect(page.GetByText("Popular interactions")).ToBeVisibleAsync();
        await Expect(page.GetByText("newsletter-signup")).ToBeVisibleAsync();

        await page.Locator("a[href=\"/umbraco/section/searchpulse/view/settings\"]").ClickAsync();
        try
        {
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Settings" })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        }
        catch (PlaywrightException exception)
        {
            var body = await page.Locator("body").InnerTextAsync();
            throw new InvalidOperationException($"SearchPulse settings did not render. URL={page.Url}. Body={body}", exception);
        }
        await Expect(page.GetByText("Collection health")).ToBeVisibleAsync();
        await Expect(page.GetByText("Data management")).ToBeVisibleAsync();

        page.Dialog += (_, dialog) => dialog.AcceptAsync();
        await page.Locator("#clear-range").SelectOptionAsync("0");
        var clearResponse = page.WaitForResponseAsync(response => response.Url.Contains("/searchpulse/settings/data", StringComparison.Ordinal) && response.Status == 204);
        await page.Locator("#clear-data").ClickAsync();
        await clearResponse;

        await page.GotoAsync(_host.OverviewUrl);
        await Expect(page.GetByText("No page views in this period.")).ToBeVisibleAsync();
        await context.DisposeAsync();
    }

    private async Task OpenBackofficeAsync(IPage page)
    {
        await page.GotoAsync(_host.BackofficeUrl);
        await page.WaitForURLAsync(new Regex("/umbraco/(login|section/)"), new() { Timeout = 30_000 });
        if (!page.Url.Contains("/umbraco/login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var loginControls = page.Locator("input");
        await Expect(loginControls).ToHaveCountAsync(2, new() { Timeout = 30_000 });
        var email = page.Locator("input").First;
        var password = page.Locator("input").Last;
        await email.FillAsync("admin@searchpulse.test");
        await password.FillAsync("SearchPulse-Integration-Only-2026!");
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForURLAsync(new Regex("/umbraco/section/"), new() { Timeout = 30_000 });
    }

    private sealed class SearchPulseBrowserHost
    {
        private readonly string _repositoryRoot = FindRepositoryRoot();
        private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), "SearchPulse.BrowserTests", Guid.NewGuid().ToString("N"));
        private Process? _process;

        public string BaseUrl { get; private set; } = string.Empty;

        public string ReviewUrl => $"{BaseUrl}/searchpulse-review";

        public string BackofficeUrl => $"{BaseUrl}/umbraco";

        public string OverviewUrl => $"{BaseUrl}/umbraco/section/searchpulse/overview";

        public string SettingsUrl => $"{BaseUrl}/umbraco/section/searchpulse/view/settings";

        public async Task StartAsync()
        {
            Directory.CreateDirectory(_temporaryDirectory);
            var port = GetUnusedPort();
            BaseUrl = $"https://127.0.0.1:{port}";
            var integrationDirectory = Path.Combine(_repositoryRoot, "tests", "SearchPulse.Umbraco.Integration");
            var executable = Path.Combine(integrationDirectory, "bin", "Release", "net10.0", "SearchPulse.Umbraco.Integration.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException("Build the SearchPulse integration host before running browser tests.", executable);
            }

            var startInfo = new ProcessStartInfo(executable)
            {
                WorkingDirectory = integrationDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
            startInfo.Environment["ConnectionStrings__umbracoDbDSN"] = $"Data Source={Path.Combine(_temporaryDirectory, "Umbraco.sqlite.db")};Cache=Shared;Foreign Keys=True;Pooling=True";
            startInfo.Environment["ConnectionStrings__umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite";

            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the SearchPulse integration host.");
            await WaitForHostAsync();
        }

        public async Task StopAsync()
        {
            if (_process is not null && !_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }

            for (var attempt = 0; attempt < 10 && Directory.Exists(_temporaryDirectory); attempt++)
            {
                try
                {
                    Directory.Delete(_temporaryDirectory, recursive: true);
                }
                catch (IOException) when (attempt < 9)
                {
                    await Task.Delay(250);
                }
            }
        }

        private async Task WaitForHostAsync()
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(2),
            };

            for (var attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    using var response = await client.GetAsync(ReviewUrl);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                    // Umbraco has not opened Kestrel yet.
                }
                catch (TaskCanceledException)
                {
                    // The host is still starting its unattended installation.
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            throw new TimeoutException($"The SearchPulse browser-test host did not start. {GetHostDiagnostics()}");
        }

        private string GetHostDiagnostics()
        {
            if (_process is null)
            {
                return "No host process was created.";
            }

            return _process.HasExited
                ? $"Host exited with code {_process.ExitCode}."
                : "Host did not become reachable within the timeout.";
        }

        private static int GetUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "SearchPulse.Umbraco.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the SearchPulse repository root.");
        }
    }
}
