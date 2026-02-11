// Theme toggle — dark / light
(function () {
    'use strict';

    const STORAGE_KEY = 'theme';

    function getPreferred() {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored === 'dark' || stored === 'light') return stored;

        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    function apply(theme) {
        document.documentElement.dataset.theme = theme;
    }

    function toggle() {
        const current = document.documentElement.dataset.theme || getPreferred();
        const next = current === 'dark' ? 'light' : 'dark';
        localStorage.setItem(STORAGE_KEY, next);
        apply(next);
    }

    // Listen for OS-level preference changes (only when no explicit choice)
    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
            if (!localStorage.getItem(STORAGE_KEY)) {
                apply(e.matches ? 'dark' : 'light');
            }
        });
    }

    // Apply on load (fallback — inline script in <head> handles the initial paint)
    apply(getPreferred());

    // Expose toggle globally for the button onclick
    window.themeToggle = toggle;
})();
