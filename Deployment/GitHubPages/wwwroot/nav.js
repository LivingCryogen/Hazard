// JavaScript source code
console.log("nav.js loaded");

document.addEventListener('DOMContentLoaded', async () => {
    await setDynamicLinkDefaultContent();
    setupDynamicLinks();
    setupDownloadTooltips();
});

// Set default content for dynamic link content area to Download.html
async function setDynamicLinkDefaultContent() {
    try {
        const defaultSource = await fetch('Download.html');
        const defaultInnerHTML = await (defaultSource.text());
        document.getElementById('content-area').innerHTML = defaultInnerHTML;
    } catch (error) {
        console.error('Error loading default content:', error);
    }
}

// Set up the dynamic link behavior
function setupDynamicLinks() {
    document.querySelectorAll('.dynamic-link').forEach(link => {
        link.addEventListener('click', async (e) => {
            e.preventDefault(); // prevent default link behavior
            try {
                const contentPath = link.dataset.content; // get the data-content value given in the .html for the link
                const response = await fetch(contentPath); // web request based on the preceding value (data-content must be a URL)
                const html = await response.text(); // get, specifically, the html from the requested page
                document.getElementById('content-area').innerHTML = html; // set the inner area of the 'content-area' div to the returned html
            } catch (error) {
                console.error('Error loading content:', error);
            }
        });
    });
}

// Add tooltips to download buttons
function setupDownloadTooltips() {
    const x64Button = document.querySelector('.download-button[href*="x64"]');
    const armButton = document.querySelector('.download-button[href*="ARM"]');

    if (x64Button) {
        x64Button.setAttribute('title', 'For most Windows, Mac, and Linux computers with Intel or AMD processors');
    }

    if (armButton) {
        armButton.setAttribute('title', 'For newer Macs with Apple Silicon (M1/M2/M3), Windows on ARM devices, and many mobile devices');
    }
}