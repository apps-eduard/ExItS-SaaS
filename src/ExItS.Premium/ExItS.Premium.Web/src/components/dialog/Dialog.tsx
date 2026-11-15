import { useEffect, useId, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";
import { ExIcon } from "../../icons/ExIcon";
import type { IconName } from "../../icons/registry";
import { Button } from "../button/Button";
import {
  DIALOG_TRANSITION_DEFAULT,
  resolveDialogMotion,
  type DialogTransition,
} from "./dialog-transition";
import styles from "./Dialog.module.scss";

export type DialogSize = "sm" | "md" | "lg";
export type DialogTone = "primary" | "info" | "success" | "warning" | "danger";
/** Centered on all widths, or bottom sheet below 640px (forms). Confirmations stay centered. */
export type DialogMobilePresentation = "center" | "sheet";
export type { DialogTransition };

type DialogProps = {
  open: boolean;
  title: string;
  description?: string;
  onClose: () => void;
  children?: ReactNode;
  footer?: ReactNode;
  size?: DialogSize;
  /** When false, backdrop and Escape do not dismiss. Header Close still works. Default true. */
  dismissible?: boolean;
  /** Optional leading icon in the header (tone styles the container). */
  icon?: IconName | ReactNode;
  iconTone?: DialogTone;
  /** Mobile layout. `sheet` docks to the bottom under 640px; desktop stays centered. */
  mobilePresentation?: DialogMobilePresentation;
  /** Header bottom divider line. Default true. */
  lineTop?: boolean;
  /** Footer top divider line. Default true. */
  lineBottom?: boolean;
  /** Soft gray footer surface. Default true. */
  grayFooter?: boolean;
  /** Enter/exit motion recipe. Default `spring` (Inspira). */
  transition?: DialogTransition;
};

function showDialog(dialog: HTMLDialogElement) {
  try {
    if (!dialog.open) dialog.showModal();
  } catch {
    dialog.setAttribute("open", "");
  }
}

function hideDialog(dialog: HTMLDialogElement) {
  if (dialog.open && typeof dialog.close === "function") dialog.close();
  dialog.removeAttribute("open");
  delete dialog.dataset.closing;
  dialog.style.removeProperty("--vv-bottom");
}

function isIconName(value: unknown): value is IconName {
  return typeof value === "string";
}

function useCompactViewport() {
  const [compact, setCompact] = useState(() => {
    if (typeof window === "undefined" || typeof window.matchMedia !== "function") return false;
    return window.matchMedia("(max-width: 639px)").matches;
  });

  useEffect(() => {
    if (typeof window.matchMedia !== "function") return;
    const media = window.matchMedia("(max-width: 639px)");
    const sync = () => setCompact(media.matches);
    sync();
    media.addEventListener("change", sync);
    return () => media.removeEventListener("change", sync);
  }, []);

  return compact;
}

export function Dialog({
  open,
  title,
  description,
  onClose,
  children,
  footer,
  size = "md",
  dismissible = true,
  icon,
  iconTone = "primary",
  mobilePresentation = "center",
  lineTop = true,
  lineBottom = true,
  grayFooter = true,
  transition: transitionPreset = DIALOG_TRANSITION_DEFAULT,
}: DialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const restoreRef = useRef<HTMLElement | null>(null);
  const requestedRef = useRef(false);
  const closingRef = useRef(false);
  const titleId = useId();
  const descriptionId = useId();
  const reduceMotion = useReducedMotion();
  const compact = useCompactViewport();
  const useSheet = mobilePresentation === "sheet" && compact;
  /** closed → mounted (laid out at initial) → open → closing */
  const [phase, setPhase] = useState<"closed" | "mounted" | "open" | "closing">("closed");
  /** Remount the panel each open so sheet % y never inherits a stale desktop/open transform. */
  const [openCycle, setOpenCycle] = useState(0);

  function requestClose() {
    if (requestedRef.current) return;
    requestedRef.current = true;
    onClose();
    window.setTimeout(() => {
      if (dialogRef.current?.open) requestedRef.current = false;
    }, 0);
  }

  function finishClose() {
    if (!closingRef.current) return;
    closingRef.current = false;
    const dialog = dialogRef.current;
    if (dialog) hideDialog(dialog);
    setPhase("closed");
    restoreRef.current?.focus();
    restoreRef.current = null;
  }

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open) {
      requestedRef.current = false;
      closingRef.current = false;
      if (document.activeElement instanceof HTMLElement && !dialog.contains(document.activeElement)) {
        restoreRef.current = document.activeElement;
      }
      setOpenCycle((cycle) => cycle + 1);
      showDialog(dialog);
      // Lay out at the off-screen initial pose first, then spring in.
      // Percentage y must resolve against a real panel height; remount clears stale transforms.
      setPhase("mounted");
      return;
    }

    setPhase((current) => {
      if (current === "closed") return current;
      closingRef.current = true;
      dialog.dataset.closing = "true";
      return "closing";
    });
  }, [open]);

  // After the panel has laid out at y:100%, advance to open (setTimeout survives rAF throttling).
  useEffect(() => {
    if (phase !== "mounted") return;
    const timer = window.setTimeout(() => {
      setPhase((current) => (current === "mounted" ? "open" : current));
    }, 0);
    return () => window.clearTimeout(timer);
  }, [phase]);

  useLayoutEffect(() => {
    if (phase !== "mounted" && phase !== "open") return;
    const dialog = dialogRef.current;
    if (!dialog?.open) return;
    const target =
      dialog.querySelector<HTMLElement>("[data-autofocus]") ??
      dialog.querySelector<HTMLElement>("button, input, select, textarea, a[href]");
    target?.focus();
  }, [phase, openCycle]);

  useEffect(() => {
    if (phase !== "closing") return;
    const timer = window.setTimeout(finishClose, reduceMotion ? 20 : 700);
    return () => window.clearTimeout(timer);
  }, [phase, reduceMotion]);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog || !open || !useSheet) {
      dialog?.style.removeProperty("--vv-bottom");
      return;
    }

    const viewport = window.visualViewport;
    if (!viewport) return;

    const syncKeyboardInset = () => {
      const inset = Math.max(0, window.innerHeight - viewport.height - viewport.offsetTop);
      dialog.style.setProperty("--vv-bottom", `${inset}px`);
    };

    syncKeyboardInset();
    viewport.addEventListener("resize", syncKeyboardInset);
    viewport.addEventListener("scroll", syncKeyboardInset);
    return () => {
      viewport.removeEventListener("resize", syncKeyboardInset);
      viewport.removeEventListener("scroll", syncKeyboardInset);
      dialog.style.removeProperty("--vv-bottom");
    };
  }, [open, useSheet]);

  const { variants, transition: panelTransition } = resolveDialogMotion({
    transition: transitionPreset,
    sheet: useSheet,
    reduced: Boolean(reduceMotion),
    closing: phase === "closing",
  });

  const animatePhase = phase === "open" ? "open" : phase === "closing" ? "exit" : "initial";

  const iconNode = icon ? (isIconName(icon) ? <ExIcon name={icon} size={22} /> : icon) : null;

  return (
    <dialog
      ref={dialogRef}
      className={styles.stage}
      data-size={size}
      data-mobile={mobilePresentation}
      data-motion={useSheet ? "sheet" : "desktop"}
      data-transition={transitionPreset}
      data-phase={phase}
      data-line-top={lineTop ? "true" : "false"}
      data-line-bottom={lineBottom ? "true" : "false"}
      data-gray-footer={grayFooter ? "true" : "false"}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => {
        event.preventDefault();
        if (dismissible) requestClose();
      }}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.preventDefault();
          if (dismissible) requestClose();
        }
      }}
      onClick={(event) => {
        if (!dismissible) return;
        if (event.target !== event.currentTarget) return;
        requestClose();
      }}
    >
      <motion.div
        key={openCycle}
        ref={panelRef}
        className={styles.panel}
        variants={variants}
        initial="initial"
        animate={animatePhase}
        transition={panelTransition}
        onAnimationComplete={() => {
          if (phase === "closing") finishClose();
        }}
        onClick={(event) => event.stopPropagation()}
      >
        <header className={styles.header}>
          {iconNode ? (
            <span className={styles.icon} data-tone={iconTone} aria-hidden="true">
              {iconNode}
            </span>
          ) : null}
          <div className={styles.copy}>
            <h2 id={titleId} className={styles.title}>
              {title}
            </h2>
            {description ? (
              <p id={descriptionId} className={styles.description}>
                {description}
              </p>
            ) : null}
          </div>
          <Button
            className={styles.close}
            variant="ghost"
            iconOnly
            aria-label="Close"
            onClick={requestClose}
            icon={<ExIcon name="close" />}
          />
        </header>
        {children ? <div className={styles.body}>{children}</div> : null}
        {footer ? <div className={styles.footer}>{footer}</div> : null}
      </motion.div>
    </dialog>
  );
}
