    // Add JavaScript for functionality
    document.addEventListener('DOMContentLoaded', async () => {
        // Load Features.html by default
        try {
            const defaultSource = await fetch('Features.html');
            const defaultInnerHTML = await (defaultSource.text());
            document.getElementById('content-area').innerHTML = defaultInnerHTML;

            // Add syntax highlighting after initial load
            if (typeof Prism !== 'undefined') {
                Prism.highlightAll();
            }

            // Balance heights for code-with-text sections after loading
            setTimeout(balanceHeights, 100);
         } catch (error) {
         console.error('Error loading default content:', error);
        }

    document.addEventListener('click', (e) => {
        if (e.target && e.target.id === 'metricsButton') {
            e.preventDefault();
            console.log('Metrics button clicked!'); // Debug log
            if (metricsModal) {
                metricsModal.style.display = 'block';
                console.log('Metrics modal opened!'); // Debug log
            } else {
                console.error('Metrics modal not found!');
            }
        }
    });

    // Set up download button tooltips
    setupDownloadTooltips();

    // Set up dynamic links
    document.querySelectorAll('.dynamic-link').forEach(link => {
    link.addEventListener('click', async (e) => {
        e.preventDefault();
        try {
            const contentPath = link.dataset.content;
            const response = await fetch(contentPath);
            const html = await response.text();
            document.getElementById('content-area').innerHTML = html;
    
            // Add syntax highlighting after loading dynamic content
            if (typeof Prism !== 'undefined') {
                Prism.highlightAll();
            }
    
            // If Features.html is loaded, balance heights
            if (contentPath === 'Features.html' || contentPath === 'TestStrategy.html' || contentPath === 'Deployment.html') {
                setTimeout(balanceHeights, 100);
            }
    
            // Scroll to content area
            document.getElementById('content-area').scrollIntoView({ behavior: 'smooth' });
        } catch (error) {
            console.error('Error loading content:', error);
        }
        });
    });

    setupCodeModals();

    // Demo modal
    const demoButton = document.getElementById('demoButton');
    const demoModal = document.getElementById('demoModal');

    if (demoButton && demoModal) {
        demoButton.addEventListener('click', (e) => {
            e.preventDefault();
            demoModal.style.display = 'block';

            if (window.innerWidth <= 768) {   // add specific modal class on mobile
                document.body.classList.add('modal-open');
            }
        });
        }

    // Testing metrics modal
    const metricsButton = document.getElementById('metricsButton');
    const metricsModal = document.getElementById('metricsModal');
    

    // Download modal
    const downloadButton = document.getElementById('downloadButton');
    const downloadModal = document.getElementById('downloadModal');

    if (downloadButton && downloadModal) {
        downloadButton.addEventListener('click', (e) => {
            e.preventDefault();
            downloadModal.style.display = 'block';

            if (window.innerWidth <= 768) {   // add specific modal class on mobile
                document.body.classList.add('modal-open');
            }
        });
        }

    // Close modals
    const closeModals = document.querySelectorAll('.close-modal');
        closeModals.forEach(closeBtn => {
        closeBtn.addEventListener('click', () => {
            document.querySelectorAll('.modal').forEach(modal => {
                modal.style.display = 'none';
            });
            if (window.innerWidth <= 768) {   // remove specific modal class on mobile
                document.body.classList.remove('modal-open');
            }
        });
        });

        // Close modal when clicking outside
        window.addEventListener('click', (e) => {
        document.querySelectorAll('.modal').forEach(modal => {
            if (e.target === modal) {
                modal.style.display = 'none';
            }
            if (window.innerWidth <= 768) {   // remove specific modal class on mobile
                document.body.classList.remove('modal-open');
            }
        });
        });

    // Image gallery navigation
    const galleryDots = document.querySelectorAll('.gallery-dot');
    const galleryImages = document.querySelectorAll('.gallery-img');

        galleryDots.forEach(dot => {
        dot.addEventListener('click', () => {
            const index = dot.dataset.index;

            // Update active classes
            document.querySelector('.gallery-dot.active').classList.remove('active');
            dot.classList.add('active');

            document.querySelector('.gallery-img.active').classList.remove('active');
            galleryImages[index].classList.add('active');
        });
        });
    });

    // Function to balance heights for code snippets and text
    function balanceHeights() {
        const containers = document.querySelectorAll('#content-area .code-with-text');

        containers.forEach(container => {
            const codeElement = container.querySelector('.code-snippet') || container.querySelector('pre');
    const textElement = container.querySelector('.image-text');

    if (!codeElement || !textElement) return;

    codeElement.style.maxHeight = 'none';
    textElement.style.maxHeight = 'none';

    const codeScrollHeight = codeElement.scrollHeight;
    const textScrollHeight = textElement.scrollHeight;

    const minHeight = Math.min(codeScrollHeight, textScrollHeight);
    const heightWithBuffer = minHeight + 1;

    codeElement.style.maxHeight = `${heightWithBuffer}px`;
    textElement.style.maxHeight = `${heightWithBuffer}px`;

    codeElement.classList.add('height-balanced');
    textElement.classList.add('height-balanced');
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

function setupCodeModals() {
    const codeModal = document.getElementById('codeModal');
    const codeModalTitle = document.getElementById('codeModalTitle');
    const codeModalContent = document.getElementById('codeModalContent');
    const closeCodeModal = document.getElementById('closeCodeModal');
    const copyCodeBtn = document.getElementById('copyCodeBtn');

    // Add click listeners to all code snippets
    document.addEventListener('click', function (e) {
        const snippet = e.target.closest('.code-snippet');
        if (!snippet || snippet.closest('.code-modal')) return;

        // Find the title from the parent section's h3
        const parentSection = snippet.closest('.code-with-text');
        let title = 'Code Snippet';

        if (parentSection) {
            const h3Element = parentSection.querySelector('.image-text h3');
            if (h3Element) {
                title = h3Element.textContent.trim();
            }
        }

        const code = snippet.querySelector('code');
        const codeText = code ? code.textContent : snippet.textContent;

        codeModalTitle.textContent = title;
        codeModalContent.querySelector('code').textContent = codeText;

        // Preserve the language class for Prism
        const originalCode = snippet.querySelector('code');
        const modalCode = codeModalContent.querySelector('code');
        if (originalCode && originalCode.className) {
            modalCode.className = originalCode.className;
        }

        codeModal.style.display = 'block';

        if (window.innerWidth <= 768) {   // add specific modal class on mobile
            document.body.classList.add('modal-open');
        }

        // Re-apply Prism highlighting
        if (typeof Prism !== 'undefined') {
            Prism.highlightElement(modalCode);
        }
    });

    // Close modal handlers
    closeCodeModal.addEventListener('click', () => {
        codeModal.style.display = 'none';
        if (window.innerWidth <= 768) {   // remove specific modal class on mobile
            document.body.classList.remove('modal-open');
        }
    });

    window.addEventListener('click', (e) => {
        if (e.target === codeModal) {
            codeModal.style.display = 'none';

            if (window.innerWidth <= 768) {   // remove specific modal class on mobile
                document.body.classList.remove('modal-open');
            }
        }
    });

    // ESC key to close
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && codeModal.style.display === 'block') {
            codeModal.style.display = 'none';

            if (window.innerWidth <= 768) {   // remove specific modal class on mobile
                document.body.classList.remove('modal-open');
            }
        }
    });

    // Copy functionality
    copyCodeBtn.addEventListener('click', async () => {
        try {
            const codeText = codeModalContent.querySelector('code').textContent;
            await navigator.clipboard.writeText(codeText);
            copyCodeBtn.textContent = 'Copied!';
            setTimeout(() => {
                copyCodeBtn.textContent = 'Copy Code';
            }, 2000);
        } catch (err) {
            console.error('Copy failed:', err);
        }
    });
}


    // Run when window is resized
    window.addEventListener('resize', function () {
        // Check if Features.html is loaded
        if (document.querySelector('.code-with-text')) {
        balanceHeights();
        }
    });