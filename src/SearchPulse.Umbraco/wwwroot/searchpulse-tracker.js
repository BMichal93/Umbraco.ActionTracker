const searchPulseEndpoint = "/searchpulse/collect";
const scrollEvents = [
    [25, "scroll-25"],
    [50, "scroll-50"],
    [75, "scroll-75"],
];
const retryDelays = [250, 1000, 3000];

let started = false;
let exitTracked = false;
const trackedScrollMilestones = new Set();

function currentPath() {
    return window.location.pathname;
}

async function send(type, target) {
    const payload = { type, path: currentPath() };
    if (target) {
        payload.target = target;
    }

    for (let attempt = 0; attempt <= retryDelays.length; attempt++) {
        try {
            const response = await fetch(searchPulseEndpoint, {
                method: "POST",
                credentials: "same-origin",
                keepalive: true,
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            });
            if (response.status !== 503 || attempt === retryDelays.length) {
                return;
            }
        } catch {
            if (attempt === retryDelays.length) {
                return;
            }
        }

        await new Promise(resolve => window.setTimeout(resolve, retryDelays[attempt]));
    }
}

function trackExit() {
    if (!exitTracked) {
        exitTracked = true;
        void send("page-exit");
    }
}

function trackScroll() {
    const scrollableHeight = document.documentElement.scrollHeight - window.innerHeight;
    if (scrollableHeight <= 0) {
        return;
    }

    const percentage = ((window.scrollY + window.innerHeight) / document.documentElement.scrollHeight) * 100;
    for (const [milestone, eventType] of scrollEvents) {
        if (percentage >= milestone && !trackedScrollMilestones.has(milestone)) {
            trackedScrollMilestones.add(milestone);
            void send(eventType);
        }
    }
}

function trackLinkClick(event) {
    if (!(event.target instanceof Element)) {
        return;
    }

    const link = event.target.closest("a[href]");
    if (!link) {
        return;
    }

    const destination = new URL(link.href, window.location.href);
    if (destination.origin !== window.location.origin) {
        void send("external-link-click", destination.hostname);
        return;
    }

    if (link.hasAttribute("download")) {
        void send("download-click", "download");
    }
}

function start() {
    if (started) {
        return;
    }

    started = true;
    void send("page-view");
    window.addEventListener("scroll", trackScroll, { passive: true });
    window.addEventListener("pagehide", trackExit, { once: true });
    document.addEventListener("click", trackLinkClick);
}

window.SearchPulse = Object.freeze({
    start,
    trackAction(target) {
        if (started) {
            void send("custom-action", target);
        }
    },
});

if (window.SearchPulseConsent === true) {
    start();
}