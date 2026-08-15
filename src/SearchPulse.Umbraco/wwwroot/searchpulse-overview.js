import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";

const searchPulseManagementApi = "/umbraco/management/api/v1/searchpulse";
const bearerSecurity = [{ scheme: "bearer", type: "http" }];

class SearchPulseOverviewElement extends UmbElementMixin(HTMLElement) {
    constructor() {
        super();
        this.rangeDays = 30;
        this.sort = "count";
    }

    connectedCallback() {
        this.renderLoading();
        void this.load();
    }

    async load() {
        try {
            const query = new URLSearchParams({
                rangeDays: String(this.rangeDays),
                sort: this.sort,
            });
            const { data, error } = await umbHttpClient.get({
                url: searchPulseManagementApi + "/overview?" + query.toString(),
                security: bearerSecurity,
            });
            if (error) {
                throw new Error("The overview could not be loaded.");
            }

            this.render(data);
        } catch {
            this.renderError();
        }
    }

    renderLoading() {
        this.innerHTML = "<uui-box><p class=\"searchpulse-loading\">Loading SearchPulse...</p></uui-box>";
    }

    render(overview) {
        const topPages = overview.topPages.length === 0
            ? emptyRow("No page views in this period.", 2)
            : overview.topPages.map((page, index) =>
                "<tr><td>" + (index + 1) + "</td><td><code>" + escapeHtml(page.path) + "</code></td><td class=\"searchpulse-number\">" + page.pageViews + "</td></tr>").join("");
        const interactions = overview.popularInteractions.length === 0
            ? emptyRow("No interactions in this period.", 2)
            : overview.popularInteractions.map((interaction, index) =>
                "<tr><td>" + (index + 1) + "</td><td>" + formatInteraction(interaction) + "</td><td class=\"searchpulse-number\">" + interaction.interactions + "</td></tr>").join("");
        const goals = (overview.goals || []).length === 0 ? emptyRow("No goals configured.", 3) : overview.goals.map(goal => "<tr><td>" + escapeHtml(goal.name) + "</td><td>" + escapeHtml(goal.eventType) + " / <code>" + escapeHtml(goal.target) + "</code></td><td class=\"searchpulse-number\">" + goal.completions + "</td></tr>").join("");
        const acquisition = (overview.acquisition || []).length === 0 ? emptyRow("No acquisition dimensions recorded.", 4) : overview.acquisition.map(item => "<tr><td>" + escapeHtml(item.source) + "</td><td>" + escapeHtml(item.medium) + "</td><td>" + escapeHtml(item.campaign) + "</td><td class=\"searchpulse-number\">" + item.interactions + "</td></tr>").join("");
        const content = (overview.contentPerformance || []).length === 0 ? emptyRow("No content attribution recorded.", 3) : overview.contentPerformance.map(item => "<tr><td><code>" + escapeHtml(item.contentKey) + "</code></td><td class=\"searchpulse-number\">" + item.pageViews + "</td><td class=\"searchpulse-number\">" + item.interactions + "</td></tr>").join("");
        const status = overview.isEnabled
            ? "<span class=\"searchpulse-status searchpulse-status-on\">Tracking on</span>"
            : "<span class=\"searchpulse-status searchpulse-status-off\">Tracking off</span>";

        this.innerHTML = [
            "<style>",
            ":host { display: block; max-width: 1240px; margin: 0 auto; padding: var(--uui-size-space-5, 24px); box-sizing: border-box; color: var(--uui-color-text, #1f2937); }",
            ".searchpulse-header { display: flex; justify-content: space-between; gap: 24px; align-items: flex-start; margin-bottom: 24px; }",
            ".searchpulse-title { margin: 0 0 6px; font-size: 28px; line-height: 1.2; }",
            ".searchpulse-subtitle, .searchpulse-meta { margin: 0; color: var(--uui-color-text-alt, #6b7280); }",
            ".searchpulse-meta { font-size: 13px; white-space: nowrap; }",
            ".searchpulse-toolbar { display: flex; flex-wrap: wrap; align-items: end; gap: 12px; padding: 16px; margin-bottom: 20px; background: var(--uui-color-surface, #fff); border: 1px solid var(--uui-color-border, #d1d5db); border-radius: 8px; }",
            ".searchpulse-control { display: grid; gap: 6px; min-width: 160px; font-size: 13px; font-weight: 600; }",
            ".searchpulse-control select { min-height: 36px; padding: 0 10px; border: 1px solid var(--uui-color-border, #cbd5e1); border-radius: 4px; background: #fff; color: inherit; }",

            ".searchpulse-metrics { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 12px; margin-bottom: 20px; }",
            ".searchpulse-metric { padding: 18px; border: 1px solid var(--uui-color-border, #dbe3ee); border-radius: 8px; background: linear-gradient(145deg, #fff, #f8fafc); }",
            ".searchpulse-metric-label { display: block; color: var(--uui-color-text-alt, #64748b); font-size: 13px; }",
            ".searchpulse-metric-value { display: block; margin-top: 8px; font-size: 30px; font-weight: 700; line-height: 1; }",
            ".searchpulse-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 20px; }",
            ".searchpulse-section { min-width: 0; border: 1px solid var(--uui-color-border, #dbe3ee); border-radius: 8px; background: #fff; overflow: hidden; }",
            ".searchpulse-section-header { padding: 18px 18px 12px; border-bottom: 1px solid var(--uui-color-border, #e5e7eb); }",
            ".searchpulse-section-header h2 { margin: 0; font-size: 17px; }",
            ".searchpulse-table { width: 100%; border-collapse: collapse; }",
            ".searchpulse-table th, .searchpulse-table td { padding: 12px 18px; border-bottom: 1px solid var(--uui-color-border, #edf0f4); text-align: left; font-size: 14px; }",
            ".searchpulse-table th { color: var(--uui-color-text-alt, #64748b); font-size: 12px; font-weight: 700; letter-spacing: .03em; text-transform: uppercase; }",
            ".searchpulse-table tr:last-child td { border-bottom: 0; }",
            ".searchpulse-number { text-align: right !important; font-variant-numeric: tabular-nums; font-weight: 700; }",
            ".searchpulse-empty { color: var(--uui-color-text-alt, #64748b); text-align: center !important; padding: 28px !important; }",
            ".searchpulse-status { display: inline-flex; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 700; }",
            ".searchpulse-status-on { background: #dcfce7; color: #166534; } .searchpulse-status-off { background: #fee2e2; color: #991b1b; }",
            ".searchpulse-loading { padding: 16px; }",
            "@media (max-width: 960px) { .searchpulse-metrics { grid-template-columns: repeat(3, minmax(0, 1fr)); } }",
            "@media (max-width: 680px) { :host { padding: 16px; } .searchpulse-header { display: block; } .searchpulse-meta { margin-top: 12px; white-space: normal; } .searchpulse-toolbar { align-items: stretch; } .searchpulse-control { flex: 1 1 100%; } .searchpulse-metrics, .searchpulse-grid { grid-template-columns: 1fr; } }",
            "</style>",
            "<section aria-label=\"SearchPulse overview\">",
            "<header class=\"searchpulse-header\"><div><h1 class=\"searchpulse-title\">SearchPulse</h1><p class=\"searchpulse-subtitle\">Anonymous engagement signals for " + escapeHtml(overview.rangeLabel) + ".</p></div><div>" + status + "<p class=\"searchpulse-meta\">Updated " + formatUpdatedAt(overview.generatedAtUtc) + "</p></div></header>",
            "<div class=\"searchpulse-toolbar\"><label class=\"searchpulse-control\">Time range<select id=\"searchpulse-range\"><option value=\"1\">Last 24 hours</option><option value=\"7\">Last 7 days</option><option value=\"30\">Last 30 days</option><option value=\"90\">Last 90 days</option><option value=\"0\">All time</option></select></label><label class=\"searchpulse-control\">Sort tables by<select id=\"searchpulse-sort\"><option value=\"count\">Highest count</option><option value=\"name\">Name</option></select></label><uui-button id=\"searchpulse-refresh\" look=\"outline\">Refresh</uui-button></div>",
            metrics(overview.totals),
            "<div class=\"searchpulse-grid\"><section class=\"searchpulse-section\"><header class=\"searchpulse-section-header\"><h2>Most viewed pages</h2></header><table class=\"searchpulse-table\"><thead><tr><th>#</th><th>Page</th><th class=\"searchpulse-number\">Views</th></tr></thead><tbody>" + topPages + "</tbody></table></section>",
            "<section class=\"searchpulse-section\"><header class=\"searchpulse-section-header\"><h2>Popular interactions</h2></header><table class=\"searchpulse-table\"><thead><tr><th>#</th><th>Interaction</th><th class=\"searchpulse-number\">Count</th></tr></thead><tbody>" + interactions + "</tbody></table></section></div>",
            "<div class=\"searchpulse-grid\"><section class=\"searchpulse-section\"><header class=\"searchpulse-section-header\"><h2>Goals</h2></header><table class=\"searchpulse-table\"><thead><tr><th>Name</th><th>Signal</th><th class=\"searchpulse-number\">Completions</th></tr></thead><tbody>" + goals + "</tbody></table></section><section class=\"searchpulse-section\"><header class=\"searchpulse-section-header\"><h2>Acquisition</h2></header><table class=\"searchpulse-table\"><thead><tr><th>Source</th><th>Medium</th><th>Campaign</th><th class=\"searchpulse-number\">Events</th></tr></thead><tbody>" + acquisition + "</tbody></table></section></div>",
            "<section class=\"searchpulse-section\"><header class=\"searchpulse-section-header\"><h2>Content attribution</h2></header><table class=\"searchpulse-table\"><thead><tr><th>Content key</th><th class=\"searchpulse-number\">Views</th><th class=\"searchpulse-number\">Interactions</th></tr></thead><tbody>" + content + "</tbody></table></section>",
            "</section>",
        ].join("");

        this.querySelector("#searchpulse-range").value = String(this.rangeDays);
        this.querySelector("#searchpulse-sort").value = this.sort;
        this.querySelector("#searchpulse-range").addEventListener("change", (event) => {
            this.rangeDays = Number(event.target.value);
            void this.load();
        });
        this.querySelector("#searchpulse-sort").addEventListener("change", (event) => {
            this.sort = event.target.value;
            void this.load();
        });
        this.querySelector("#searchpulse-refresh").addEventListener("click", () => void this.load());

    }

