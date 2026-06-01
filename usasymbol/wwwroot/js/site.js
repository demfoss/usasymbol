// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Mobile menu toggle
document.addEventListener('DOMContentLoaded', function () {
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenu = document.getElementById('mobile-menu');

    if (mobileMenuButton && mobileMenu) {
        mobileMenuButton.addEventListener('click', function () {
            mobileMenu.classList.toggle('hidden');
        });
    }

    initVisualAssetLightbox();
});

function initVisualAssetLightbox() {
    if (window.__visualLightboxInitialized) {
        return;
    }

    window.__visualLightboxInitialized = true;

    function ensureVisualLightbox() {
        let overlay = document.getElementById('visualAssetLightbox');
        if (overlay) {
            return overlay;
        }

        const wrapper = document.createElement('div');
        wrapper.id = 'visualAssetLightbox';
        wrapper.className = 'fixed inset-0 z-50 hidden bg-slate-950/80 backdrop-blur-sm';
        wrapper.setAttribute('aria-hidden', 'true');

        wrapper.innerHTML = `
            <div class="flex min-h-screen items-center justify-center p-4 sm:p-6">
                <div class="relative w-full max-w-5xl overflow-hidden rounded-[1.4rem] bg-[#1b2230] text-white shadow-2xl">
                    <button type="button"
                            id="visualAssetLightboxClose"
                            class="absolute right-4 top-4 z-10 inline-flex h-11 w-11 items-center justify-center rounded-full bg-white/8 text-white transition hover:bg-white/14"
                            aria-label="Close image popup">
                        <i class="fa-solid fa-xmark text-2xl"></i>
                    </button>

                    <div class="flex max-h-[82vh] min-h-[300px] items-center justify-center bg-[#1b2230] px-4 py-14 sm:px-6">
                        <img id="visualAssetLightboxImage"
                             src=""
                             alt="Expanded image"
                             class="max-h-[68vh] w-auto max-w-full rounded-[0.9rem] object-contain" />
                    </div>

                    <div id="visualAssetLightboxCaptionWrap" class="border-t border-white/10 bg-white/6 px-5 py-4">
                        <div id="visualAssetLightboxCaption" class="text-sm font-semibold leading-6 text-slate-100"></div>
                    </div>
                </div>
            </div>`;

        document.body.appendChild(wrapper);
        return wrapper;
    }

    const visualLightbox = ensureVisualLightbox();
    const visualLightboxImage = document.getElementById('visualAssetLightboxImage');
    const visualLightboxCaption = document.getElementById('visualAssetLightboxCaption');
    const visualLightboxCaptionWrap = document.getElementById('visualAssetLightboxCaptionWrap');
    const visualLightboxClose = document.getElementById('visualAssetLightboxClose');

    function openVisualLightbox(src, alt, caption) {
        if (!visualLightbox || !visualLightboxImage) {
            return;
        }

        visualLightboxImage.src = src || '';
        visualLightboxImage.alt = alt || '';

        if (visualLightboxCaption && visualLightboxCaptionWrap) {
            if (caption && caption.trim()) {
                visualLightboxCaption.textContent = caption;
                visualLightboxCaptionWrap.classList.remove('hidden');
            } else {
                visualLightboxCaption.textContent = '';
                visualLightboxCaptionWrap.classList.add('hidden');
            }
        }

        visualLightbox.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }

    window.openVisualLightbox = openVisualLightbox;
    window.__openVisualLightboxFromButton = function (button) {
        if (!button) {
            return;
        }

        openVisualLightbox(
            button.getAttribute('data-lightbox-src'),
            button.getAttribute('data-lightbox-alt'),
            button.getAttribute('data-lightbox-caption')
        );
    };

    function closeVisualLightbox() {
        if (!visualLightbox) {
            return;
        }

        visualLightbox.classList.add('hidden');
        document.body.style.overflow = '';

        if (visualLightboxImage) {
            visualLightboxImage.src = '';
            visualLightboxImage.alt = '';
        }
    }

    document.addEventListener('click', (event) => {
        const button = event.target instanceof Element ? event.target.closest('.js-visual-lightbox') : null;
        if (!button) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        window.__openVisualLightboxFromButton(button);
    });

    if (visualLightboxClose) {
        visualLightboxClose.addEventListener('click', closeVisualLightbox);
    }

    if (visualLightbox) {
        visualLightbox.addEventListener('click', (event) => {
            if (event.target === visualLightbox) {
                closeVisualLightbox();
            }
        });
    }

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && visualLightbox && !visualLightbox.classList.contains('hidden')) {
            closeVisualLightbox();
        }
    });
}
