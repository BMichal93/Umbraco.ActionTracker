import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";

const searchPulseSettingsApi = "/umbraco/management/api/v1/searchpulse/settings";
const bearerSecurity = [{ scheme: "bearer", type: "http" }];

class SearchPulseSettingsElement extends UmbElementMixin(HTMLElement) {
    connectedCallback() {
        this.renderLoading();
        void this.load();
    }

    async load() {
        try {
            const { data, error } = await umbHttpClient.get({
                url: searchPulseSettingsApi,
                security: bearerSecurity,
            });
            if (error) {
                throw new Error("The settings could not be loaded.");
            }

            this.render(data);
        } catch {
            this.renderError();
        }
    }

    renderLoading() {
        this.innerHTML = "<uui-box><p class=\"searchpulse-loading\">Loading SearchPulse...</p></uui-box>";
    }

    render(settings) {
        const queueUsage = settings.maximumQueuedEvents === 0
            ? 0
            : Math.min(100, Math.round((settings.pendingEvents / settings.maximumQueuedEvents) * 100));
        const warningThreshold = Number(settings.queueWarningThresholdPercent || 75);
        const queueStatus = queueUsage < warningThreshold ? "Healthy" : queueUsage < 100 ? "Needs attention" : "At capacity";
        const workerStatus = !settings.workerStarted ? "Starting" : settings.lastFailureUtc && (!settings.lastSuccessfulBatchUtc || new Date(settings.lastFailureUtc) > new Date(settings.lastSuccessfulBatchUtc)) ? "Retrying after a failed batch" : "Running";

        this.innerHTML = [
            "<style>",
            ":host { display: block; max-width: 960px; margin: 0 auto; padding: var(--uui-size-space-5, 24px); box-sizing: border-box; color: var(--uui-color-text, #1f2937); }",
            ".searchpulse-heading { margin: 0 0 6px; font-size: 28px; line-height: 1.2; } .searchpulse-intro { margin: 0 0 24px; color: var(--uui-color-text-alt, #64748b); }",
            ".searchpulse-stack { display: grid; gap: 20px; } .searchpulse-box { border: 1px solid var(--uui-color-border, #dbe3ee); border-radius: 8px; background: var(--uui-color-surface, #fff); padding: 22px; }",
            ".searchpulse-box h2 { margin: 0 0 8px; font-size: 18px; } .searchpulse-box p { margin: 0; line-height: 1.55; } .searchpulse-muted { color: var(--uui-color-text-alt, #64748b); }",
            ".searchpulse-toggle { display: flex; align-items: center; gap: 12px; margin: 18px 0 12px; font-weight: 600; }",
            ".searchpulse-status { display: inline-flex; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 700; } .searchpulse-on { background: #dcfce7; color: #166534; } .searchpulse-off { background: #fee2e2; color: #991b1b; }",
            ".searchpulse-queue { display: grid; grid-template-columns: auto 1fr auto; gap: 12px; align-items: center; margin-top: 18px; } .searchpulse-queue strong { font-variant-numeric: tabular-nums; } .searchpulse-diagnostics { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px 20px; margin: 18px 0 0; } .searchpulse-diagnostics div { min-width: 0; } .searchpulse-diagnostics dt { color: var(--uui-color-text-alt, #64748b); font-size: 12px; font-weight: 700; } .searchpulse-diagnostics dd { margin: 4px 0 0; font-size: 14px; font-variant-numeric: tabular-nums; }",
            ".searchpulse-progress { height: 9px; overflow: hidden; border-radius: 99px; background: #e7edf4; } .searchpulse-progress span { display: block; height: 100%; border-radius: inherit; background: #3b82f6; }",
            ".searchpulse-danger { border-color: #fecaca; background: #fffafa; } .searchpulse-danger h2 { color: #991b1b; } .searchpulse-actions { display: flex; flex-wrap: wrap; align-items: end; gap: 12px; margin-top: 18px; }",
            ".searchpulse-control { display: grid; gap: 6px; min-width: 200px; font-size: 13px; font-weight: 600; } .searchpulse-control select { min-height: 36px; padding: 0 10px; border: 1px solid var(--uui-color-border, #cbd5e1); border-radius: 4px; background: #fff; color: inherit; }",
            ".searchpulse-feedback { min-height: 20px; margin-top: 12px !important; font-size: 13px; } .searchpulse-loading { padding: 16px; }",
            "@media (max-width: 620px) { :host { padding: 16px; } .searchpulse-queue { grid-template-columns: 1fr; gap: 6px; } .searchpulse-actions { align-items: stretch; } .searchpulse-control { width: 100%; } }",
            "</style>",
            "<section aria-label=\"SearchPulse settings\"><h1 class=\"searchpulse-heading\">Settings</h1><p class=\"searchpulse-intro\">Manage collection and the anonymous data stored by this Umbraco installation.</p><div class=\"searchpulse-stack\">",
            "<section class=\"searchpulse-box\"><h2>Tracking</h2><p class=\"searchpulse-muted\">SearchPulse records only anonymous content signals after your website's existing analytics-consent check approves them.</p><label class=\"searchpulse-toggle\"><uui-toggle id=\"tracking-toggle\" " + (settings.isEnabled ? "checked" : "") + "></uui-toggle> Turn on SearchPulse tracking</label><p id=\"tracking-status\"><span class=\"searchpulse-status " + (settings.isEnabled ? "searchpulse-on\">Tracking on" : "searchpulse-off\">Tracking off") + "</span></p></section>",
            "<section class=\"searchpulse-box\"><h2>Collection health</h2><p class=\"searchpulse-muted\">Accepted events are stored in SearchPulse's durable local queue before they are added to reporting data. At capacity, collection returns HTTP 503 so loss is visible rather than silent.</p><div class=\"searchpulse-queue\"><strong>" + escapeHtml(queueStatus) + "</strong><div class=\"searchpulse-progress\" aria-label=\"Queue use " + queueUsage + "%\"><span style=\"width:" + queueUsage + "%\"></span></div><strong>" + formatNumber(settings.pendingEvents) + " / " + formatNumber(settings.maximumQueuedEvents) + " queued</strong></div><dl class=\"searchpulse-diagnostics\"><div><dt>Worker</dt><dd>" + escapeHtml(workerStatus) + "</dd></div><div><dt>Last completed batch</dt><dd>" + formatDate(settings.lastSuccessfulBatchUtc) + "</dd></div><div><dt>Oldest queued event</dt><dd>" + formatDate(settings.oldestPendingEventUtc) + "</dd></div><div><dt>Failed batches since start</dt><dd>" + formatNumber(settings.failedBatchCount) + "</dd></div></dl></section>",
            "<section class=\"searchpulse-box searchpulse-danger\"><h2>Data management</h2><p class=\"searchpulse-muted\">Remove anonymous SearchPulse data from this installation. This cannot be undone.</p><div class=\"searchpulse-actions\"><label class=\"searchpulse-control\">Data to remove<select id=\"clear-range\"><option value=\"1\">Last 24 hours</option><option value=\"7\">Last 7 days</option><option value=\"30\">Last 30 days</option><option value=\"90\">Last 90 days</option><option value=\"0\">All SearchPulse data</option></select></label><uui-button id=\"clear-data\" look=\"outline\" color=\"danger\">Clear data</uui-button></div><p id=\"data-status\" class=\"searchpulse-feedback\" aria-live=\"polite\"></p></section>",
            "</div></section>",
        ].join("");

        this.querySelector("#tracking-toggle").addEventListener("change", async (event) => {
            await this.save(event.target.checked);
        });
        this.querySelector("#clear-data").addEventListener("click", () => void this.clearData());
    }

