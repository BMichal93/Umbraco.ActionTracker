import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";

const searchPulseSettingsApi = "/umbraco/management/api/v1/searchpulse/settings";

class SearchPulseSettingsElement extends UmbElementMixin(HTMLElement) {
    connectedCallback() {
        this.renderLoading();
        void this.load();
    }

    async load() {
        try {
            const response = await this.request(searchPulseSettingsApi);
            if (!response.ok) {
                throw new Error("The settings could not be loaded.");
            }

            this.render((await response.json()).isEnabled);
        } catch {
            this.renderError();
        }
    }

    renderLoading() {
        this.innerHTML = "<uui-box><p>Loading SearchPulse…</p></uui-box>";
    }

    render(isEnabled) {
        this.innerHTML = `
            <uui-box headline="Tracking">
                <p>SearchPulse records only anonymous content signals after your website's existing analytics-consent check approves them.</p>
                <label>
                    <uui-toggle id="tracking-toggle" ${isEnabled ? "checked" : ""}></uui-toggle>
                    Turn on SearchPulse tracking
                </label>
                <p id="status-message">${isEnabled ? "Tracking is on." : "Tracking is off."}</p>
            </uui-box>`;

        this.querySelector("#tracking-toggle").addEventListener("change", async (event) => {
            await this.save(event.target.checked);
        });
    }

    async save(isEnabled) {
        const statusMessage = this.querySelector("#status-message");
        statusMessage.textContent = "Saving…";

        try {
            const response = await this.request(searchPulseSettingsApi, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ isEnabled }),
            });
            if (!response.ok) {
                throw new Error("The setting could not be saved.");
            }

            statusMessage.textContent = isEnabled ? "Tracking is on." : "Tracking is off.";
        } catch {
            statusMessage.textContent = "The setting was not changed. Try again.";
            this.querySelector("#tracking-toggle").checked = !isEnabled;
        }
    }

    async request(url, options = {}) {
        const authContext = await this.getContext(UMB_AUTH_CONTEXT);
        const token = await authContext?.getLatestToken();

        return fetch(url, {
            credentials: "include",
            ...options,
            headers: {
                Authorization: `Bearer ${token}`,
                ...options.headers,
            },
        });
    }
    renderError() {
        this.innerHTML = "<uui-box headline=\"Tracking\"><p>We could not load this setting. Refresh the page, or check that SearchPulse is installed and the backoffice user has access.</p></uui-box>";
    }
}

customElements.define("searchpulse-settings", SearchPulseSettingsElement);
export default SearchPulseSettingsElement;
