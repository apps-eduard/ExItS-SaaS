// P4-WP04: applies persisted theme + culture before CSS/render to avoid a flash of
// the wrong theme or language. Must be loaded synchronously in <head>, before app.css.
(function () {
    "use strict";
    var THEME_KEY = "exits-admin-theme";
    var CULTURE_KEY = "exits-admin-culture";

    try {
        var theme = window.localStorage.getItem(THEME_KEY);
        if (theme === "Light") {
            document.documentElement.setAttribute("data-theme", "light");
        } else if (theme === "Dark") {
            document.documentElement.setAttribute("data-theme", "dark");
        } else {
            document.documentElement.setAttribute("data-theme", "system");
        }
    } catch (e) {
        document.documentElement.setAttribute("data-theme", "system");
    }

    try {
        var culture = window.localStorage.getItem(CULTURE_KEY);
        if (culture === "fil-PH" || culture === "en") {
            document.documentElement.setAttribute("lang", culture);
        }
    } catch (e) {
        /* ignore — fall back to server-rendered lang attribute */
    }
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
    applyTheme: function (theme) {
        var normalized = theme === "Light" ? "light" : theme === "Dark" ? "dark" : "system";
        document.documentElement.setAttribute("data-theme", normalized);
    },
    applyCulture: function (culture) {
        if (culture) {
            document.documentElement.setAttribute("lang", culture);
        }
    },
    prefersDark: function () {
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
    }
};
