import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";

const api = "/umbraco/management/api/v1/searchpulse";
const security = [{ scheme: "bearer", type: "http" }];

class SearchPulseSettingsElement extends UmbElementMixin(HTMLElement) {
    connectedCallback() { this.renderLoading(); void this.load(); }
    async load() {
        try {
            const [settingsResult, goalsResult] = await Promise.all([
                umbHttpClient.get({ url: api + "/settings", security }),
                umbHttpClient.get({ url: api + "/goals", security }),
            ]);
            if (settingsResult.error || goalsResult.error) throw new Error("load failed");
            this.render(settingsResult.data, goalsResult.data || []);
        } catch { this.renderError(); }
    }
    renderLoading() { this.innerHTML = "<uui-box><p>Loading SearchPulse...</p></uui-box>"; }
    render(settings, goals) {
        const usage = settings.maximumQueuedEvents ? Math.min(100, Math.round(settings.pendingEvents / settings.maximumQueuedEvents * 100)) : 0;
        const worker = !settings.workerStarted ? "Starting" : settings.lastFailureUtc && (!settings.lastSuccessfulBatchUtc || new Date(settings.lastFailureUtc) > new Date(settings.lastSuccessfulBatchUtc)) ? "Retrying after a failed batch" : "Running";
        const goalRows = goals.length ? goals.map(goal => "<div class=\"goal\"><span><strong>" + escapeHtml(goal.name) + "</strong><small>" + escapeHtml(goal.eventType) + " / " + escapeHtml(goal.target) + "</small></span><uui-button look=\"secondary\" data-delete=\"" + goal.id + "\">Delete</uui-button></div>").join("") : "<p class=\"muted\">No goals configured.</p>";
        this.innerHTML = [
            "<style>:host{display:block;max-width:960px;margin:0 auto;padding:24px;color:var(--uui-color-text,#1f2937)}.stack{display:grid;gap:20px}.box{border:1px solid var(--uui-color-border,#dbe3ee);border-radius:8px;background:#fff;padding:22px}.box h1,.box h2{margin:0 0 8px}.muted{color:var(--uui-color-text-alt,#64748b);line-height:1.5}.toggle{display:flex;gap:12px;align-items:center;margin:18px 0;font-weight:600}.queue{display:grid;grid-template-columns:auto 1fr auto;gap:12px;align-items:center;margin-top:18px}.progress{height:9px;background:#e7edf4;border-radius:99px;overflow:hidden}.progress span{display:block;height:100%;background:#3b82f6}.diag{display:grid;grid-template-columns:repeat(2,1fr);gap:12px;margin-top:18px}.diag dt{font-size:12px;font-weight:700;color:#64748b}.diag dd{margin:4px 0}.danger{border-color:#fecaca}.actions{display:flex;flex-wrap:wrap;gap:10px;align-items:end;margin-top:16px}.actions input,.actions select{min-height:34px;padding:5px 8px;border:1px solid #cbd5e1;border-radius:4px}.goal{display:flex;justify-content:space-between;align-items:center;padding:10px 0;border-bottom:1px solid #edf0f4}.goal small{display:block;color:#64748b;margin-top:4px}@media(max-width:620px){:host{padding:16px}.queue,.diag{grid-template-columns:1fr}.actions input,.actions select{width:100%}}</style>",
            "<section class=\"stack\"><section class=\"box\"><h1>SearchPulse settings</h1><p class=\"muted\">Manage anonymous collection, local goals, and operational health for this Umbraco installation.</p><label class=\"toggle\"><uui-toggle id=\"toggle\" " + (settings.isEnabled ? "checked" : "") + "></uui-toggle> Tracking enabled</label><p id=\"tracking-status\"></p></section>",
            "<section class=\"box\"><h2>Collection health</h2><p class=\"muted\">Events are accepted into the local durable queue before reporting work runs. A full queue returns HTTP 503 so loss is visible.</p><div class=\"queue\"><strong>" + usage + "% used</strong><div class=\"progress\"><span style=\"width:" + usage + "%\"></span></div><strong>" + formatNumber(settings.pendingEvents) + " / " + formatNumber(settings.maximumQueuedEvents) + " queued</strong></div><dl class=\"diag\"><div><dt>Worker</dt><dd>" + escapeHtml(worker) + "</dd></div><div><dt>Last completed batch</dt><dd>" + formatDate(settings.lastSuccessfulBatchUtc) + "</dd></div><div><dt>Oldest queued event</dt><dd>" + formatDate(settings.oldestPendingEventUtc) + "</dd></div><div><dt>Failed batches</dt><dd>" + formatNumber(settings.failedBatchCount) + "</dd></div></dl></section>",
            "<section class=\"box\"><h2>Goals</h2><p class=\"muted\">Goals count approved anonymous signals and never store visitor identities.</p><div id=\"goals\">" + goalRows + "</div><div class=\"actions\"><input id=\"goal-name\" placeholder=\"Goal name\" maxlength=\"80\"><select id=\"goal-type\"><option value=\"FormSuccess\">Form success</option><option value=\"CustomAction\">Custom action</option><option value=\"DownloadClick\">Download</option><option value=\"ExternalLinkClick\">External link</option><option value=\"SiteSearch\">Site search</option></select><input id=\"goal-target\" placeholder=\"Target\" maxlength=\"80\"><uui-button id=\"add\" look=\"primary\">Add goal</uui-button></div><p id=\"goal-status\" class=\"muted\"></p></section>",
            "<section class=\"box danger\"><h2>Data management</h2><p class=\"muted\">Remove anonymous SearchPulse data. This cannot be undone.</p><div class=\"actions\"><select id=\"clear-range\"><option value=\"1\">Last 24 hours</option><option value=\"7\">Last 7 days</option><option value=\"30\">Last 30 days</option><option value=\"90\">Last 90 days</option><option value=\"0\">All data</option></select><uui-button id=\"clear\" color=\"danger\">Clear data</uui-button></div><p id=\"data-status\" class=\"muted\"></p></section></section>",
        ].join("");
        this.querySelector("#toggle").addEventListener("change", event => void this.save(event.target.checked));
        this.querySelector("#add").addEventListener("click", () => void this.addGoal());
        this.querySelectorAll("[data-delete]").forEach(button => button.addEventListener("click", () => void this.deleteGoal(button.dataset.delete)));
        this.querySelector("#clear").addEventListener("click", () => void this.clearData());
    }
    async save(isEnabled) { const status = this.querySelector("#tracking-status"); status.textContent = "Saving..."; const { error } = await umbHttpClient.put({ url: api + "/settings", security, body: { isEnabled } }); status.textContent = error ? "The setting could not be saved." : "Tracking setting saved."; if (error) this.querySelector("#toggle").checked = !isEnabled; }
    async addGoal() { const name = this.querySelector("#goal-name").value.trim(), eventType = this.querySelector("#goal-type").value, target = this.querySelector("#goal-target").value.trim(), status = this.querySelector("#goal-status"); if (!name || !target) { status.textContent = "Enter a name and target."; return; } const { error } = await umbHttpClient.post({ url: api + "/goals", security, body: { name, eventType, target, isEnabled: true } }); if (error) { status.textContent = "The goal could not be saved."; return; } await this.load(); }
    async deleteGoal(id) { if (!window.confirm("Delete this goal?")) return; const { error } = await umbHttpClient.delete({ url: api + "/goals/" + id, security }); if (error) { this.querySelector("#goal-status").textContent = "The goal could not be deleted."; return; } await this.load(); }
    async clearData() { const range = Number(this.querySelector("#clear-range").value); if (!window.confirm("Clear SearchPulse data? This cannot be undone.")) return; const status = this.querySelector("#data-status"); status.textContent = "Clearing..."; const { error } = await umbHttpClient.delete({ url: api + "/settings/data?rangeDays=" + range, security }); status.textContent = error ? "The data could not be cleared." : "Data cleared."; }
    renderError() { this.innerHTML = "<uui-box headline=\"Settings\"><p>We could not load SearchPulse settings. Refresh the page, or check access.</p></uui-box>"; }
}
function formatNumber(value) { return Number(value || 0).toLocaleString(); }
function formatDate(value) { if (!value) return "Not available"; const date = new Date(value); return Number.isNaN(date.valueOf()) ? "Not available" : date.toLocaleString(); }
function escapeHtml(value) { const element = document.createElement("span"); element.textContent = value ?? ""; return element.innerHTML; }
customElements.define("searchpulse-settings", SearchPulseSettingsElement);
export default SearchPulseSettingsElement;
