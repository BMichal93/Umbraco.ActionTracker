using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace SearchPulse.BrowserTests;

[Collection("SearchPulse browser")]
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
        await Expect(page.Locator("#searchpulse-clear")).ToHaveCountAsync(0);
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

    [Fact]
    public async Task StressTestRecordsEveryConcurrentClick()
    {
        const int requestCount = 300;
        var path = $"/searchpulse-load/stress-{Guid.NewGuid():N}";
        using var client = CreateCollectorClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, requestCount)
            .Select(index => PostCustomActionAsync(client, path, $"stress-click-{index}")));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));
        await WaitForRecordedEventCountAsync(path, requestCount, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task PerformanceTestAcceptsConcurrentClicksWithinLatencyBudget()
    {
        const int requestCount = 120;
        const int maximumConcurrency = 20;
        var path = $"/searchpulse-load/performance-{Guid.NewGuid():N}";
        using var client = CreateCollectorClient();
        using var limiter = new SemaphoreSlim(maximumConcurrency, maximumConcurrency);

        var responses = await Task.WhenAll(Enumerable.Range(0, requestCount).Select(async index =>
        {
            await limiter.WaitAsync();
            try
            {
                return await PostCustomActionAsync(client, path, $"performance-click-{index}");
            }
            finally
            {
                limiter.Release();
            }
        }));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));
        var p95 = responses
            .Select(response => response.Duration)
            .OrderBy(duration => duration)
            .ElementAt((int)Math.Ceiling(requestCount * 0.95) - 1);
        Assert.True(p95 < TimeSpan.FromSeconds(2), $"Collector p95 was {p95.TotalMilliseconds:F0} ms.");
        await WaitForRecordedEventCountAsync(path, requestCount, TimeSpan.FromSeconds(20));
    }

    [Fact]
    public async Task RetentionArchivesExpiredRowsIntoDailyAggregates()
    {
        var occurredUtc = DateTime.UtcNow.Date.AddDays(-31).AddHours(12);
        await using (var connection = new SqliteConnection($"Data Source={_host.DatabasePath};Cache=Shared;Pooling=False"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO searchPulseEvent (occurredUtc, eventType, path, target) VALUES ($occurredUtc, $eventType, $path, NULL), ($occurredUtc, $eventType, $path, NULL)";
            command.Parameters.AddWithValue("$occurredUtc", occurredUtc);
            command.Parameters.AddWithValue("$eventType", "PageView");
            command.Parameters.AddWithValue("$path", "/searchpulse-retention/archive-check");
            await command.ExecuteNonQueryAsync();
        }

        using var client = CreateCollectorClient();
        using var response = await client.PostAsync("/searchpulse-test/purge", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var verificationConnection = new SqliteConnection($"Data Source={_host.DatabasePath};Mode=ReadOnly;Cache=Shared;Pooling=False");
        await verificationConnection.OpenAsync();
        await using var aggregateCommand = verificationConnection.CreateCommand();
        aggregateCommand.CommandText = "SELECT eventCount FROM searchPulseDailyAggregate WHERE occurredDateUtc = $occurredDateUtc AND eventType = $eventType AND path = $path";
        aggregateCommand.Parameters.AddWithValue("$occurredDateUtc", occurredUtc.Date);
        aggregateCommand.Parameters.AddWithValue("$eventType", "PageView");
        aggregateCommand.Parameters.AddWithValue("$path", "/searchpulse-retention/archive-check");
        var eventCount = await aggregateCommand.ExecuteScalarAsync();

        Assert.Equal(2L, Convert.ToInt64(eventCount, CultureInfo.InvariantCulture));
        await using var rawCommand = verificationConnection.CreateCommand();
        rawCommand.CommandText = "SELECT COUNT(*) FROM searchPulseEvent WHERE path = $path";
        rawCommand.Parameters.AddWithValue("$path", "/searchpulse-retention/archive-check");
        Assert.Equal(0L, Convert.ToInt64(await rawCommand.ExecuteScalarAsync(), CultureInfo.InvariantCulture));

        var browser = Assert.IsAssignableFrom<IBrowser>(_browser);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await OpenBackofficeAsync(page);
        await page.GotoAsync(_host.OverviewUrl);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "SearchPulse" })).ToBeVisibleAsync();
        await page.Locator("#searchpulse-range").SelectOptionAsync("0");
        await Expect(page.Locator(".searchpulse-metric-value").First).ToHaveTextAsync("2");

        await page.GotoAsync(_host.SettingsUrl);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Settings" })).ToBeVisibleAsync();
        page.Dialog += (_, dialog) => dialog.AcceptAsync();
        await page.Locator("#clear-range").SelectOptionAsync("0");
        var clearResponse = page.WaitForResponseAsync(response => response.Url.Contains("/searchpulse/settings/data", StringComparison.Ordinal) && response.Status == 204);
        await page.Locator("#clear-data").ClickAsync();
        await clearResponse;
        await context.DisposeAsync();

        await using var clearedAggregateCommand = verificationConnection.CreateCommand();
        clearedAggregateCommand.CommandText = "SELECT COUNT(*) FROM searchPulseDailyAggregate";
        Assert.Equal(0L, Convert.ToInt64(await clearedAggregateCommand.ExecuteScalarAsync(), CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task HostedServicesStopWithoutWorkerFailure()
    {
        await _host.StopGracefullyAsync();

        Assert.DoesNotContain("BackgroundService failed", _host.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", _host.Output, StringComparison.Ordinal);
    }
    private HttpClient CreateCollectorClient()
    {
        var cookieContainer = new CookieContainer();
        cookieContainer.Add(new Uri(_host.BaseUrl), new System.Net.Cookie("SearchPulseIntegrationConsent", "yes", "/"));
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(_host.BaseUrl),
            Timeout = TimeSpan.FromSeconds(15),
        };
    }

    private async Task<CollectorResponse> PostCustomActionAsync(HttpClient client, string path, string target)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/searchpulse/collect")
        {
            Content = JsonContent.Create(new
            {
                type = "custom-action",
                path,
                target,
            }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        };
        request.Headers.Add("Origin", _host.BaseUrl);

        var stopwatch = Stopwatch.StartNew();
        using var response = await client.SendAsync(request);
        stopwatch.Stop();
        return new CollectorResponse(response.StatusCode, stopwatch.Elapsed);
    }

    private async Task WaitForRecordedEventCountAsync(string path, int expectedCount, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        var actualCount = 0;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={_host.DatabasePath};Mode=ReadOnly;Cache=Shared;Pooling=False");
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM searchPulseEvent WHERE path = $path";
                command.Parameters.AddWithValue("$path", path);
                actualCount = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
                if (actualCount == expectedCount)
                {
                    return;
                }
            }
            catch (SqliteException)
            {
                // SQLite may briefly hold the database file while the queue worker commits a batch.
            }

            await Task.Delay(200);
        }

        Assert.Equal(expectedCount, actualCount);
    }

    private sealed record CollectorResponse(HttpStatusCode StatusCode, TimeSpan Duration);

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
        private readonly object _outputLock = new();
        private readonly StringBuilder _output = new();
        private Process? _process;

        public string BaseUrl { get; private set; } = string.Empty;

        public string DatabasePath => Path.Combine(_temporaryDirectory, "Umbraco.sqlite.db");

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
            startInfo.Environment["ConnectionStrings__umbracoDbDSN"] = $"Data Source={DatabasePath};Cache=Shared;Foreign Keys=True;Pooling=True";
            startInfo.Environment["ConnectionStrings__umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite";

            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the SearchPulse integration host.");
            _process.OutputDataReceived += (_, eventArgs) => AppendOutput(eventArgs.Data);
            _process.ErrorDataReceived += (_, eventArgs) => AppendOutput(eventArgs.Data);
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            await WaitForHostAsync();
        }

        public string Output { get { lock (_outputLock) { return _output.ToString(); } } }

        public async Task StopGracefullyAsync()
        {
            if (_process is null || _process.HasExited)
            {
                throw new InvalidOperationException("The SearchPulse integration host is not running.");
            }

            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(15),
            };
            using var response = await client.PostAsync("/searchpulse-test/stop", content: null);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                throw new InvalidOperationException($"The graceful-stop endpoint returned {(int)response.StatusCode}.");
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _process.WaitForExitAsync(timeout.Token);
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

        private void AppendOutput(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _output.AppendLine(value);
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
