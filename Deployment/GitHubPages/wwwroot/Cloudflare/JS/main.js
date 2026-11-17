// Get prefetched global summary promise (used by default view)
const globalSummaryPromise = await loadJson('snapshots/globalsummary.json');
        
// Get local install ID from localStorage if available
const LOCALSTORAGE_INSTALLID_KEY = "StatisticsView.localinstallId";

const localInstallId = getLocalInstallId();
const installSummary = localInstallId && /^[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(localInstallId)
    ? { id: localInstallId, promise: fetchInstallSummary(localInstallId) }
    : { id: null, promise: Promise.resolve(null) };

function getLocalInstallId() {
    try {
        return localStorage.getItem(LOCALSTORAGE_INSTALLID_KEY);
    } catch {
        return null; // localStorage unavailable
    }
}

async function fetchInstallSummary(id) {
    return loadJson(`snapshots/installsummaries/${id}.json`);
}

// Loads a JSON file from the server; returns a Promise for the parsed JSON object
async function loadJson(path) {
    const response = await fetch(path);
    if (response.status === 404)
        return null; // No data found at this path
    if (!response.ok) {
        throw new Error(`Failed to load ${path}: ${response.status} ${response.statusText}`);
    }
    return response.json();
}




const installForm = document.getElementById("installid-form");
const installInput = document.getElementById("installid-input");
const installSubmit = document.getElementById("installid-submit");
const installForget = document.getElementById("installid-forget");

const LOCALSTORAGE_INSTALLID_KEY = "StatisticsView.localinstallId";




function readURLRoute() {
    const hashString = location.hash.slice(1); // remove the hash(#)
    const routeParams = new URLSearchParams(hashString);

    // ROUTING~!!
}

function render() {
    if (installSummary && defaultView) {

    }
    else if (installSummary && !defaultView) {

    }

    
    renderInstallIdPane(window.localInstallSummary? window.localInstallSummary. : );
}

addEventListener("hashchange", () => render(readURLRoute()));



// Trim whitepsace from install id input aggressively (whenever changed)
installInput.addEventListener("change", () => {
    installInput.value = installInput.value.trim();
});

// Clear custom validity message on input to allow re-validation
installInput.addEventListener("input", () => installInput.setCustomValidity(""));

// Handle install id form submission
installForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const id = installInput.value.trim();
    installSubmit.disabled = true;
    installInput.setCustomValidity(""); // Clear any previous validity messages

    // Verify install ID exists by fetching install summary
    try {
        const localInstallSummary = await fetchInstallSummary(id);

        if (!localInstallSummary) {
            installInput.setCustomValidity("No stats found for that install ID.");
            installForm.reportValidity();
            return;
        }
        window.localInstallSummary = localInstallSummary;
    }
    catch (err) {
        installInput.setCustomValidity("There was an error fetching stats for install ID.");
        installForm.reportValidity();
        console.error(err);
    }
    finally { 
        installSubmit.disabled = false;
    }

    localStorage.setItem(LOCALSTORAGE_INSTALLID_KEY, id);

    render(readURLRoute());
})

render(readURLRoute());
