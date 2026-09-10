import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type MouseEvent,
  type ReactNode,
} from "react";
import { X } from "lucide-react";
import { getToastNavigate } from "@/components/exits/toast-navigation";
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

/**
 * Toast UI lives in ToastProvider, which wraps RouterProvider in AppProviders.
 * react-router `Link` requires Router context and crashes there — use a plain
 * anchor and SPA-navigate via ToastNavigateBridge when the router is mounted.
 */
function ToastActionLink({ href, label }: { href: string; label: string }) {
  function onClick(event: MouseEvent<HTMLAnchorElement>) {
    if (
      event.defaultPrevented ||
      event.button !== 0 ||
      event.metaKey ||
      event.altKey ||
      event.ctrlKey ||
      event.shiftKey
    ) {
      return;
    }
    const navigate = getToastNavigate();
    if (!navigate) {
      return;
    }
    event.preventDefault();
    void navigate(href);
  }

  return (
    <a
      href={href}
      className="exits-toast__action"
      data-testid="exits-toast-action"
      onClick={onClick}
    >
      {label}
    </a>
  );
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const dismissToast = useCallback((id: string) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

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
            )}
            role="status"
            data-testid="exits-toast"
            data-tone={toast.tone}
          >
            <div className="exits-toast__body">
              <div className="exits-toast__title">{toast.title}</div>
              {toast.description ? (
                <div className="exits-toast__description">{toast.description}</div>
              ) : null}
              {toast.action ? (
                <ToastActionLink href={toast.action.href} label={toast.action.label} />
              ) : null}
            </div>
            <button
              type="button"
              className="exits-toast__close"
              data-testid="exits-toast-close"
              aria-label="Close"
              onClick={() => dismissToast(toast.id)}
            >
              <X className="size-4" aria-hidden />
            </button>
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
