import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

export type ToastTone = "success" | "error";

export type ToastAction = {
  label: string;
  href: string;
};

export type ToastPayload = {
  title: string;
  description?: string;
  tone?: ToastTone;
  action?: ToastAction;
};

type ToastItem = {
  id: string;
  title: string;
  description?: string;
  tone: ToastTone;
  action?: ToastAction;
};

type ToastContextValue = {
  showToast: {
    (message: string, tone?: ToastTone): void;
    (payload: ToastPayload): void;
  };
};

const ToastContext = createContext<ToastContextValue | null>(null);

const AUTO_DISMISS_MS = 4200;

function isToastPayload(value: string | ToastPayload): value is ToastPayload {
  return typeof value === "object" && value !== null && "title" in value;
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const showToast = useCallback((messageOrPayload: string | ToastPayload, tone: ToastTone = "success") => {
    const id =
      typeof crypto !== "undefined" && "randomUUID" in crypto
        ? crypto.randomUUID()
        : `toast-${Date.now()}-${Math.random().toString(16).slice(2)}`;

    const item: ToastItem = isToastPayload(messageOrPayload)
      ? {
          id,
          title: messageOrPayload.title,
          description: messageOrPayload.description,
          tone: messageOrPayload.tone ?? "success",
          action: messageOrPayload.action,
        }
      : {
          id,
          title: messageOrPayload,
          tone,
        };

    setToasts((current) => [...current, item]);
    window.setTimeout(() => {
      setToasts((current) => current.filter((toast) => toast.id !== id));
    }, AUTO_DISMISS_MS);
  }, []);

  const value = useMemo(() => ({ showToast }), [showToast]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="exits-toast-region" aria-live="polite" aria-relevant="additions" data-testid="exits-toast-region">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={cn(
              "exits-toast",
              toast.tone === "success" ? "exits-toast--success" : "exits-toast--error",
              toast.action && "exits-toast--interactive",
            )}
            role="status"
            data-testid="exits-toast"
            data-tone={toast.tone}
          >
            <div className="exits-toast__title">{toast.title}</div>
            {toast.description ? (
              <div className="exits-toast__description">{toast.description}</div>
            ) : null}
            {toast.action ? (
              <Link
                to={toast.action.href}
                className="exits-toast__action"
                data-testid="exits-toast-action"
              >
                {toast.action.label}
              </Link>
            ) : null}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error("useToast must be used within ToastProvider");
  }
  return ctx;
}
