import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { ErrorState } from "@/components/exits/ErrorState";
import { usePreferences } from "@/hooks/use-preferences";
import { isAuthenticationLostFailure } from "@/api/auth/session-expiry";
import { PlatformApiError } from "@/api/platform-http";
import { isAbortError } from "@/lib/diagnostics/diagnostic-redaction";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticCategory, DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type ReportOptions = {
  operation?: string;
  category?: DiagnosticCategory;
  componentStack?: string;
  retry?: () => void;
};

type ActiveNotice = {
  diagnostic: DiagnosticRecord;
  retry?: () => void;
};

type DiagnosticsContextValue = {
  report: (error: unknown, options?: ReportOptions) => DiagnosticRecord | null;
  clear: () => void;
  active: DiagnosticRecord | null;
};

const DiagnosticsContext = createContext<DiagnosticsContextValue | null>(null);
const handledErrors = new WeakSet<object>();

function markHandled(error: unknown): void {
  if (error && (typeof error === "object" || typeof error === "function")) {
    handledErrors.add(error);
  }
}

function wasHandled(error: unknown): boolean {
  return Boolean(
    error && (typeof error === "object" || typeof error === "function") && handledErrors.has(error),
  );
}

export function DiagnosticsProvider({ children }: { children: ReactNode }) {
  const { language, theme, density } = usePreferences();
  const [notice, setNotice] = useState<ActiveNotice | null>(null);

  const environment = useMemo(
    () => ({
      locale: language,
      theme,
      density,
    }),
    [language, theme, density],
  );

  const report = useCallback(
    (error: unknown, options?: ReportOptions): DiagnosticRecord | null => {
      if (isAbortError(error) || wasHandled(error)) {
        return null;
      }
      if (
        error instanceof PlatformApiError &&
        isAuthenticationLostFailure(error.status, error.problem.errorCode)
      ) {
        return null;
      }
      markHandled(error);
      const diagnostic = normalizeDiagnosticError({
        error,
        operation: options?.operation,
        category: options?.category,
        componentStack: options?.componentStack,
        environment,
      });
      setNotice({ diagnostic, retry: options?.retry });
      if (import.meta.env.DEV) {
        console.error("Platform Admin Web diagnostic", diagnostic.errorReference, error);
      }
      return diagnostic;
    },
    [environment],
  );

  const clear = useCallback(() => {
    setNotice(null);
  }, []);

  useEffect(() => {
    function onWindowError(event: ErrorEvent) {
      if (isAbortError(event.error) || wasHandled(event.error)) {
        return;
      }
      report(event.error ?? event.message, { category: "UNEXPECTED_CLIENT_ERROR" });
    }

    function onUnhandledRejection(event: PromiseRejectionEvent) {
      if (isAbortError(event.reason) || wasHandled(event.reason)) {
        return;
      }
      report(event.reason, { category: "UNEXPECTED_CLIENT_ERROR" });
    }

    window.addEventListener("error", onWindowError);
    window.addEventListener("unhandledrejection", onUnhandledRejection);
    return () => {
      window.removeEventListener("error", onWindowError);
      window.removeEventListener("unhandledrejection", onUnhandledRejection);
    };
  }, [report]);

  const value = useMemo(
    () => ({ report, clear, active: notice?.diagnostic ?? null }),
    [report, clear, notice],
  );

  return (
    <DiagnosticsContext.Provider value={value}>
      {children}
      {notice ? (
        <div className="pointer-events-none fixed inset-x-3 bottom-3 z-[var(--exits-z-notice)] sm:inset-x-auto sm:right-3 sm:max-w-md">
          <div className="pointer-events-auto shadow-lg">
            <ErrorState
              diagnostic={notice.diagnostic}
              onRetry={
                notice.retry
                  ? () => {
                      const retry = notice.retry;
                      clear();
                      retry?.();
                    }
                  : undefined
              }
              onClose={clear}
            />
          </div>
        </div>
      ) : null}
    </DiagnosticsContext.Provider>
  );
}

export function useDiagnostics(): DiagnosticsContextValue {
  const context = useContext(DiagnosticsContext);
  if (!context) {
    throw new Error("useDiagnostics must be used within DiagnosticsProvider.");
  }
  return context;
}
