const LOCALSTORAGE_INSTALLID_KEY = "StatisticsView.localinstallId";

const installSummary = window.localInstallSummary.id
    ? await window.localInstallSummary.promise.catch(() => null)
    : null;

const installForm = document.getElementById("installid-form");
const installInput = document.getElementById("installid-input");
const installSubmit = document.getElementById("installid-submit");
const installForget = document.getElementById("installid-forget");

const LOCALSTORAGE_INSTALLID_KEY = "StatisticsView.localinstallId";

// List of template names to load (must match the names of the HTML files in the templates directory)
const templateNames = ["GameSessionList", "GameSessionDetail", "GameSessionRow", "PlayerStatsList", "PlayerStatsDetail", "PlayerStatsRow"];

// Loads a template from the server; returns a Promise for the template element
async function loadTemplate(name) {
    const response = await fetch(`templates/${name}.html`);
    if (!response.ok) {
        throw new Error(`Failed to load template ${name}: ${response.status} ${response.statusText}`);
    }

    const newTemplate = document.createElement("template");
    newTemplate.innerHTML = await response.text();
    return newTemplate;
}

// Loads a JSON file from the server; returns a Promise for the parsed JSON object
async function loadJson(path) {
    const response = await fetch(path);
    if (!response.ok) {
        throw new Error(`Failed to load ${path}: ${response.status} ${response.statusText}`);
    }
    return response.json();
}

async function fetchInstallSummary(id) {
    const response = await fetch(`snapshots/installsummaries/${id}.json`);
    if (response.status === 404)
        return null; // No data found for this install ID
    if (!response.ok)
        throw new Error(`Failed to fetch install summary for ID ${id}: ${response.status} ${response.statusText}`);
    return await response.json(); // Assure JSON is valid before returning
}

function getLocalInstallId() {
    try {
        return localStorage.getItem(LOCALSTORAGE_INSTALLID_KEY);
    } catch {
        return null; // localStorage unavailable
    }
}

// Store a promise for local install summary from localStorage if it exists
// Validate GUID shape
const installId = getLocalInstallId();
window.localInstallSummary = installId && /^[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(installId)
    ? { id: installId, promise: fetchInstallSummary(installId) }
    : { id: null, promise: Promise.resolve(null) };

// Load all templates and store them in a global map for later use
window.templatePromises = new Map();
for (const templateName of templateNames) {
    templatePromises.set(templateName, loadTemplate(templateName));
}

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
