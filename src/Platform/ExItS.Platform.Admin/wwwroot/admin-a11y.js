/* Platform Admin accessibility helpers (dialogs + drawer). No framework dependency. */
(function () {
  "use strict";

  var previousFocus = null;
  var activeDialog = null;
  var keyHandler = null;

  function focusable(root) {
    if (!root) return [];
    return Array.prototype.slice.call(
      root.querySelectorAll('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])')
    ).filter(function (el) {
      return el.offsetParent !== null || el === document.activeElement;
    });
  }

  function dialogOpen(dialogEl) {
    if (!dialogEl) return;
    previousFocus = document.activeElement;
    activeDialog = dialogEl;
    if (keyHandler) {
      dialogEl.removeEventListener("keydown", keyHandler);
    }
    keyHandler = function (e) {
      if (e.key !== "Tab") return;
      var list = focusable(dialogEl);
      if (list.length === 0) {
        e.preventDefault();
        return;
      }
      var first = list[0];
      var last = list[list.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    };
    dialogEl.addEventListener("keydown", keyHandler);
  }

  function dialogClose() {
    if (activeDialog && keyHandler) {
      activeDialog.removeEventListener("keydown", keyHandler);
    }
    activeDialog = null;
    keyHandler = null;
    var restore = previousFocus;
    previousFocus = null;
    if (restore && typeof restore.focus === "function") {
      try {
        restore.focus();
      } catch (_) {
        /* ignore */
      }
    }
  }

  function syncDrawerToggle() {
    var cb = document.getElementById("nav-drawer-toggle");
    var openBtn = document.querySelector(".menu-toggle");
    var closeBtn = document.querySelector(".drawer-close");
    var sidebar = document.getElementById("app-sidebar");
    if (!cb) return;
    var expanded = !!cb.checked;
    if (openBtn) {
      openBtn.setAttribute("aria-expanded", expanded ? "true" : "false");
      openBtn.setAttribute("aria-controls", "app-sidebar");
    }
    if (closeBtn) {
      closeBtn.setAttribute("aria-expanded", expanded ? "true" : "false");
      closeBtn.setAttribute("aria-controls", "app-sidebar");
    }
    if (sidebar) {
      if (window.matchMedia("(max-width: 1024px)").matches) {
        sidebar.setAttribute("aria-hidden", expanded ? "false" : "true");
      } else {
        sidebar.removeAttribute("aria-hidden");
      }
    }
  }

  function bindDrawer() {
    var cb = document.getElementById("nav-drawer-toggle");
    if (!cb || cb.dataset.a11yBound === "1") return;
    cb.dataset.a11yBound = "1";
    cb.addEventListener("change", syncDrawerToggle);
    syncDrawerToggle();
  }

  window.exitsAdminA11y = {
    dialogOpen: dialogOpen,
    dialogClose: dialogClose,
    syncDrawerToggle: syncDrawerToggle,
    bindDrawer: bindDrawer
  };

  function boot() {
    bindDrawer();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }

  document.addEventListener("enhancedload", boot);
  if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
    window.Blazor.addEventListener("enhancedload", boot);
  }
})();
