// One authoritative theme boot for Platform Admin.
// Persisted preference: system | light | dark (legacy PascalCase accepted).
// Blazor enhanced navigation strips dynamically-set <html> attributes unless
 // re-applied via Blazor.addEventListener('enhancedload') — NOT document.addEventListener.
(function () {
    "use strict";
    var THEME_KEY = "exits-admin-theme";
    var CULTURE_KEY = "exits-admin-culture";

    function normalizeTheme(value) {
        if (!value) {
            return "system";
        }
        var v = String(value).trim().toLowerCase();
        if (v === "light" || v === "dark" || v === "system") {
            return v;
        }
        return "system";
    }

    function applyTheme(theme) {
        var normalized = normalizeTheme(theme);
        var root = document.documentElement;
        root.setAttribute("data-theme", normalized);
        // Mirror on body for selectors that target body and to survive partial merges.
        if (document.body) {
            document.body.setAttribute("data-theme", normalized);
        }
        root.dataset.exitsTheme = normalized;
    }

    function readAndApplyTheme() {
        try {
            applyTheme(window.localStorage.getItem(THEME_KEY));
        } catch (e) {
            applyTheme("system");
        }
    }

    // First paint — before CSS if this script is in <head>.
    readAndApplyTheme();

    try {
        var culture = window.localStorage.getItem(CULTURE_KEY);
        if (culture === "fil-PH" || culture === "en") {
            document.documentElement.setAttribute("lang", culture);
        }
    } catch (e) {
        /* ignore */
    }

    function attachBlazorEnhancedLoad() {
        if (!window.Blazor || typeof window.Blazor.addEventListener !== "function") {
            return false;
        }
        if (window.__exitsThemeEnhancedBound) {
            return true;
        }
        window.Blazor.addEventListener("enhancedload", function () {
            readAndApplyTheme();
        });
        window.__exitsThemeEnhancedBound = true;
        return true;
    }

    function scheduleBlazorHook() {
        if (attachBlazorEnhancedLoad()) {
            return;
        }
        var attempts = 0;
        var timer = window.setInterval(function () {
            attempts += 1;
            if (attachBlazorEnhancedLoad() || attempts >= 200) {
                window.clearInterval(timer);
            }
        }, 25);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () {
            readAndApplyTheme();
            scheduleBlazorHook();
        });
    } else {
        scheduleBlazorHook();
    }

    window.addEventListener("pageshow", function () {
        readAndApplyTheme();
        scheduleBlazorHook();
    });
})();

window.exitsAdminShell = {
    closeDrawer: function () {
        var toggle = document.getElementById("nav-drawer-toggle");
        if (toggle) {
            toggle.checked = false;
        }
    }
};

window.exitsAdminTheme = {
    storageKey: "exits-admin-theme",
    get: function (key) {
        try { return window.localStorage.getItem(key); } catch (e) { return null; }
    },
    set: function (key, value) {
        try { window.localStorage.setItem(key, value); } catch (e) { /* ignore */ }
    },
    normalize: function (theme) {
        if (!theme) {
            return "system";
        }
        var v = String(theme).trim().toLowerCase();
        return (v === "light" || v === "dark" || v === "system") ? v : "system";
    },
    applyTheme: function (theme) {
        var normalized = window.exitsAdminTheme.normalize(theme);
        var root = document.documentElement;
        root.setAttribute("data-theme", normalized);
        if (document.body) {
            document.body.setAttribute("data-theme", normalized);
        }
        root.dataset.exitsTheme = normalized;
    },
    applyCulture: function (culture) {
        if (culture) {
            document.documentElement.setAttribute("lang", culture);
        }
    },
    prefersDark: function () {
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
    },
    reapplyFromStorage: function () {
        try {
            window.exitsAdminTheme.applyTheme(window.localStorage.getItem("exits-admin-theme"));
        } catch (e) {
            window.exitsAdminTheme.applyTheme("system");
        }
    }
};
