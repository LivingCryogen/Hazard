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

// Load all templates and store them in a global map for later use
window.templatePromises = new Map();
for (const templateName of templateNames) {
    templatePromises.set(templateName, loadTemplate(templateName));
}

