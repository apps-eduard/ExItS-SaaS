import { useId, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { hideOverlay, placeOverlay, showOverlay } from "../overlay/overlay";
import styles from "./Tooltip.module.scss";

type TooltipProps = {
  content: string;
  children: ReactNode;
};

export function Tooltip({ content, children }: TooltipProps) {
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
      placeOverlay(anchor, tip, "top");
      setOpen(true);
    }, delay);
  }

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
