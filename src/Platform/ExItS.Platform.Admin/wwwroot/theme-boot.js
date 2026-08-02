// One authoritative theme boot for Platform Admin.
// Persisted preference: light | dark | system.
// Swaps Ant Design light/dark CSS; data-theme keeps the preference (system resolves for CSS).
(function () {
    "use strict";
    var THEME_KEY = "exits-admin-theme";
    var CULTURE_KEY = "exits-admin-culture";
    var ANTD_LIGHT = "/_content/AntDesign/css/ant-design-blazor.css";
    var ANTD_DARK = "/_content/AntDesign/css/ant-design-blazor.dark.css";

    function normalizePreference(value) {
        if (!value) {
            return "light";
        }
        var v = String(value).trim().toLowerCase();
        if (v === "dark" || v === "light" || v === "system") {
            return v;
        }
        return "light";
    }

    function resolveAppearance(preference) {
        if (preference === "system") {
            return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
                ? "dark"
                : "light";
        }
        return preference === "dark" ? "dark" : "light";
    }

    function setAntdStylesheet(appearance) {
        var href = appearance === "dark" ? ANTD_DARK : ANTD_LIGHT;
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
        var preference = normalizePreference(theme);
        var appearance = resolveAppearance(preference);
        var root = document.documentElement;
        root.setAttribute("data-theme", preference);
        root.classList.toggle("exits-theme-dark", appearance === "dark");
        if (document.body) {
            document.body.setAttribute("data-theme", preference);
            document.body.classList.toggle("exits-theme-dark", appearance === "dark");
        }
        root.dataset.exitsTheme = preference;
        setAntdStylesheet(appearance);
    }

    function readAndApplyTheme() {
        try {
            applyTheme(normalizePreference(window.localStorage.getItem(THEME_KEY)));
        } catch (e) {
            applyTheme("light");
        }
    }

    readAndApplyTheme();

    try {
        var culture = window.localStorage.getItem(CULTURE_KEY);
        if (culture === "fil-PH" || culture === "en") {
            document.documentElement.setAttribute("lang", culture);
        }
    } catch (e) { /* ignore */ }

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

    if (window.matchMedia) {
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function () {
            try {
                if (normalizePreference(window.localStorage.getItem(THEME_KEY)) === "system") {
                    readAndApplyTheme();
                }
            } catch (e) { /* ignore */ }
        });
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

    window.exitsAdminTheme = {
        storageKey: THEME_KEY,
        get: function (key) {
            try { return window.localStorage.getItem(key); } catch (e) { return null; }
        },
        set: function (key, value) {
            try { window.localStorage.setItem(key, value); } catch (e) { /* ignore */ }
        },
        normalize: normalizePreference,
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
                applyTheme(normalizePreference(window.localStorage.getItem(THEME_KEY)));
            } catch (e) {
                applyTheme("light");
            }
        }
    };
})();
