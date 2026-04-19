// ====================================
// SHARE & COPY BUTTONS
// ====================================
function initShareButtons() {
    const shareButton = document.getElementById("shareButton");

    if (shareButton) {
        shareButton.addEventListener("click", async () => {
            const shareData = {
                title: document.title,
                text: "Check out this state symbol!",
                url: window.location.href
            };

            if (navigator.share) {
                try {
                    await navigator.share(shareData);
                    return;
                } catch (err) {
                    console.log("Share failed, fallback to copy");
                }
            }

            try {
                await navigator.clipboard.writeText(window.location.href);

                shareButton.innerHTML = `<i class="fas fa-check"></i> Copied!`;
                shareButton.classList.remove("from-blue-600", "to-indigo-600", "from-purple-600", "to-pink-600");
                shareButton.classList.add("from-green-600", "to-green-600");

                setTimeout(() => {
                    shareButton.innerHTML = `<i class="fas fa-share-nodes"></i> Share`;
                    shareButton.classList.remove("from-green-600", "to-green-600");
                    shareButton.classList.add("from-blue-600", "to-indigo-600");
                }, 1500);

            } catch (err) {
                alert("Your browser does not support copying.");
            }
        });
    }

    const copyBtn = document.getElementById("copyLinkBtn");

    if (copyBtn) {
        copyBtn.addEventListener("click", async () => {
            try {
                await navigator.clipboard.writeText(window.location.href);

                copyBtn.innerHTML = `<i class="fas fa-check"></i> Copied!`;
                copyBtn.classList.add("bg-green-600", "text-white", "border-green-700");
                copyBtn.classList.remove("bg-white", "text-blue-700", "border-blue-300", "text-purple-700", "border-purple-300");

                setTimeout(() => {
                    copyBtn.innerHTML = `<i class="fas fa-copy"></i> Copy Link`;
                    copyBtn.classList.remove("bg-green-600", "text-white", "border-green-700");
                    copyBtn.classList.add("bg-white", "text-blue-700", "border-blue-300");
                }, 1500);

            } catch (err) {
                alert("Copy failed. Your browser may not support navigator.clipboard.");
            }
        });
    }
}

// ====================================
// TABLE OF CONTENTS FUNCTIONALITY
// ====================================
function initTableOfContents() {
    // Desktop TOC Toggle
    const tocToggle = document.getElementById('tocToggle');
    const tocContent = document.getElementById('tocContent');
    const tocToggleIcon = document.getElementById('tocToggleIcon');

    if (tocToggle && tocContent) {
        tocToggle.addEventListener('click', () => {
            const isHidden = tocContent.classList.contains('hidden');

            if (isHidden) {
                tocContent.classList.remove('hidden');
                tocToggleIcon.classList.add('rotate-180');
            } else {
                tocContent.classList.add('hidden');
                tocToggleIcon.classList.remove('rotate-180');
            }
        });

        // Close TOC when clicking a link (desktop)
        const tocLinks = tocContent.querySelectorAll('.toc-link');
        tocLinks.forEach(link => {
            link.addEventListener('click', () => {
                tocContent.classList.add('hidden');
                tocToggleIcon.classList.remove('rotate-180');
            });
        });
    }

    // Mobile Floating Button & Drawer
    const floatingBtn = document.getElementById('floatingTocBtn');
    const tocOverlay = document.getElementById('tocOverlay');
    const tocDrawer = document.getElementById('tocDrawer');
    const closeDrawerBtn = document.getElementById('closeTocDrawer');

    function openTocDrawer() {
        if (tocOverlay && tocDrawer) {
            tocOverlay.classList.remove('hidden');
            setTimeout(() => {
                tocDrawer.classList.remove('translate-y-full');
            }, 10);
        }
    }

    function closeTocDrawer() {
        if (tocOverlay && tocDrawer) {
            tocDrawer.classList.add('translate-y-full');
            setTimeout(() => {
                tocOverlay.classList.add('hidden');
            }, 300);
        }
    }

    if (floatingBtn) {
        floatingBtn.addEventListener('click', openTocDrawer);
    }

    if (closeDrawerBtn) {
        closeDrawerBtn.addEventListener('click', closeTocDrawer);
    }

    if (tocOverlay) {
        tocOverlay.addEventListener('click', (e) => {
            if (e.target === tocOverlay) {
                closeTocDrawer();
            }
        });
    }

    const mobileTocLinks = document.querySelectorAll('.toc-mobile-link');
    mobileTocLinks.forEach(link => {
        link.addEventListener('click', () => {
            closeTocDrawer();
        });
    });

    // Smooth scroll for all TOC links
    document.querySelectorAll('.toc-link, .toc-mobile-link').forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.getAttribute('href');
            const targetElement = document.querySelector(targetId);

            if (targetElement) {
                const offset = 80;
                const elementPosition = targetElement.getBoundingClientRect().top;
                const offsetPosition = elementPosition + window.pageYOffset - offset;

                window.scrollTo({
                    top: offsetPosition,
                    behavior: 'smooth'
                });
            }
        });
    });

    // Hide floating button when scrolled to bottom
    let lastScrollTop = 0;
    window.addEventListener('scroll', () => {
        if (floatingBtn) {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            const windowHeight = window.innerHeight;
            const documentHeight = document.documentElement.scrollHeight;

            if (scrollTop + windowHeight >= documentHeight - 100) {
                floatingBtn.style.opacity = '0';
                floatingBtn.style.pointerEvents = 'none';
            } else {
                floatingBtn.style.opacity = '1';
                floatingBtn.style.pointerEvents = 'auto';
            }
        }
    });
}

// ====================================
// READING PROGRESS BAR
// ====================================
function initProgressBar() {
    const progressBar = document.getElementById('progressBar');

    if (progressBar) {
        window.addEventListener('scroll', () => {
            const windowHeight = document.documentElement.scrollHeight - window.innerHeight;
            const scrolled = (window.scrollY / windowHeight) * 100;
            progressBar.style.width = Math.min(scrolled, 100) + '%';
        });
    }
}

// ====================================
// INITIALIZE ALL
// ====================================
document.addEventListener('DOMContentLoaded', () => {
    initShareButtons();
    initTableOfContents();
    initProgressBar();
});