const searchPulseEndpoint = "/searchpulse/collect";
const scrollEvents = [[25, "scroll-25"], [50, "scroll-50"], [75, "scroll-75"]];
const retryDelays = [250, 1000, 3000];
const engagementMilestones = [15, 30, 60, 180];

let started = false;
let consentGranted = false;
let exitTracked = false;
let spaNavigationStarted = false;
let engagementTimer = 0;
let activeSeconds = 0;
let meaningfulEngagement = false;
const trackedScrollMilestones = new Set();
const trackedEngagementMilestones = new Set();
const listeners = [];
const originalHistoryMethods = {};

function currentPath() { return window.location.pathname; }
function isSafeToken(value) { return typeof value === "string" && /^[a-z0-9][a-z0-9._-]*$/i.test(value) && value.length <= 64 ? value.toLowerCase() : undefined; }
function isSafeDomain(value) { return typeof value === "string" && /^[a-z0-9][a-z0-9.-]*\.[a-z]{2,}$/i.test(value) && value.length <= 64 ? value.toLowerCase() : undefined; }

function getContext() {
    const context = {};
    const contentKey = document.documentElement.dataset.searchpulseContentKey || window.SearchPulseContentKey;
    const safeContentKey = isSafeToken(contentKey);
    if (safeContentKey) context.contentKey = safeContentKey;
    try {
        if (document.referrer) {
            const referrer = new URL(document.referrer);
            if (referrer.origin !== window.location.origin) context.referrerDomain = isSafeDomain(referrer.hostname);
        }
    } catch { /* invalid referrers are ignored */ }
    try {
        const params = new URLSearchParams(window.location.search);
        context.utmSource = isSafeToken(params.get("utm_source"));
        context.utmMedium = isSafeToken(params.get("utm_medium"));
        context.utmCampaign = isSafeToken(params.get("utm_campaign"));
    } catch { /* malformed URLs are ignored */ }
    for (const key of Object.keys(context)) if (!context[key]) delete context[key];
    return context;
}

function addListener(target, event, handler, options) {
    target.addEventListener(event, handler, options);
    listeners.push(() => target.removeEventListener(event, handler, options));
}

