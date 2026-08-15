/* Platform Admin accessibility helpers for ConfirmDialog focus trap.
   Legacy drawer focus helpers were removed — Ant Design Drawer owns that UX. */
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

  window.exitsAdminA11y = {
    dialogOpen: dialogOpen,
    dialogClose: dialogClose
  };

  window.exitsDownloadBase64 = function (fileName, base64, contentType) {
    try {
      var binary = atob(base64);
      var bytes = new Uint8Array(binary.length);
      for (var i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
      }
      var blob = new Blob([bytes], { type: contentType || "application/octet-stream" });
      var url = URL.createObjectURL(blob);
      var a = document.createElement("a");
      a.href = url;
      a.download = fileName || "download";
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (e) {
      console.error("exitsDownloadBase64 failed", e);
    }
  };
  window.exitsFillTestUserLogin = function (username) {
    var user = document.getElementById("login-email");
    var pass = document.getElementById("login-password");
    if (user) {
      user.value = username || "";
      user.dispatchEvent(new Event("input", { bubbles: true }));
      user.dispatchEvent(new Event("change", { bubbles: true }));
    }
    if (pass) {
      pass.value = "";
      pass.dispatchEvent(new Event("input", { bubbles: true }));
      pass.dispatchEvent(new Event("change", { bubbles: true }));
    }
  };
})();