    renderError(message = "We could not load the overview. Refresh the page, or check that SearchPulse is installed and the backoffice user has access.") {
        this.innerHTML = "<uui-box headline=\"SearchPulse\"><p>" + escapeHtml(message) + "</p></uui-box>";
    }
}

function metrics(totals) {
    const values = [
        ["Page views", totals.pageViews],
        ["Exits", totals.exits],
        ["Reached 25%", totals.reached25Percent],
        ["Reached 50%", totals.reached50Percent],
        ["Reached 75%", totals.reached75Percent],
    ];
    return '<div class="searchpulse-metrics">' + values.map(([label, value]) =>
        '<div class="searchpulse-metric"><span class="searchpulse-metric-label">' + label + '</span><strong class="searchpulse-metric-value">' + value + '</strong></div>').join("") + "</div>";
}

function emptyRow(message, columns) {
    return "<tr><td class=\"searchpulse-empty\" colspan=\"" + columns + "\">" + message + "</td></tr>";
}

function formatInteraction(interaction) {
    const target = escapeHtml(interaction.target || "");
    switch (interaction.eventType) {
        case "ExternalLinkClick":
            return "External link to <code>" + target + "</code>";
        case "DownloadClick":
            return "Download <code>" + target + "</code>";
        case "CustomAction":
            return "Custom action: <code>" + target + "</code>";
        case "FormSubmit":
            return "Form submitted: <code>" + target + "</code>";
        case "VideoPlay":
            return "Video played: <code>" + target + "</code>";
        default:
            return "Interaction";
    }
}

function formatUpdatedAt(value) {
    const date = new Date(value);
    return Number.isNaN(date.valueOf()) ? "just now" : escapeHtml(date.toLocaleString());
}

function escapeHtml(value) {
    const element = document.createElement("span");
    element.textContent = value;
    return element.innerHTML;
}

customElements.define("searchpulse-overview", SearchPulseOverviewElement);
export default SearchPulseOverviewElement;
