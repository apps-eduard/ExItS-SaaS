import { useEffect, useLayoutEffect, useRef } from "react";
import { hideOverlay, placeOverlay, showOverlay } from "../overlay/overlay";

const APPEARANCE_ATTRS = ["data-theme", "data-palette", "data-radius", "data-density"] as const;

function applyScopedAppearance(anchor: HTMLElement, popover: HTMLElement) {
  for (const name of APPEARANCE_ATTRS) {
    popover.removeAttribute(name);
    const host = anchor.closest(`[${name}]`);
    if (!host || host === document.documentElement) continue;
    const value = host.getAttribute(name);
    if (value) popover.setAttribute(name, value);
  }
}

export function useDateSurface(open: boolean, compact: boolean) {
  const anchorRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const popoverRef = useRef<HTMLDivElement>(null);

  useLayoutEffect(() => {
    if (!open || compact) return;
    const popover = popoverRef.current;
    const anchor = anchorRef.current;
    if (!popover || !anchor) return;
    applyScopedAppearance(anchor, popover);
    showOverlay(popover);
    placeOverlay(anchor, popover, "bottom");
    popover.querySelector<HTMLElement>("[data-autofocus='true']")?.focus();
  }, [open, compact]);

  useEffect(() => {
    if (!open) {
      hideOverlay(popoverRef.current);
      return;
    }
    if (compact) return;
    function onPlace() {
      if (anchorRef.current && popoverRef.current) placeOverlay(anchorRef.current, popoverRef.current, "bottom");
    }
    window.addEventListener("resize", onPlace);
    window.addEventListener("scroll", onPlace, true);
    return () => {
      window.removeEventListener("resize", onPlace);
      window.removeEventListener("scroll", onPlace, true);
    };
  }, [open, compact]);

  useEffect(() => {
    if (!open || !compact) return;
    const timer = window.setTimeout(() => {
      document.querySelector<HTMLElement>("[data-autofocus='true']")?.focus();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [open, compact]);

  return { anchorRef, triggerRef, popoverRef };
}
