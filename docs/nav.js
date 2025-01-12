// JavaScript source code
document.addEventListener('DOMContentLoaded', async () => {
    // set default for dynamic link to Download.html
    try {
        const defaultSource = await fetch('Download.html');
        const defaultInnerHTML = await(defaultSource.text());
        document.getElementById('content-area').innerHTML = defaultInnerHTML;
    } catch (error) {
        console.error('Error loading default content:', error);
    }

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
});

