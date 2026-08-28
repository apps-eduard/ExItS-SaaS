import { useEffect, useState, type ReactNode } from "react";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";
import {
  reportGlobalRuntimeError,
  subscribeGlobalClientErrors,
} from "@/diagnostics/global-error-reporter";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";

/**
 * Captures window.onerror, unhandledrejection, and reported client errors;
 * shows a copyable diagnostic overlay. React render crashes use GlobalErrorBoundary.
 */
export function GlobalRuntimeErrorHost({ children }: { children: ReactNode }) {
  const [report, setReport] = useState<PosErrorReportInput | null>(null);

  useEffect(() => subscribeGlobalClientErrors((next) => setReport(next)), []);

  useEffect(() => {
    function onWindowError(event: ErrorEvent) {
      if (!event.error && !event.message) {
        return;
      }
      reportGlobalRuntimeError({
        source: "window-error",
        error: event.error ?? event.message,
      });
    }

    function onUnhandledRejection(event: PromiseRejectionEvent) {
      reportGlobalRuntimeError({
        source: "unhandled-rejection",
        error: event.reason,
      });
    }

    window.addEventListener("error", onWindowError);
    window.addEventListener("unhandledrejection", onUnhandledRejection);
    return () => {
      window.removeEventListener("error", onWindowError);
      window.removeEventListener("unhandledrejection", onUnhandledRejection);
    };
  }, []);

  return (
    <>
      {children}
      {report ? (
        <div
          className="pointer-events-none fixed inset-0 z-[1000] flex items-start justify-center overflow-auto p-4"
          data-testid="client-error-overlay"
        >
          {/* Backdrop is non-blocking so bottom nav / shell stay clickable (freeze audit). */}
          <div className="pointer-events-none absolute inset-0 bg-black/40" aria-hidden />
          <div className="pointer-events-auto relative z-[1] w-full max-w-2xl">
            <ClientErrorPanel
              input={report}
              onReload={() => window.location.reload()}
              onDismiss={() => setReport(null)}
            />
          </div>
        </div>
      ) : null}
    </>
  );
}
