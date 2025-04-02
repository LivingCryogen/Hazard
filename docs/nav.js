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
    const downloadButton = document.getElementById('autodetect-download-button');
    if (!downloadButton)
        return;
    downloadButton.textContent = "Detecting Architecture...";
    var architecture = await getArchitecture();
    if (architecture == "undetected") {
        architecture = "Undetected(x64?)";
    }
    else if (architecture == "x86") {
        downloadButton.textContent = "CPU Not Supported";
        downloadButton.disabled = true;
        return;
    }
    else {
        downloadButton.textContent = `Download! (${architecture})`
    }

    downloadButton.href = `https://hazardgameproxy-d4caecgsapakcwh0.centralus-01.azurewebsites.net/secure-url?arch=${architecture}`;
}

async function getArchitecture() {
    if (navigator.userAgentData && navigator.userAgentData.getHighEntropyValues) {
        const highEntropyValues = await navigator.userAgentData.getHighEntropyValues(["architecture"]);
        return highEntropyValues.architecture;
    }
    else {
        const userAgent = navigator.userAgent.toLowerCase();
        if (userAgent.includes("arm") || userAgent.includes("aarch64")) {
            return "ARM";
        }
        else if (userAgent.includes("x86_64") || userAgent.includes("amd64") || userAgent.includes("win64")) {
            return "x64"
        }
        else if (userAgent.includes("x86") || userAgent.includes("i386") || userAgent.includes("i686")) {
            return "x86"
        }
        else return "undetected";
    }
}


