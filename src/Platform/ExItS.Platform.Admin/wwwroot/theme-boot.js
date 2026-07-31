// One authoritative theme boot for Platform Admin.
// Persisted preference: light | dark (legacy "system" → light).
// Swaps Ant Design light/dark CSS + data-theme so components and shell match.
// Blazor enhanced navigation strips dynamically-set <html> attributes unless
// reapplied via Blazor.addEventListener('enhancedload') — NOT document.addEventListener.
(function () {
    "use strict";
    var THEME_KEY = "exits-admin-theme";
    var CULTURE_KEY = "exits-admin-culture";
    var ANTD_LIGHT = "/_content/AntDesign/css/ant-design-blazor.css";
    var ANTD_DARK = "/_content/AntDesign/css/ant-design-blazor.dark.css";

    function normalizeTheme(value) {
        if (!value) {
            return "light";
        }
        var v = String(value).trim().toLowerCase();
        if (v === "dark") {
            return "dark";
        }
        // light + legacy system
        return "light";
    }

    function setAntdStylesheet(theme) {
        var href = theme === "dark" ? ANTD_DARK : ANTD_LIGHT;
        var link = document.getElementById("exits-antd-theme");
        if (!link) {
            link = document.querySelector('link[href*="ant-design-blazor"]');
            if (link) {
                link.id = "exits-antd-theme";
            }
        }
        if (!link) {
            link = document.createElement("link");
            link.id = "exits-antd-theme";
            link.rel = "stylesheet";
            document.head.appendChild(link);
        }
        if (link.getAttribute("href") !== href) {
            link.setAttribute("href", href);
        }
    }

    function applyTheme(theme) {
        var normalized = normalizeTheme(theme);
        var root = document.documentElement;
        root.setAttribute("data-theme", normalized);
        root.classList.toggle("exits-theme-dark", normalized === "dark");
        if (document.body) {
            document.body.setAttribute("data-theme", normalized);
            document.body.classList.toggle("exits-theme-dark", normalized === "dark");
        }
        root.dataset.exitsTheme = normalized;
        setAntdStylesheet(normalized);
    }

    function readAndApplyTheme() {
        try {
            var stored = window.localStorage.getItem(THEME_KEY);
            applyTheme(normalizeTheme(stored));
        } catch (e) {
            applyTheme("light");
        }
    }

    // First paint — link#exits-antd-theme should already be in <head> before this script.
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

    window.exitsAdminShell = {
        closeDrawer: function () {
            var toggle = document.getElementById("nav-drawer-toggle");
            if (toggle) {
                toggle.checked = false;
            }
        }
    };

    window.exitsAdminTheme = {
        storageKey: THEME_KEY,
        get: function (key) {
            try { return window.localStorage.getItem(key); } catch (e) { return null; }
        },
        set: function (key, value) {
            try { window.localStorage.setItem(key, value); } catch (e) { /* ignore */ }
        },
        normalize: normalizeTheme,
        applyTheme: applyTheme,
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
                applyTheme(normalizeTheme(window.localStorage.getItem(THEME_KEY)));
            } catch (e) {
                applyTheme("light");
            }
        }
    };
})();
