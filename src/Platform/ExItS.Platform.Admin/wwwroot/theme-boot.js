// Applies persisted theme + culture before CSS/render to avoid a flash of the wrong theme.
// Must load synchronously in <head>, before app.css.
// Authoritative storage values: system | light | dark (legacy PascalCase still accepted).
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
        document.documentElement.setAttribute("data-theme", normalizeTheme(theme));
    }

    function readAndApplyTheme() {
        try {
            applyTheme(window.localStorage.getItem(THEME_KEY));
        } catch (e) {
            applyTheme("system");
        }
    }

    readAndApplyTheme();

    try {
        var culture = window.localStorage.getItem(CULTURE_KEY);
        if (culture === "fil-PH" || culture === "en") {
            document.documentElement.setAttribute("lang", culture);
        }
    } catch (e) {
        /* ignore — fall back to server-rendered lang attribute */
    }

    // Blazor enhanced navigation can replace document attributes from SSR HTML
    // without re-running this boot script — re-apply from storage.
    document.addEventListener("enhancedload", function () {
        readAndApplyTheme();
    });

    window.addEventListener("pageshow", function () {
        readAndApplyTheme();
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
        document.documentElement.setAttribute("data-theme", window.exitsAdminTheme.normalize(theme));
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
