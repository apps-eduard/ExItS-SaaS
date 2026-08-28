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
          className="fixed inset-0 z-[1000] flex items-start justify-center overflow-auto bg-black/40 p-4"
          data-testid="client-error-overlay"
        >
          <ClientErrorPanel
            input={report}
            onReload={() => window.location.reload()}
            onDismiss={() => setReport(null)}
          />
        </div>
      ) : null}
    </>
  );
}
