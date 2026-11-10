export function showOverlay(element: HTMLElement | null) {
  if (!element) return;
  const withPopover = element as HTMLElement & { showPopover?: () => void };
  if (typeof withPopover.showPopover === "function") {
    try {
      if (!element.matches(":popover-open")) withPopover.showPopover();
      return;
    } catch {
      element.dataset.open = "true";
      return;
    }
  }
  element.dataset.open = "true";
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
  const gap = 8;
  const margin = 8;
  let top = prefer === "bottom" ? anchorBox.bottom + gap : anchorBox.top - overlayBox.height - gap;
  let left = anchorBox.left;
  if (top + overlayBox.height > window.innerHeight - margin) top = anchorBox.top - overlayBox.height - gap;
  if (top < margin) top = anchorBox.bottom + gap;
  if (top < margin) top = margin;
  if (left + overlayBox.width > window.innerWidth - margin) left = window.innerWidth - overlayBox.width - margin;
  if (left < margin) left = margin;
  overlay.style.top = `${Math.round(top)}px`;
  overlay.style.left = `${Math.round(left)}px`;
}
