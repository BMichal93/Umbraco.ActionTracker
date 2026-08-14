using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace SearchPulse.BrowserTests;

[Collection("SearchPulse browser")]
public sealed class SearchPulseDemoSiteBrowserTests : IAsyncLifetime
{
    private readonly SearchPulseDemoHost _host = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        await _host.StartAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
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
    public async Task DemoSiteExposesAndRecordsEverySupportedSignal()
    {
        var browser = Assert.IsAssignableFrom<IBrowser>(_browser);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_host.BaseUrl);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "SearchPulse demo home" })).ToBeVisibleAsync();
        await Expect(page.GetByText("Analytics consent is disabled.")).ToBeVisibleAsync();
        await Expect(page.Locator("a[href='/services']")).ToBeVisibleAsync();
        await Expect(page.Locator("a[href='/contact']")).ToBeVisibleAsync();
        await Expect(page.Locator("a[download]")).ToBeVisibleAsync();
        await Expect(page.Locator("a[href='https://umbraco.com']")).ToBeVisibleAsync();

        await page.Locator("#consent-button").ClickAsync();
        await page.WaitForURLAsync("**/");
        await Expect(page.GetByText("Analytics consent is enabled.")).ToBeVisibleAsync();
        await page.Locator("[data-searchpulse-action='book-consultation']").ClickAsync();
        await page.Locator("a[href='https://umbraco.com']").ClickAsync();
        var download = page.WaitForDownloadAsync();
        await page.Locator("a[download]").ClickAsync();
        await download;

        await page.Locator("a[href='/services']").ClickAsync();
        await page.WaitForURLAsync("**/services");
        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await page.WaitForTimeoutAsync(500);
        await page.Locator("[data-searchpulse-action='request-pricing']").ClickAsync();
        await page.Locator("a[href='/contact']").ClickAsync();
        await page.WaitForURLAsync("**/contact");
        await page.Locator("[data-searchpulse-action='newsletter-signup']").ClickAsync();

        await WaitForSupportedSignalsAsync(TimeSpan.FromSeconds(30));
        await context.DisposeAsync();
    }

    private async Task WaitForSupportedSignalsAsync(TimeSpan timeout)
    {
        var expectedTypes = new[]
        {
            "PageView",
            "PageExit",
            "Scroll25",
            "Scroll50",
            "Scroll75",
            "ExternalLinkClick",
            "DownloadClick",
            "CustomAction",
        };
        var expectedTargets = new[]
        {
            "book-consultation",
            "request-pricing",
            "newsletter-signup",
            "umbraco.com",
            "/downloads/searchpulse-demo-guide.txt",
        };
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={_host.DatabasePath};Mode=ReadOnly;Cache=Shared;Pooling=False");
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT eventType || '|' || COALESCE(target, '') FROM searchPulseEvent";
                await using var reader = await command.ExecuteReaderAsync();
                var signals = new HashSet<string>(StringComparer.Ordinal);
                while (await reader.ReadAsync())
                {
                    signals.Add(reader.GetString(0));
                }

                if (expectedTypes.All(type => signals.Any(signal => signal.StartsWith(type + "|", StringComparison.Ordinal)))
                    && expectedTargets.All(target => signals.Any(signal => signal.EndsWith("|" + target, StringComparison.Ordinal))))
                {
                    return;
                }
            }
            catch (SqliteException)
            {
                // SQLite can briefly hold the file while the durable queue worker commits a batch.
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("The demo did not persist every supported SearchPulse signal.");
    }

    private sealed class SearchPulseDemoHost
    {
        private readonly string _repositoryRoot = FindRepositoryRoot();
        private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), "SearchPulse.DemoSite.BrowserTests", Guid.NewGuid().ToString("N"));
        private Process? _process;

        public string BaseUrl { get; private set; } = string.Empty;

        public string DatabasePath => Path.Combine(_temporaryDirectory, "Umbraco.sqlite.db");

        public async Task StartAsync()
        {
            Directory.CreateDirectory(_temporaryDirectory);
            var port = GetUnusedPort();
            BaseUrl = $"https://127.0.0.1:{port}";
            var demoDirectory = Path.Combine(_repositoryRoot, "samples", "SearchPulse.DemoSite");
            var executable = Path.Combine(demoDirectory, "bin", "Release", "net10.0", "SearchPulse.DemoSite.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException("Build the SearchPulse demo site before running browser tests.", executable);
            }

            var startInfo = new ProcessStartInfo(executable)
            {
                WorkingDirectory = demoDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
            startInfo.Environment["ConnectionStrings__umbracoDbDSN"] = $"Data Source={DatabasePath};Cache=Shared;Foreign Keys=True;Pooling=True";
            startInfo.Environment["ConnectionStrings__umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite";
            startInfo.Environment["SearchPulseDemo__DataDirectory"] = Path.Combine(_temporaryDirectory, "data-protection");

            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the SearchPulse demo site.");
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
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
            for (var attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    using var response = await client.GetAsync(BaseUrl);
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

            throw new TimeoutException("The SearchPulse demo site did not start.");
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