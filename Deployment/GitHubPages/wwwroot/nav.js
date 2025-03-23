// JavaScript source code
document.addEventListener('DOMContentLoaded', async () => {
    await setDynamicLinkDefaultContent();
    setupDynamicLinks();
    await setupDownloadButton();
});

// set default content for dynamic link content area to Download.html
async function setDynamicLinkDefaultContent() {
    try {
        const defaultSource = await fetch('Download.html');
        const defaultInnerHTML = await(defaultSource.text());
        document.getElementById('content-area').innerHTML = defaultInnerHTML;
    } catch (error) {
        console.error('Error loading default content:', error);
    }
}

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

async function setupDownloadButton() {
    const downloadButton = document.getElementById('download-button');
    if (!downloadButton)
        return;
    downloadButton.textContent = "Fetching Token...";
    const secureLink = await getSecureLink();
    if (!secureLink || !secureLink.URL || !secureLink.SAS) {
        downloadButton.textContent = "Fetch Failed!";
        return;
    }
    downloadButton.href = secureLink.URL;
    downloadButton.textContent = "Download!";
}

// get secure URL (with SASToken) for download from Azure Blob Storage
async function getSecureLink() {
    try {
        const azFuncResponse = await fetch('');
        if (!azFuncResponse.ok)
            throw new Error(`Failed to fetch SAS token: ${azFuncResponse.status} ${azFuncResponse.statusText}`);
        return await azFuncResponse.json();
    }
    catch (error) {
        console.error('Error when fetching SAS token:', error);
        return null;
    }
}

