const LOCALSTORAGE_INSTALLID_KEY = "StatisticsView.localinstallId";

const installForm = document.getElementById("installid-form");
const installInput = document.getElementById("installid-input");
const installSubmit = document.getElementById("installid-submit");
const installForget = document.getElementById("installid-forget");


function renderInstallIdPane() {
    if (window.localInstallSummary) {

    }
}

async function fetchInstallSummary(id) {
    const response = await fetch(`snapshots/installsummaries/${id}.json`);
    if (response.status === 404)
        return null; // No data found for this install ID
    if (!response.ok)
        throw new Error(`Failed to fetch install summary for ID ${id}: ${response.status} ${response.statusText}`);
    return await response.json(); // Assure JSON is valid before returning
}

// Trim whitepsace from install id input aggressively (whenever changed)
installInput.addEventListener("change", () => {
    installInput.value = installInput.value.trim();
});

// Handle install id form submission
installForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const id = installInput.value.trim();
    installSubmit.disabled = true;
    installInput.setCustomValidity(""); // Clear any previous validity messages

    // Verify install ID exists by fetching install summary
    try {
        window.localInstallSummary = await fetchInstallSummary(id);

        if (!window.localInstallSummary) {
            installInput.setCustomValidity("No stats found for that install ID.");
            installForm.reportValidity();
            return;
        }

        localStorage.setItem(LOCALSTORAGE_INSTALLID_KEY, id);
    }
    catch (err) {
        installInput.setCustomValidity("There was an error fetching stats for install ID.");
        installForm.reportValidity();
        console.error(err);
    }
    finally { 
        installSubmit.disabled = false;
    }

    if (summary)
        return;
    }
})
