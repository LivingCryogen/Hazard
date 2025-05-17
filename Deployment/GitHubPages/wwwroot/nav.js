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
                const contentPath = link.dataset.content; // get the data-content value
                const response = await fetch(contentPath); // web request
                const html = await response.text(); // get the html
                document.getElementById('content-area').innerHTML = html; // set inner area

                // If the content is Features.html, balance the heights after a short delay
                // to ensure content is fully rendered
                if (contentPath === 'Features.html') {
                    setTimeout(balanceHeights, 100);
                }

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

// Function to balance heights for code snippets and text
function balanceHeights() {
    // Find all image-with-text containers in the dynamic content area
    const containers = document.querySelectorAll('#content-area .code-with-text');

    containers.forEach(container => {
        // Find the code snippet and text elements
        const codeElement = container.querySelector('.code-snippet') || container.querySelector('pre');
        const textElement = container.querySelector('.image-text');

        if (!codeElement || !textElement) return;

        // Reset any previously set heights to get natural heights
        codeElement.style.maxHeight = 'none';
        textElement.style.maxHeight = 'none';

        // Get the scroll heights (full content height)
        const codeScrollHeight = codeElement.scrollHeight;
        const textScrollHeight = textElement.scrollHeight;

        // Determine which is shorter
        const minHeight = Math.min(codeScrollHeight, textScrollHeight);

        // Set max-height to both elements based on the shorter one
        // Add a small buffer (20px) for padding and to prevent unnecessary scrollbars
        const heightWithBuffer = minHeight + 1;
        codeElement.style.maxHeight = `${heightWithBuffer}px`;
        textElement.style.maxHeight = `${heightWithBuffer}px`;

        // Add a class to indicate they've been balanced
        codeElement.classList.add('height-balanced');
        textElement.classList.add('height-balanced');
    });
}

// Run when window is resized (only affects features page)
window.addEventListener('resize', function () {
    // Only balance heights if Features.html is currently loaded
    const featuresLink = document.querySelector('.dynamic-link[data-content="Features.html"]');
    if (featuresLink && featuresLink.classList.contains('active')) {
        balanceHeights();
    }
});