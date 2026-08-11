const searchPulseManagementApi = "/umbraco/management/api/v1/searchpulse";

class SearchPulseOverviewElement extends HTMLElement {
    connectedCallback() {
        this.renderLoading();
        void this.load();
    }

    async load() {
        try {
            const response = await fetch(`${searchPulseManagementApi}/overview`, {
                credentials: "same-origin",
            });
            if (!response.ok) {
                throw new Error("The overview could not be loaded.");
            }

            this.render(await response.json());
        } catch {
            this.renderError();
        }
    }

    renderLoading() {
        this.innerHTML = "<uui-box><p>Loading SearchPulse…</p></uui-box>";
    }

    render(overview) {
        const status = overview.isEnabled ? "Tracking is on" : "Tracking is off";
        const guidance = overview.isEnabled
            ? "Signals appear after your existing consent setup starts the tracker."
            : "Turn it on in Settings when your consent setup is ready.";
        const topPages = overview.topPages.length === 0
            ? "<p>No signals yet. <a href=\"https://github.com/BMichal93/searchpulse-for-umbraco#installation-and-setup\">Complete the consent and tracker setup</a>, then return here.</p>"
            : `<ul>${overview.topPages.map((page) => `<li><code>${escapeHtml(page.path)}</code> — ${page.pageViews} views</li>`).join("")}</ul>`;
        const popularInteractions = overview.popularInteractions.length === 0
            ? "<p>No interactions yet.</p>"
            : `<ul>${overview.popularInteractions.map((interaction) => `<li>${formatInteraction(interaction)} &mdash; ${interaction.interactions} interactions</li>`).join("")}</ul>`;

        this.innerHTML = `
            <uui-box headline="SearchPulse — last 30 days">
                <p><strong>${status}</strong><br>${guidance}</p>
                <p><small>Updated ${formatUpdatedAt(overview.generatedAtUtc)}</small></p>
                <dl>
                    <dt>Page views</dt><dd>${overview.totals.pageViews}</dd>
                    <dt>Exits</dt><dd>${overview.totals.exits}</dd>
                    <dt>Reached 25%</dt><dd>${overview.totals.reached25Percent}</dd>
                    <dt>Reached 50%</dt><dd>${overview.totals.reached50Percent}</dd>
                    <dt>Reached 75%</dt><dd>${overview.totals.reached75Percent}</dd>
                </dl>
                <h3>Most viewed pages</h3>
                ${topPages}
                <h3>Popular interactions</h3>
                ${popularInteractions}
            </uui-box>`;
    }

    renderError() {
        this.innerHTML = "<uui-box headline=\"SearchPulse\"><p>We could not load the overview. Refresh the page, or check that SearchPulse is installed and the backoffice user has access.</p></uui-box>";
    }
}

function formatInteraction(interaction) {
    const target = escapeHtml(interaction.target || "");
    switch (interaction.eventType) {
        case "ExternalLinkClick":
            return `External link to <code>${target}</code>`;
        case "DownloadClick":
            return "Download";
        case "CustomAction":
            return `Custom action: <code>${target}</code>`;
        default:
            return "Interaction";
    }
}

function formatUpdatedAt(value) {
    const date = new Date(value);
    return Number.isNaN(date.valueOf())
        ? "just now"
        : escapeHtml(date.toLocaleString());
}
function escapeHtml(value) {
    const element = document.createElement("span");
    element.textContent = value;
    return element.innerHTML;
}

customElements.define("searchpulse-overview", SearchPulseOverviewElement);
