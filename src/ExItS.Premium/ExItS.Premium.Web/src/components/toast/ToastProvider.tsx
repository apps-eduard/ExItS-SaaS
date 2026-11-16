import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ExIcon } from "../../icons/ExIcon";
import type { IconName } from "../../icons/registry";
import { Button } from "../button/Button";
import { hideOverlay, showOverlay } from "../overlay/overlay";
import styles from "./Toast.module.scss";

export const TOAST_TONES = ["info", "success", "warning", "danger"] as const;

export type ToastTone = (typeof TOAST_TONES)[number];

/** Where the toast stack anchors in the viewport. Default: bottom-end. */
export const TOAST_POSITIONS = [
  "top-start",
  "top-center",
  "top-end",
  "bottom-start",
  "bottom-center",
  "bottom-end",
] as const;

export type ToastPosition = (typeof TOAST_POSITIONS)[number];

export const TOAST_DEFAULT_POSITION: ToastPosition = "bottom-end";

/** Auto-dismiss length. The right-edge countdown uses this same value. */
export const TOAST_DISMISS_MS = 4000;

export type ToastInput = {
  title: string;
  description?: string;
  tone?: ToastTone;
};

type ToastRecord = ToastInput & { id: number; tone: ToastTone };

type ToastContextValue = {
  push: (toast: ToastInput) => void;
  position: ToastPosition;
  setPosition: (position: ToastPosition) => void;
};

const ToastContext = createContext<ToastContextValue | null>(null);
const ICONS: Record<ToastTone, IconName> = {
  info: "info",
  success: "check",
  warning: "warning",
  danger: "error",
};

function ToastCard({ toast, onDismiss }: { toast: ToastRecord; onDismiss: (id: number) => void }) {
  const rootRef = useRef<HTMLDivElement>(null);
  const hold = useRef({ hover: false, focus: false });
  const clock = useRef<{ pause: () => void; resume: () => void } | null>(null);

  useEffect(() => {
    const root = rootRef.current;
    let timer = 0;
    let frame = 0;
    let remaining = TOAST_DISMISS_MS;
    let started = 0;
    let paused = false;
    let running = false;

    const clearTimer = () => window.clearTimeout(timer);

    const arm = (ms: number) => {
      clearTimer();
      remaining = ms;
      started = performance.now();
      running = true;
      timer = window.setTimeout(() => onDismiss(toast.id), ms);
    };

    const pause = () => {
      if (paused) return;
      if (running) {
        remaining = Math.max(0, remaining - (performance.now() - started));
        clearTimer();
        running = false;
      }
      paused = true;
      root?.setAttribute("data-paused", "true");
    };

    const resume = () => {
      if (!paused) return;
      paused = false;
      root?.removeAttribute("data-paused");
      arm(remaining);
    };

    clock.current = { pause, resume };
    frame = window.requestAnimationFrame(() => {
      root?.setAttribute("data-countdown", "run");
      if (!paused) arm(remaining);
    });

    return () => {
      window.cancelAnimationFrame(frame);
      clearTimer();
      clock.current = null;
    };
  }, [onDismiss, toast.id]);

  function setHold(kind: "hover" | "focus", active: boolean) {
    hold.current[kind] = active;
    if (hold.current.hover || hold.current.focus) clock.current?.pause();
    else clock.current?.resume();
  }

  return (
    <div
      ref={rootRef}
      className={`${styles.toast} ${styles[toast.tone]}`}
      role={toast.tone === "danger" ? "alert" : "status"}
      onMouseEnter={() => setHold("hover", true)}
      onMouseLeave={() => setHold("hover", false)}
      onFocusCapture={() => setHold("focus", true)}
      onBlurCapture={(event) => {
        const next = event.relatedTarget;
        if (next instanceof Node && event.currentTarget.contains(next)) return;
        setHold("focus", false);
      }}
    >
      <span className={styles.icon} aria-hidden="true">
        <ExIcon name={ICONS[toast.tone]} size={18} />
      </span>
      <div className={styles.copy}>
        <strong className={styles.title}>{toast.title}</strong>
        {toast.description ? <span className={styles.description}>{toast.description}</span> : null}
      </div>
      <Button
        className={styles.dismiss}
        variant="ghost"
        iconOnly
        aria-label="Dismiss notification"
        onClick={() => onDismiss(toast.id)}
        icon={<ExIcon name="close" size={16} />}
      />
      <span className={styles.countdown} data-toast-countdown="" aria-hidden="true" />
    </div>
  );
}

export function ToastProvider({
  children,
  position: positionProp,
  defaultPosition = TOAST_DEFAULT_POSITION,
}: {
  children: ReactNode;
  /** Controlled stack position. */
  position?: ToastPosition;
  /** Uncontrolled initial position. */
  defaultPosition?: ToastPosition;
}) {
  const [toasts, setToasts] = useState<ToastRecord[]>([]);
  const [uncontrolledPosition, setUncontrolledPosition] = useState<ToastPosition>(defaultPosition);
  const position = positionProp ?? uncontrolledPosition;
  const regionRef = useRef<HTMLDivElement>(null);
  const nextId = useRef(1);

  const setPosition = useCallback(
    (next: ToastPosition) => {
      if (positionProp === undefined) setUncontrolledPosition(next);
    },
    [positionProp],
  );

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const push = useCallback((toast: ToastInput) => {
    const id = nextId.current;
    nextId.current += 1;
    setToasts((current) => [...current, { ...toast, id, tone: toast.tone ?? "info" }].slice(-4));
  }, []);

  useEffect(() => {
    const region = regionRef.current;
    if (!region) return;
    if (toasts.length > 0) {
      showOverlay(region);
      /* Fallback when showPopover no-ops — CSS needs data-open or :popover-open. */
      region.dataset.open = "true";
    } else {
      hideOverlay(region);
    }
  }, [toasts.length]);

  const value = useMemo(() => ({ push, position, setPosition }), [push, position, setPosition]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        ref={regionRef}
        className={styles.region}
        data-position={position}
        popover="manual"
        aria-label="Notifications"
      >
        {toasts.map((toast) => (
          <ToastCard key={toast.id} toast={toast} onDismiss={dismiss} />
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) throw new Error("useToast must be used within ToastProvider");
  return context;
}
