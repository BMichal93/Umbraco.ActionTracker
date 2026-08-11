const searchPulseSettingsApi = "/umbraco/management/api/v1/searchpulse/settings";

class SearchPulseSettingsElement extends HTMLElement {
    connectedCallback() {
        this.renderLoading();
        void this.load();
    }

    async load() {
        try {
            const response = await fetch(searchPulseSettingsApi, { credentials: "same-origin" });
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
            const response = await fetch(searchPulseSettingsApi, {
                method: "PUT",
                credentials: "same-origin",
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

    renderError() {
        this.innerHTML = "<uui-box headline=\"Tracking\"><p>We could not load this setting. Refresh the page, or check that SearchPulse is installed and the backoffice user has access.</p></uui-box>";
    }
}

customElements.define("searchpulse-settings", SearchPulseSettingsElement);
