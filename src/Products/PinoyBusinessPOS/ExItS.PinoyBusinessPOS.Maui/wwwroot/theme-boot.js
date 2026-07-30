// P5-WP01: applies the persisted theme + culture (mirrored from MAUI Preferences into
// localStorage by ThemeController/CultureController) before CSS/render, to avoid a flash of the
// wrong theme or language on WebView (re)load. Must be loaded synchronously in <head>, before
// exits-design-system.css and app.css. Preferences.Default is not reachable from JS before the
// WebView boots, so the very first load always falls back to "system" until Blazor's first
// render calls exitsPosTheme.applyTheme/applyCulture with the real persisted value.
(function () {
    "use strict";
    var THEME_KEY = "exits-pos-theme";
    var CULTURE_KEY = "exits-pos-culture";

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
        /* ignore — fall back to the default lang attribute */
    }
})();

window.exitsPosTheme = {
    applyTheme: function (theme) {
        var normalized = theme === "Light" ? "light" : theme === "Dark" ? "dark" : "system";
        document.documentElement.setAttribute("data-theme", normalized);
        try {
            window.localStorage.setItem("exits-pos-theme", theme);
        } catch (e) {
            /* ignore */
        }
    },
    applyCulture: function (culture) {
        if (!culture) {
            return;
        }
        document.documentElement.setAttribute("lang", culture);
        try {
            window.localStorage.setItem("exits-pos-culture", culture);
        } catch (e) {
            /* ignore */
        }
    },
    prefersDark: function () {
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
    }
};
