import { useEffect, useState, type ReactNode } from "react";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";
import { normalizeReactClientError } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";

/**
 * Captures window.onerror and unhandledrejection, shows a copyable diagnostic overlay.
 * Does not replace the React tree (dismissible). React render crashes use GlobalErrorBoundary.
 */
export function GlobalRuntimeErrorHost({ children }: { children: ReactNode }) {
  const [report, setReport] = useState<PosErrorReportInput | null>(null);

  useEffect(() => {
    function capture(source: PosErrorReportInput["source"], error: unknown) {
      setReport(
        normalizeReactClientError({
          source,
          error,
        }),
      );
      console.error(`[ExItS] ${source}`, error);
    }

    function onWindowError(event: ErrorEvent) {
      if (!event.error && !event.message) {
        return;
      }
      capture("window-error", event.error ?? event.message);
    }

    function onUnhandledRejection(event: PromiseRejectionEvent) {
      capture("unhandled-rejection", event.reason);
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
