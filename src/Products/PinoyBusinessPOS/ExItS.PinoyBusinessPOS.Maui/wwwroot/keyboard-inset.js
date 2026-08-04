/**
 * Keeps focused inputs visible above the Android soft keyboard in Blazor Hybrid.
 * Uses visualViewport inset + scrollIntoView against the POS scroll region (#pos-main).
 */
(function () {
    var KEYBOARD_OPEN_PX = 80;
    var scrollTimer = 0;

    function isEditable(el) {
        if (!(el instanceof HTMLElement)) {
            return false;
        }

        var tag = el.tagName;
        if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT") {
            return true;
        }

        return el.isContentEditable === true;
    }

    function scrollFocusedIntoView(target) {
        if (!isEditable(target)) {
            return;
        }

        window.clearTimeout(scrollTimer);
        scrollTimer = window.setTimeout(function () {
            try {
                target.scrollIntoView({
                    block: "center",
                    inline: "nearest",
                    behavior: "smooth"
                });
            } catch (_) {
                target.scrollIntoView(true);
            }
        }, 120);
    }

    function applyKeyboardInset() {
        var inset = 0;
        if (window.visualViewport) {
            inset = Math.max(
                0,
                window.innerHeight - window.visualViewport.height - window.visualViewport.offsetTop
            );
        }

        document.documentElement.style.setProperty("--pos-keyboard-inset", inset + "px");
        document.documentElement.classList.toggle("pos-keyboard-open", inset > KEYBOARD_OPEN_PX);

        if (inset > KEYBOARD_OPEN_PX) {
            scrollFocusedIntoView(document.activeElement);
        }
    }

    document.addEventListener(
        "focusin",
        function (event) {
            scrollFocusedIntoView(event.target);
            window.setTimeout(applyKeyboardInset, 250);
        },
        true
    );

    document.addEventListener(
        "focusout",
        function () {
            window.setTimeout(applyKeyboardInset, 200);
        },
        true
    );

    if (window.visualViewport) {
        window.visualViewport.addEventListener("resize", applyKeyboardInset);
        window.visualViewport.addEventListener("scroll", applyKeyboardInset);
    }

    window.addEventListener("resize", applyKeyboardInset);
    applyKeyboardInset();
})();
