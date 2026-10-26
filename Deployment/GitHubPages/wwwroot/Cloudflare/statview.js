const installForm = document.getElementById("installid-form");
const installInput = document.getElementById("install-id");
const installForget = document.getElementById("install-forget");


function renderInstallIdPane() {
    if (window.localInstallSummaryPromise) {

    }
}

async function fetchInstallSummary(id) {
    return await fetch(`snapshots/installsummaries/${id}.json}`);
}

// Trim whitepsace from install id input aggressively (whenever changed)
installInput.addEventListener("change", () => {
    installInput.value = installInput.value.trim();
});

// Handle install id form submission
installForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const id = installInput.value.trim();
    const summary = await fetchInstallSummary(id);
    if (summary)
        return;
    }
})
