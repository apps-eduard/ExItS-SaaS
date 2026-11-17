/**
 * Chrome starts a CSS animation while a popover is still `display: none`, then
 * shows the finished frame. Restart it after the popover is actually open.
 * Reduced motion stays off: the stylesheet animation name is `none`.
 */
export function replayOverlayMotion(element: HTMLElement | null) {
  if (!element) return;
  if (typeof window.matchMedia === "function" && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  const name = getComputedStyle(element).animationName;
  if (!name || name === "none") return;
  element.style.animationName = "none";
  void element.offsetWidth;
  element.style.removeProperty("animation-name");
}

export function showOverlay(element: HTMLElement | null) {
  if (!element) return;
  const withPopover = element as HTMLElement & { showPopover?: () => void };
  let revealed = false;
  if (typeof withPopover.showPopover === "function") {
    try {
      if (!element.matches(":popover-open")) {
        withPopover.showPopover();
        revealed = true;
      }
    } catch {
      revealed = element.dataset.open !== "true";
      element.dataset.open = "true";
    }
  } else if (element.dataset.open !== "true") {
    element.dataset.open = "true";
    revealed = true;
  }
  if (revealed) replayOverlayMotion(element);
}

export function hideOverlay(element: HTMLElement | null) {
  if (!element) return;
  const withPopover = element as HTMLElement & { hidePopover?: () => void };
  if (typeof withPopover.hidePopover === "function") {
    try {
      if (element.matches(":popover-open")) withPopover.hidePopover();
    } catch {
      delete element.dataset.open;
    }
  }
  delete element.dataset.open;
}

export function placeOverlay(anchor: HTMLElement, overlay: HTMLElement, prefer: "top" | "bottom" = "bottom") {
  const anchorBox = anchor.getBoundingClientRect();
  const overlayBox = overlay.getBoundingClientRect();
  const placed = placeAnchored(anchorBox, overlayBox, prefer, 8);
  overlay.style.top = `${placed.top}px`;
  overlay.style.left = `${placed.left}px`;
}

export type TooltipPlacement = "top" | "end";

/** Tooltip placement. Same edge rules as `placeOverlay`, with room for the tail. */
export function placeTooltip(anchor: HTMLElement, overlay: HTMLElement, prefer: TooltipPlacement = "top") {
  const anchorBox = anchor.getBoundingClientRect();
  const overlayBox = overlay.getBoundingClientRect();
  if (prefer === "end") {
    const placed = placeBeside(anchorBox, overlayBox, 10);
    const height = overlayBox.height;
    const inset = height > 0 ? Math.min(14, height / 2) : 0;
    const center = anchorBox.top + anchorBox.height / 2 - placed.top;
    const tail = height > 0 ? Math.round(Math.max(inset, Math.min(center, height - inset))) : 0;
    overlay.style.top = `${placed.top}px`;
    overlay.style.left = `${placed.left}px`;
    overlay.dataset.side = placed.side;
    overlay.style.setProperty("--tooltip-tail", `${tail}px`);
    return;
  }

  const placed = placeAnchored(anchorBox, overlayBox, "top", 10);
  const width = overlayBox.width;
  const inset = width > 0 ? Math.min(14, width / 2) : 0;
  const center = anchorBox.left + anchorBox.width / 2 - placed.left;
  const tail = width > 0 ? Math.round(Math.max(inset, Math.min(center, width - inset))) : 0;
  overlay.style.top = `${placed.top}px`;
  overlay.style.left = `${placed.left}px`;
  overlay.dataset.side = placed.side;
  overlay.style.setProperty("--tooltip-tail", `${tail}px`);
}

function placeBeside(anchorBox: DOMRect, overlayBox: DOMRect, gap: number) {
  const margin = 8;
  let left = anchorBox.right + gap;
  let top = anchorBox.top + anchorBox.height / 2 - overlayBox.height / 2;
  let side: "end" | "start" = "end";
  if (left + overlayBox.width > window.innerWidth - margin) {
    left = anchorBox.left - overlayBox.width - gap;
    side = "start";
  }
  if (left < margin) {
    left = margin;
    side = "end";
  }
  if (top + overlayBox.height > window.innerHeight - margin) top = window.innerHeight - overlayBox.height - margin;
  if (top < margin) top = margin;
  return { top: Math.round(top), left: Math.round(left), side };
}

function placeAnchored(anchorBox: DOMRect, overlayBox: DOMRect, prefer: "top" | "bottom", gap: number) {
  const margin = 8;
  let top = prefer === "bottom" ? anchorBox.bottom + gap : anchorBox.top - overlayBox.height - gap;
  let left = anchorBox.left;
  if (top + overlayBox.height > window.innerHeight - margin) top = anchorBox.top - overlayBox.height - gap;
  if (top < margin) top = anchorBox.bottom + gap;
  if (top < margin) top = margin;
  if (left + overlayBox.width > window.innerWidth - margin) left = window.innerWidth - overlayBox.width - margin;
  if (left < margin) left = margin;
  const side = top >= anchorBox.bottom - 1 ? "bottom" : "top";
  return { top: Math.round(top), left: Math.round(left), side };
}
