import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ExIcon } from "../../icons/ExIcon";
import type { IconName } from "../../icons/registry";
import { Button } from "../button/Button";
import { hideOverlay, showOverlay } from "../overlay/overlay";
import styles from "./Toast.module.scss";

export const TOAST_TONES = ["info", "success", "warning", "danger"] as const;

export type ToastTone = (typeof TOAST_TONES)[number];

export type ToastInput = {
  title: string;
  description?: string;
  tone?: ToastTone;
};

type ToastRecord = ToastInput & { id: number; tone: ToastTone };

type ToastContextValue = {
  push: (toast: ToastInput) => void;
};

const ToastContext = createContext<ToastContextValue | null>(null);
const ICONS: Record<ToastTone, IconName> = {
  info: "info",
  success: "check",
  warning: "warning",
  danger: "error",
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastRecord[]>([]);
  const regionRef = useRef<HTMLDivElement>(null);
  const timers = useRef(new Map<number, number>());
  const nextId = useRef(1);

  const dismiss = useCallback((id: number) => {
    const timer = timers.current.get(id);
    if (timer) window.clearTimeout(timer);
    timers.current.delete(id);
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const schedule = useCallback(
    (id: number) => {
      const existing = timers.current.get(id);
      if (existing) window.clearTimeout(existing);
      timers.current.set(
        id,
        window.setTimeout(() => dismiss(id), 4000),
      );
    },
    [dismiss],
  );

  const push = useCallback(
    (toast: ToastInput) => {
      const id = nextId.current;
      nextId.current += 1;
      setToasts((current) => [...current, { ...toast, id, tone: toast.tone ?? "info" }].slice(-4));
      schedule(id);
    },
    [schedule],
  );

  useEffect(() => {
    const region = regionRef.current;
    if (!region) return;
    if (toasts.length > 0) showOverlay(region);
    else hideOverlay(region);
  }, [toasts.length]);

  const value = useMemo(() => ({ push }), [push]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div ref={regionRef} className={styles.region} popover="manual" aria-label="Notifications">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={`${styles.toast} ${styles[toast.tone]}`}
            role={toast.tone === "danger" ? "alert" : "status"}
            onMouseEnter={() => {
              const timer = timers.current.get(toast.id);
              if (timer) window.clearTimeout(timer);
            }}
            onMouseLeave={() => schedule(toast.id)}
          >
            <span className={styles.icon} aria-hidden="true">
              <ExIcon name={ICONS[toast.tone]} />
            </span>
            <div className={styles.copy}>
              <strong className="type-label">{toast.title}</strong>
              {toast.description ? <span className="type-caption">{toast.description}</span> : null}
            </div>
            <Button variant="ghost" iconOnly aria-label="Dismiss notification" onClick={() => dismiss(toast.id)} icon={<ExIcon name="close" size={18} />} />
          </div>
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