async function send(type, target) {
    if (!started || !consentGranted) return;
    const payload = { type, path: currentPath(), ...getContext() };
    if (target) payload.target = target;
    if (window.SearchPulseDataLayerExport === true) {
        window.dataLayer = window.dataLayer || [];
        window.dataLayer.push({ event: `searchpulse_${type}`, searchpulse: { ...payload } });
    }
    for (let attempt = 0; attempt <= retryDelays.length; attempt++) {
        try {
            const response = await fetch(searchPulseEndpoint, { method: "POST", credentials: "same-origin", keepalive: true, headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
            if (response.status !== 503 || attempt === retryDelays.length) return;
        } catch { if (attempt === retryDelays.length) return; }
        await new Promise(resolve => window.setTimeout(resolve, retryDelays[attempt]));
    }
}

function trackExit() {
    if (exitTracked || !started) return;
    exitTracked = true;
    if (activeSeconds < 10 && !meaningfulEngagement) void send("low-engagement-exit", "under-10s");
    void send("page-exit");
}
function trackScroll() {
    const height = document.documentElement.scrollHeight - window.innerHeight;
    if (height <= 0) return;
    meaningfulEngagement = true;
    const percentage = ((window.scrollY + window.innerHeight) / document.documentElement.scrollHeight) * 100;
    for (const [milestone, eventType] of scrollEvents) if (percentage >= milestone && !trackedScrollMilestones.has(milestone)) { trackedScrollMilestones.add(milestone); void send(eventType); }
}
function trackLinkClick(event) {
    if (!(event.target instanceof Element)) return;
    meaningfulEngagement = true;
    const action = event.target.closest("[data-searchpulse-action]");
    if (action?.dataset.searchpulseAction) void send("custom-action", action.dataset.searchpulseAction);
    const link = event.target.closest("a[href]");
    if (!link) return;
    try {
        const destination = new URL(link.href, window.location.href);
        if (link.hasAttribute("download")) { void send("download-click", destination.pathname); return; }
        if (destination.origin !== window.location.origin) void send("external-link-click", destination.hostname);
    } catch { /* invalid links are ignored */ }
}
function trackFormSubmit(event) { if (event.target instanceof HTMLFormElement && event.target.dataset.searchpulseForm) { meaningfulEngagement = true; void send("form-submit", event.target.dataset.searchpulseForm); } }
function trackVideoPlay(event) { if (event.target instanceof HTMLVideoElement && event.target.dataset.searchpulseVideo) { meaningfulEngagement = true; void send("video-play", event.target.dataset.searchpulseVideo); } }
function updateEngagement() {
    if (document.visibilityState === "hidden") return;
    activeSeconds += 5;
    for (const milestone of engagementMilestones) if (activeSeconds >= milestone && !trackedEngagementMilestones.has(milestone)) { trackedEngagementMilestones.add(milestone); void send("active-engagement", `seconds-${milestone}`); }
}
function trackPageView() {
    if (!started) return;
    exitTracked = false; activeSeconds = 0; meaningfulEngagement = false; trackedScrollMilestones.clear(); trackedEngagementMilestones.clear();
    void send("page-view");
}
function startSpaNavigationTracking() {
    if (spaNavigationStarted) return;
    spaNavigationStarted = true;
    for (const method of ["pushState", "replaceState"]) {
        originalHistoryMethods[method] = window.history[method];
        window.history[method] = function (...args) { const previousPath = currentPath(); const result = originalHistoryMethods[method].apply(this, args); if (currentPath() !== previousPath) trackPageView(); return result; };
    }
    addListener(window, "popstate", trackPageView);
}
function stopSpaNavigationTracking() {
    for (const method of ["pushState", "replaceState"]) if (originalHistoryMethods[method]) window.history[method] = originalHistoryMethods[method];
    spaNavigationStarted = false;
}
function toExternalHost(value) { try { return new URL(value, window.location.href).hostname; } catch { return value; } }
function toDownloadPath(value) { try { return new URL(value, window.location.href).pathname; } catch { return value; } }
function start() {
    consentGranted = true;
    if (started) return;
    started = true;
    trackPageView();
    addListener(window, "scroll", trackScroll, { passive: true });
    addListener(window, "pagehide", trackExit, { once: true });
    addListener(document, "click", trackLinkClick);
    addListener(document, "submit", trackFormSubmit);
    addListener(document, "play", trackVideoPlay, true);
    engagementTimer = window.setInterval(updateEngagement, 5000);
    startSpaNavigationTracking();
}
function stop() {
    consentGranted = false;
    if (!started) return;
    trackExit();
    started = false;
    for (const remove of listeners.splice(0)) remove();
    if (engagementTimer) window.clearInterval(engagementTimer);
    engagementTimer = 0;
    stopSpaNavigationTracking();
}

window.SearchPulse = Object.freeze({
    start, stop, setConsent: value => value ? start() : stop(), trackPageView,
    trackAction: target => { meaningfulEngagement = true; void send("custom-action", target); },
    trackExternalLink: target => { meaningfulEngagement = true; void send("external-link-click", toExternalHost(target)); },
    trackDownload: target => { meaningfulEngagement = true; void send("download-click", toDownloadPath(target)); },
    trackFormSubmit: target => { meaningfulEngagement = true; void send("form-submit", target); },
    trackFormSuccess: target => { meaningfulEngagement = true; void send("form-success", target); },
    trackSiteSearch: target => { meaningfulEngagement = true; void send("site-search", target); },
    trackVideoPlay: target => { meaningfulEngagement = true; void send("video-play", target); },
});

if (window.SearchPulseConsent === true) start();
