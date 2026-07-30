// P5-WP02: applies persisted theme, density, and culture (mirrored from MAUI Preferences into
// WebView storage by ThemeController/DensityController/CultureController) before CSS/render,
// to avoid a flash of the wrong theme or density on WebView (re)load. Must be loaded
// synchronously in <head>, before exits-design-system.css and app.css.
(function () {
    "use strict";
    var THEME_KEY = "exits-pos-theme";
    var DENSITY_KEY = "exits-pos-density";
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
        var density = window.localStorage.getItem(DENSITY_KEY);
        if (density === "Comfortable") {
            document.documentElement.setAttribute("data-density", "comfortable");
        } else {
            document.documentElement.setAttribute("data-density", "compact");
        }
    } catch (e) {
        document.documentElement.setAttribute("data-density", "compact");
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
    applyDensity: function (density) {
        var normalized = density === "Comfortable" ? "comfortable" : "compact";
        document.documentElement.setAttribute("data-density", normalized);
        try {
            window.localStorage.setItem("exits-pos-density", density);
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
