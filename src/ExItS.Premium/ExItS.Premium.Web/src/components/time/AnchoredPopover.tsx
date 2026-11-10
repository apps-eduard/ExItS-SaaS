import { useEffect, useLayoutEffect, useRef, type ReactNode, type RefObject } from "react";
import { createPortal } from "react-dom";
import { hideOverlay, placeOverlay, showOverlay } from "../overlay/overlay";
import styles from "./AnchoredPopover.module.scss";

const FOCUSABLE = "button:not(:disabled), input:not(:disabled):not([type='hidden']), select:not(:disabled), textarea:not(:disabled)";

type AnchoredPopoverProps = {
  open: boolean;
  anchorRef: RefObject<HTMLElement | null>;
  label: string;
  describedBy?: string;
  onCancel: () => void;
  wide?: boolean;
  placementKey?: string;
  children: ReactNode;
};

function focusableItems(panel: HTMLElement) {
  return [...panel.querySelectorAll<HTMLElement>(FOCUSABLE)].sort((left, right) => {
    const position = left.compareDocumentPosition(right);
    if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
    if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
    return 0;
  });
}

export function AnchoredPopover({ open, anchorRef, label, describedBy, onCancel, placementKey = "", wide = false, children }: AnchoredPopoverProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const onCancelRef = useRef(onCancel);
  const wasOpen = useRef(false);

  useEffect(() => {
    onCancelRef.current = onCancel;
  }, [onCancel]);

  useLayoutEffect(() => {
    const panel = panelRef.current;
    const anchor = anchorRef.current;
    if (!open || !panel || !anchor) return;
    showOverlay(panel);
    placeOverlay(anchor, panel, "bottom");
  }, [anchorRef, open, placementKey]);

  useEffect(() => {
    if (!open) {
      hideOverlay(panelRef.current);
      if (wasOpen.current) {
        wasOpen.current = false;
        anchorRef.current?.focus();
      }
      return;
    }
    wasOpen.current = true;
    const panel = panelRef.current;
    const preferred = panel?.querySelector<HTMLElement>("[data-autofocus]");
    const first = preferred ?? (panel ? focusableItems(panel)[0] : undefined);
    first?.focus();
  }, [anchorRef, open]);

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: PointerEvent) {
      const target = event.target;
      if (!(target instanceof Node)) return;
      const insidePanel = panelRef.current?.contains(target) ?? false;
      const insideAnchor = anchorRef.current?.contains(target) ?? false;
      if (insidePanel || insideAnchor) return;
      onCancelRef.current();
    }
    function onPlace() {
      const panel = panelRef.current;
      const anchor = anchorRef.current;
      if (panel && anchor) placeOverlay(anchor, panel, "bottom");
    }
    document.addEventListener("pointerdown", onPointerDown);
    window.addEventListener("resize", onPlace);
    window.addEventListener("scroll", onPlace, true);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      window.removeEventListener("resize", onPlace);
      window.removeEventListener("scroll", onPlace, true);
    };
  }, [anchorRef, open]);

  return createPortal(
    <div
      ref={panelRef}
      className={wide ? `${styles.panel} ${styles.wide}` : styles.panel}
      popover="manual"
      role="dialog"
      aria-label={label}
      aria-describedby={describedBy}
      hidden={!open}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.preventDefault();
          onCancelRef.current();
          return;
        }
        if (event.key !== "Tab") return;
        const items = focusableItems(event.currentTarget);
        if (items.length === 0) return;
        const active = document.activeElement;
        const leavingForward = !event.shiftKey && active === items[items.length - 1];
        const leavingBackward = event.shiftKey && active === items[0];
        if (leavingForward || leavingBackward) {
          event.preventDefault();
          onCancelRef.current();
        }
      }}
    >
      {children}
    </div>,
    document.body,
  );
}
