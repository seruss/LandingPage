// ===================================================================
// Comprehensive Client-Side Analytics Tracker
// Collects: fingerprint, screen info, scroll depth, clicks, mouse
// movement, section visibility, time-on-page, performance metrics.
// Batches events & sends every 5s + on page unload via sendBeacon.
// Client-side rate limiter: max 200 events per session.
// ===================================================================
(function () {
    'use strict';

    const CONFIG = {
        enrichEndpoint: '/api/t/e',
        eventsEndpoint: '/api/t/x',
        flushIntervalMs: 5000,
        maxEventsPerSession: 200,
        mouseSampleMs: 500,
        scrollSampleMs: 300,
    };

    // --- State ---
    let visitId = null;
    let eventQueue = [];
    let eventCount = 0;
    let maxScrollPercent = 0;
    let sessionStartTime = Date.now();
    let lastActiveTime = Date.now();
    let totalActiveTimeMs = 0;
    let isActive = true;
    let lastMouseSample = 0;
    let lastScrollSample = 0;
    let enrichSent = false;

    // --- Get visit ID from cookie ---
    function getVisitId() {
        const match = document.cookie.match(/(?:^|;\s*)_vid=([^;]*)/);
        return match ? match[1] : null;
    }

    // --- Fingerprinting ---
    function generateFingerprint() {
        const components = [];

        // Canvas fingerprint
        try {
            const canvas = document.createElement('canvas');
            canvas.width = 200;
            canvas.height = 50;
            const ctx = canvas.getContext('2d');
            ctx.textBaseline = 'top';
            ctx.font = '14px Arial';
            ctx.fillStyle = '#f60';
            ctx.fillRect(125, 1, 62, 20);
            ctx.fillStyle = '#069';
            ctx.fillText('fp_canvas_test', 2, 15);
            ctx.fillStyle = 'rgba(102, 204, 0, 0.7)';
            ctx.fillText('fp_canvas_test', 4, 17);
            components.push(canvas.toDataURL());
        } catch (e) {
            components.push('canvas_error');
        }

        // WebGL fingerprint
        try {
            const gl = document.createElement('canvas').getContext('webgl');
            if (gl) {
                const ext = gl.getExtension('WEBGL_debug_renderer_info');
                components.push(gl.getParameter(gl.VENDOR));
                components.push(gl.getParameter(gl.RENDERER));
                if (ext) {
                    components.push(gl.getParameter(ext.UNMASKED_VENDOR_WEBGL));
                    components.push(gl.getParameter(ext.UNMASKED_RENDERER_WEBGL));
                }
            }
        } catch (e) {
            components.push('webgl_error');
        }

        // AudioContext fingerprint
        try {
            const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioCtx.createOscillator();
            const analyser = audioCtx.createAnalyser();
            const gain = audioCtx.createGain();
            gain.gain.value = 0;
            oscillator.type = 'triangle';
            oscillator.connect(analyser);
            analyser.connect(gain);
            gain.connect(audioCtx.destination);

            components.push('audio_' + audioCtx.sampleRate);
            audioCtx.close();
        } catch (e) {
            components.push('audio_error');
        }

        // Stable browser properties
        components.push(navigator.userAgent);
        components.push(navigator.language);
        components.push(screen.colorDepth);
        components.push(screen.width + 'x' + screen.height);
        components.push(new Date().getTimezoneOffset());
        components.push(navigator.hardwareConcurrency || 'unknown');
        components.push(navigator.platform || 'unknown');

        // Available fonts probe
        const testFonts = ['monospace', 'sans-serif', 'serif', 'Arial', 'Courier New', 'Georgia', 'Times New Roman', 'Verdana'];
        const span = document.createElement('span');
        span.style.cssText = 'position:absolute;left:-9999px;font-size:72px;';
        span.textContent = 'mmmmmmmmmmlli';
        document.body.appendChild(span);
        const widths = testFonts.map(f => {
            span.style.fontFamily = f;
            return span.offsetWidth + ':' + span.offsetHeight;
        });
        document.body.removeChild(span);
        components.push(widths.join(','));

        // Hash the components
        return hashString(components.join('|||'));
    }

    function hashString(str) {
        let hash = 0;
        for (let i = 0; i < str.length; i++) {
            const char = str.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash = hash & hash; // Convert to 32bit int
        }
        // Make it a positive hex string
        return (hash >>> 0).toString(16).padStart(8, '0');
    }

    // --- Enrich visit with client data ---
    function sendEnrichment() {
        if (enrichSent || !visitId) return;
        enrichSent = true;

        const nav = navigator;
        const conn = nav.connection || nav.mozConnection || nav.webkitConnection;

        const data = {
            visitId: visitId,
            screenWidth: screen.width,
            screenHeight: screen.height,
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            devicePixelRatio: window.devicePixelRatio || 1,
            platform: nav.platform || nav.userAgentData?.platform || 'unknown',
            language: nav.language,
            cookiesEnabled: nav.cookieEnabled,
            doNotTrack: nav.doNotTrack === '1' || nav.doNotTrack === 'yes',
            connectionType: conn ? conn.effectiveType || conn.type : null,
            hardwareConcurrency: nav.hardwareConcurrency || null,
            deviceMemory: nav.deviceMemory || null,
            touchSupport: 'ontouchstart' in window || nav.maxTouchPoints > 0,
            colorDepth: screen.colorDepth,
            timezoneOffset: new Date().getTimezoneOffset(),
            timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
            fingerprintHash: generateFingerprint(),
        };

        // Add performance metrics (delayed slightly for accuracy)
        setTimeout(() => {
            const perf = performance.getEntriesByType('navigation')[0];
            if (perf) {
                data.pageLoadTimeMs = perf.loadEventEnd - perf.startTime;
                data.domContentLoadedMs = perf.domContentLoadedEventEnd - perf.startTime;
            }

            const paint = performance.getEntriesByType('paint');
            paint.forEach(p => {
                if (p.name === 'first-paint') data.firstPaintMs = p.startTime;
                if (p.name === 'first-contentful-paint') data.firstContentfulPaintMs = p.startTime;
            });

            // TTI approximation
            if (perf) {
                data.timeToInteractiveMs = perf.domInteractive - perf.startTime;
            }

            fetch(CONFIG.enrichEndpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data),
            }).catch(() => { /* silent */ });
        }, 2000);
    }

    // --- Event queue management ---
    function pushEvent(type, data) {
        if (eventCount >= CONFIG.maxEventsPerSession) return;

        eventQueue.push({
            visitId: visitId,
            eventType: type,
            eventData: data,
            pageUrl: window.location.pathname,
            timestamp: new Date().toISOString(),
        });
        eventCount++;
    }

    function flushEvents() {
        if (eventQueue.length === 0 || !visitId) return;

        const batch = eventQueue.splice(0);

        const payload = JSON.stringify({
            visitId: visitId,
            events: batch,
        });

        // Try fetch first, fallback to sendBeacon
        fetch(CONFIG.eventsEndpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: payload,
            keepalive: true,
        }).catch(() => {
            // If fetch fails, try sendBeacon
            try {
                navigator.sendBeacon(CONFIG.eventsEndpoint, new Blob([payload], { type: 'application/json' }));
            } catch (_) { /* silent */ }
        });
    }

    // --- Section visibility tracking ---
    function setupSectionTracking() {
        const sections = document.querySelectorAll('section, [id]');
        if (sections.length === 0) return;

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const target = entry.target;
                    const id = target.id || target.className.split(' ')[0] || target.tagName;
                    pushEvent('section_view', {
                        sectionId: id,
                        visiblePercent: Math.round(entry.intersectionRatio * 100),
                    });
                }
            });
        }, { threshold: [0.25, 0.5, 0.75, 1.0] });

        sections.forEach(s => observer.observe(s));
    }

    // --- Scroll tracking ---
    function setupScrollTracking() {
        window.addEventListener('scroll', () => {
            const now = Date.now();
            if (now - lastScrollSample < CONFIG.scrollSampleMs) return;
            lastScrollSample = now;

            const scrollTop = window.scrollY || document.documentElement.scrollTop;
            const docHeight = document.documentElement.scrollHeight - window.innerHeight;
            const percent = docHeight > 0 ? Math.round((scrollTop / docHeight) * 100) : 0;

            if (percent > maxScrollPercent) {
                maxScrollPercent = percent;
                // Only log at 25% intervals to reduce noise
                if (percent % 25 === 0 || percent === 100) {
                    pushEvent('scroll', { depthPercent: percent });
                }
            }
        }, { passive: true });
    }

    // --- Click tracking ---
    function setupClickTracking() {
        document.addEventListener('click', (e) => {
            const target = e.target.closest('a, button, [role="button"], input[type="submit"], [data-track]') || e.target;

            pushEvent('click', {
                x: e.clientX,
                y: e.clientY,
                pageX: e.pageX,
                pageY: e.pageY,
                tag: target.tagName.toLowerCase(),
                id: target.id || null,
                className: (target.className && typeof target.className === 'string')
                    ? target.className.substring(0, 100) : null,
                text: (target.textContent || '').trim().substring(0, 50) || null,
                href: target.href || null,
            });
        }, { passive: true });
    }

    // --- Mouse movement (sampled) ---
    function setupMouseTracking() {
        document.addEventListener('mousemove', (e) => {
            const now = Date.now();
            if (now - lastMouseSample < CONFIG.mouseSampleMs) return;
            lastMouseSample = now;

            pushEvent('mouse_move', {
                x: e.clientX,
                y: e.clientY,
                pageX: e.pageX,
                pageY: e.pageY,
            });
        }, { passive: true });
    }

    // --- Active/idle time tracking ---
    function setupVisibilityTracking() {
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                // Going idle
                if (isActive) {
                    totalActiveTimeMs += Date.now() - lastActiveTime;
                    isActive = false;
                }
                pushEvent('visibility_change', { state: 'hidden' });
            } else {
                // Coming back
                lastActiveTime = Date.now();
                isActive = true;
                pushEvent('visibility_change', { state: 'visible' });
            }
        });

        // Also track user inactivity (no mouse/key for 30s)
        let inactivityTimer;
        const resetInactivity = () => {
            if (!isActive) {
                lastActiveTime = Date.now();
                isActive = true;
            }
            clearTimeout(inactivityTimer);
            inactivityTimer = setTimeout(() => {
                if (isActive) {
                    totalActiveTimeMs += Date.now() - lastActiveTime;
                    isActive = false;
                }
            }, 30000);
        };

        document.addEventListener('mousemove', resetInactivity, { passive: true });
        document.addEventListener('keydown', resetInactivity, { passive: true });
        document.addEventListener('touchstart', resetInactivity, { passive: true });
        document.addEventListener('scroll', resetInactivity, { passive: true });
        resetInactivity();
    }

    // --- Page unload: send final metrics ---
    function setupUnloadTracking() {
        const sendFinal = () => {
            if (isActive) {
                totalActiveTimeMs += Date.now() - lastActiveTime;
            }

            pushEvent('unload', {
                totalTimeMs: Date.now() - sessionStartTime,
                activeTimeMs: totalActiveTimeMs,
                maxScrollPercent: maxScrollPercent,
                totalEvents: eventCount,
            });

            // Use sendBeacon for reliability on unload
            const payload = JSON.stringify({
                visitId: visitId,
                events: eventQueue.splice(0),
            });

            try {
                navigator.sendBeacon(
                    CONFIG.eventsEndpoint,
                    new Blob([payload], { type: 'application/json' })
                );
            } catch (_) { /* silent */ }
        };

        window.addEventListener('beforeunload', sendFinal);
        // iOS doesn't always fire beforeunload
        window.addEventListener('pagehide', sendFinal);
    }

    // --- Init ---
    function init() {
        visitId = getVisitId();
        if (!visitId) {
            // Cookie not yet available (first load, server hasn't set it yet)
            // Retry after a short delay
            setTimeout(() => {
                visitId = getVisitId();
                if (visitId) startTracking();
            }, 1000);
            return;
        }
        startTracking();
    }

    function startTracking() {
        // Send client enrichment data
        sendEnrichment();

        // Record initial page view event
        pushEvent('page_view', {
            url: window.location.href,
            referrer: document.referrer || null,
            title: document.title,
        });

        // Setup all trackers
        setupSectionTracking();
        setupScrollTracking();
        setupClickTracking();
        setupMouseTracking();
        setupVisibilityTracking();
        setupUnloadTracking();

        // Periodic flush
        setInterval(flushEvents, CONFIG.flushIntervalMs);
    }

    // --- Boot ---
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
