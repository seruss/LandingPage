// ===================================================================
// Client-Side Analytics Tracker (Enrichment Only)
// Collects: fingerprint, screen info, performance metrics.
// ===================================================================
(function () {
    'use strict';

    const CONFIG = {
        enrichEndpoint: '/api/t/e',
    };

    // --- State ---
    let visitId = null;
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
    }

    // --- Boot ---
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