    async save(isEnabled) {
        const status = this.querySelector("#tracking-status");
        status.textContent = "Saving...";

        try {
            const { error } = await umbHttpClient.put({
                url: searchPulseSettingsApi,
                security: bearerSecurity,
                body: { isEnabled },
            });
            if (error) {
                throw new Error("The setting could not be saved.");
            }

            status.innerHTML = "<span class=\"searchpulse-status " + (isEnabled ? "searchpulse-on\">Tracking on" : "searchpulse-off\">Tracking off") + "</span>";
        } catch {
            status.textContent = "The setting was not changed. Try again.";
            this.querySelector("#tracking-toggle").checked = !isEnabled;
        }
    }

    async clearData() {
        const rangeDays = Number(this.querySelector("#clear-range").value);
        const label = rangeDays === 0 ? "all SearchPulse data" : "SearchPulse data from the selected time range";
        if (!window.confirm("Clear " + label + "? This cannot be undone.")) {
            return;
        }

        const status = this.querySelector("#data-status");
        status.textContent = "Clearing data...";
        try {
            const { error } = await umbHttpClient.delete({
                url: searchPulseSettingsApi + "/data?rangeDays=" + rangeDays,
                security: bearerSecurity,
            });
            if (error) {
                throw new Error("The data could not be cleared.");
            }

            status.textContent = "SearchPulse data was cleared.";
            await this.load();
        } catch {
            status.textContent = "The data could not be cleared. Try again.";
        }
    }

    renderError() {
        this.innerHTML = "<uui-box headline=\"Settings\"><p>We could not load SearchPulse settings. Refresh the page, or check that SearchPulse is installed and the backoffice user has access.</p></uui-box>";
    }
}

function formatNumber(value) {
    return Number(value || 0).toLocaleString();
}

function formatDate(value) {
    if (!value) {
        return "Not available";
    }

    const date = new Date(value);
    return Number.isNaN(date.valueOf()) ? "Not available" : date.toLocaleString();
}

function escapeHtml(value) {
    const element = document.createElement("span");
    element.textContent = value;
    return element.innerHTML;
}

customElements.define("searchpulse-settings", SearchPulseSettingsElement);
export default SearchPulseSettingsElement;