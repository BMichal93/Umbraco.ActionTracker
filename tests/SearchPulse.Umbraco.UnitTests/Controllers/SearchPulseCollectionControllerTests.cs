using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SearchPulse.Umbraco.Consent;
using SearchPulse.Umbraco.Controllers;
using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.UnitTests.Controllers;

public sealed class SearchPulseCollectionControllerTests
{
    [Fact]
    public async Task CollectAsyncDoesNotExposeCollectionWhenDisabled()
    {
        var consentProvider = new StubConsentProvider(true);
        var store = new RecordingEventStore();
        var controller = CreateController(false, consentProvider, store, "https://website.test");

        var result = await controller.CollectAsync(CreatePageViewRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, consentProvider.CallCount);
        Assert.Empty(store.Events);
    }

    [Fact]
    public async Task CollectAsyncSilentlyDropsEventWhenConsentIsDenied()
    {
        var consentProvider = new StubConsentProvider(false);
        var store = new RecordingEventStore();
        var controller = CreateController(true, consentProvider, store, "https://website.test");

        var result = await controller.CollectAsync(CreatePageViewRequest(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(1, consentProvider.CallCount);
        Assert.Empty(store.Events);
    }

    [Fact]
    public async Task CollectAsyncStoresValidatedSameOriginEvent()
    {
        var consentProvider = new StubConsentProvider(true);
        var store = new RecordingEventStore();
        var controller = CreateController(true, consentProvider, store, "https://website.test");

        var result = await controller.CollectAsync(CreatePageViewRequest(), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        var recordedEvent = Assert.Single(store.Events);
        Assert.Equal(SearchPulseEventType.PageView, recordedEvent.Type);
        Assert.Equal("/services/seo", recordedEvent.Path);
    }

    [Fact]
    public async Task CollectAsyncReturnsServiceUnavailableWhenDurableQueueIsFull()
    {
        var consentProvider = new StubConsentProvider(true);
        var store = new RecordingEventStore(SearchPulseEventRecordResult.QueueFull);
        var controller = CreateController(true, consentProvider, store, "https://website.test");

        var result = await controller.CollectAsync(CreatePageViewRequest(), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
        Assert.Empty(store.Events);
    }

    [Fact]
    public async Task CollectAsyncRejectsCrossOriginRequestsBeforeConsentCheck()
    {
        var consentProvider = new StubConsentProvider(true);
        var store = new RecordingEventStore();
        var controller = CreateController(true, consentProvider, store, "https://untrusted.example");

        var result = await controller.CollectAsync(CreatePageViewRequest(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        Assert.Equal(0, consentProvider.CallCount);
        Assert.Empty(store.Events);
    }

    private static SearchPulseEventRequest CreatePageViewRequest() => new()
    {
        Type = "page-view",
        Path = "/services/seo",
    };

    private static SearchPulseCollectionController CreateController(
        bool enabled,
        StubConsentProvider consentProvider,
        RecordingEventStore eventStore,
        string origin)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("website.test");
        httpContext.Request.Headers.Origin = origin;

        return new SearchPulseCollectionController(
            new StubSettingsService(enabled),
            consentProvider,
            eventStore)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    private sealed class StubSettingsService(bool enabled) : ISearchPulseSettingsService
    {
        public bool IsEnabled() => enabled;

        public void SetEnabled(bool value) => enabled = value;
    }

    private sealed class StubConsentProvider(bool isAllowed) : IAnalyticsConsentProvider
    {
        public int CallCount { get; private set; }

        public ValueTask<bool> HasAnalyticsConsentAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult(isAllowed);
        }
    }

    private sealed class RecordingEventStore(SearchPulseEventRecordResult result = SearchPulseEventRecordResult.Accepted) : ISearchPulseEventStore
    {
        public List<SearchPulseEvent> Events { get; } = [];

        public Task<SearchPulseEventRecordResult> RecordAsync(
            SearchPulseEvent searchPulseEvent,
            CancellationToken cancellationToken = default)
        {
            if (result == SearchPulseEventRecordResult.Accepted)
            {
                Events.Add(searchPulseEvent);
            }

            return Task.FromResult(result);
        }

        public int GetPendingEventCount() => 0;
    }
}