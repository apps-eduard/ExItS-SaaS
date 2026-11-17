import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { hideOverlay, placeTooltip, showOverlay, type TooltipPlacement } from "../overlay/overlay";
import styles from "./Tooltip.module.scss";

type TooltipProps = {
  content: string;
  children: ReactNode;
  /** Default top. Use `end` for icon rails so the tip sits beside the control. */
  placement?: TooltipPlacement;
};

export function Tooltip({ content, children, placement = "top" }: TooltipProps) {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLSpanElement>(null);
  const tipRef = useRef<HTMLSpanElement>(null);
  const timer = useRef<number | null>(null);
  const id = useId();

  function clearTimer() {
    if (timer.current) window.clearTimeout(timer.current);
    timer.current = null;
  }

  function show(delay: number) {
    clearTimer();
    timer.current = window.setTimeout(() => {
      const tip = tipRef.current;
      const anchor = anchorRef.current;
      if (!tip || !anchor) return;
      showOverlay(tip);
      placeTooltip(anchor, tip, placement);
      setOpen(true);
    }, delay);
  }

  useEffect(() => {
    if (!open) return;
    function place() {
      const tip = tipRef.current;
      const anchor = anchorRef.current;
      if (tip && anchor) placeTooltip(anchor, tip, placement);
    }
    window.addEventListener("resize", place);
    window.addEventListener("scroll", place, true);
    return () => {
      window.removeEventListener("resize", place);
      window.removeEventListener("scroll", place, true);
    };
  }, [open, placement]);

  function hide() {
    clearTimer();
    hideOverlay(tipRef.current);
    setOpen(false);
  }

  return (
    <>
      <span
        ref={anchorRef}
        className={styles.anchor}
        aria-describedby={open ? id : undefined}
        onMouseEnter={() => show(300)}
        onMouseLeave={hide}
        onFocus={() => show(40)}
        onBlur={hide}
        onKeyDown={(event) => {
          if (event.key === "Escape") hide();
        }}
      >
        {children}
      </span>
      {createPortal(
        <span ref={tipRef} id={id} role="tooltip" className={styles.tip} popover="manual">
          {content}
        </span>,
        document.body,
      )}
    </>
  );
}
