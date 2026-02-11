// Scroll-triggered reveal animations
(function () {
    'use strict';

    // IntersectionObserver for .reveal elements
    const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                // Add stagger delay for child reveals
                const parent = entry.target.closest('.reveal');
                if (parent && parent !== entry.target) {
                    const siblings = Array.from(parent.querySelectorAll(':scope > .reveal, :scope > * > .reveal'));
                    const idx = siblings.indexOf(entry.target);
                    if (idx > 0) {
                        entry.target.style.transitionDelay = `${idx * 0.1}s`;
                    }
                }
                entry.target.classList.add('visible');
                revealObserver.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.08,
        rootMargin: '0px 0px -40px 0px'
    });

    // Observe all .reveal elements
    function observeReveals() {
        document.querySelectorAll('.reveal').forEach(el => {
            revealObserver.observe(el);
        });
    }

    // Counter animation for stat numbers
    function animateCounters() {
        const counters = document.querySelectorAll('[data-count]');
        const counterObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    const target = parseInt(el.dataset.count);
                    const suffix = el.textContent.replace(/[\d]/g, '');
                    const duration = 1500;
                    const start = performance.now();

                    function update(now) {
                        const progress = Math.min((now - start) / duration, 1);
                        const eased = 1 - Math.pow(1 - progress, 3); // ease-out cubic
                        const current = Math.floor(eased * target);
                        el.textContent = current + suffix;
                        if (progress < 1) requestAnimationFrame(update);
                    }
                    requestAnimationFrame(update);
                    counterObserver.unobserve(el);
                }
            });
        }, { threshold: 0.5 });

        counters.forEach(el => counterObserver.observe(el));
    }

    // Expandable details animation
    function setupExpandables() {
        document.querySelectorAll('.expandable').forEach(details => {
            details.addEventListener('toggle', () => {
                const icon = details.querySelector('.expandable-icon');
                if (icon) {
                    icon.style.transform = details.open ? 'rotate(180deg)' : 'rotate(0deg)';
                }
            });
        });
    }

    // Parallax effect on hero grid
    function setupParallax() {
        const hero = document.querySelector('.hero-grid-bg');
        if (!hero) return;
        let ticking = false;
        window.addEventListener('scroll', () => {
            if (!ticking) {
                requestAnimationFrame(() => {
                    const scroll = window.scrollY;
                    hero.style.transform = `translateY(${scroll * 0.3}px)`;
                    ticking = false;
                });
                ticking = true;
            }
        });
    }

    // Init
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
        observeReveals();
        animateCounters();
        setupExpandables();
        setupParallax();
    }
})();
