import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type MouseEvent,
  type ReactNode,
} from "react";
import { CircleAlert, CircleCheck, Info, TriangleAlert, X } from "lucide-react";
import { getToastNavigate } from "@/components/exits/toast-navigation";
import { cn } from "@/lib/cn";

export type ToastTone = "success" | "info" | "warning" | "error";

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

export type ExitsToastApi = {
  success: (title: string, description?: string) => void;
  info: (title: string, description?: string) => void;
  warning: (title: string, description?: string) => void;
  error: (title: string, description?: string) => void;
  show: {
    (message: string, tone?: ToastTone): void;
    (payload: ToastPayload): void;
  };
};

type ToastContextValue = {
  /** @deprecated Prefer `toast.success|info|warning|error` — kept for existing callers. */
  showToast: {
    (message: string, tone?: ToastTone): void;
    (payload: ToastPayload): void;
  };
  toast: ExitsToastApi;
};

const ToastContext = createContext<ToastContextValue | null>(null);

const AUTO_DISMISS_MS = 4200;
const WARNING_ACTION_DISMISS_MS = 10000;

const TOAST_ICONS = {
  success: CircleCheck,
  info: Info,
  warning: TriangleAlert,
  error: CircleAlert,
} as const;

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
    const dismissMs =
      item.tone === "warning" && item.action ? WARNING_ACTION_DISMISS_MS : AUTO_DISMISS_MS;
    window.setTimeout(() => {
      setToasts((current) => current.filter((toast) => toast.id !== id));
    }, dismissMs);
  }, []);

  const toastApi = useMemo<ExitsToastApi>(
    () => ({
      success: (title, description) => showToast({ title, description, tone: "success" }),
      info: (title, description) => showToast({ title, description, tone: "info" }),
      warning: (title, description) => showToast({ title, description, tone: "warning" }),
      error: (title, description) => showToast({ title, description, tone: "error" }),
      show: showToast,
    }),
    [showToast],
  );

  const value = useMemo(() => ({ showToast, toast: toastApi }), [showToast, toastApi]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="exits-toast-region" aria-live="polite" aria-relevant="additions" data-testid="exits-toast-region">
        {toasts.map((toast) => {
          const Icon = TOAST_ICONS[toast.tone];
          return (
            <div
              key={toast.id}
              className={cn(
                "exits-toast",
                toast.tone === "success" && "exits-toast--success",
                toast.tone === "info" && "exits-toast--info",
                toast.tone === "error" && "exits-toast--error",
                toast.tone === "warning" && "exits-toast--warning",
              )}
              role="status"
              data-testid="exits-toast"
              data-tone={toast.tone}
            >
              <span className="exits-toast__icon" aria-hidden>
                <Icon className="size-4" />
              </span>
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
          );
        })}
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

/** Canonical toast helpers — same as `useToast().toast`. */
export function useExitsToast(): ExitsToastApi {
  return useToast().toast;
}
