import { useMemo } from "react";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";
import { buildErrorDiagnostic } from "@/diagnostics/build-error-diagnostic";
import { CopyErrorDetailsButton } from "@/diagnostics/CopyErrorDetailsButton";
import type { PosErrorSource } from "@/diagnostics/pos-error-report";

export function ErrorState({
  title,
  detail,
  diagnostic,
  error,
  operation,
  source = "network",
}: {
  title: string;
  detail: string;
  diagnostic?: PosErrorReportInput;
  /** When set, builds a copy-paste diagnostic report automatically. */
  error?: unknown;
  operation?: string;
  source?: PosErrorSource;
}) {
  const resolvedDiagnostic = useMemo(() => {
    if (diagnostic) {
      return diagnostic;
    }
    if (error === undefined) {
      return undefined;
    }
    return buildErrorDiagnostic(error, {
      source,
      operation,
      friendlyMessage: detail,
    });
  }, [detail, diagnostic, error, operation, source]);

  return (
    <div
      role="alert"
      className="flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-destructive px-4 py-4"
      data-testid="error-state"
    >
      <div className="flex flex-col gap-1">
        <p className="exits-type-section-title m-0">{title}</p>
        <p className="exits-type-muted m-0">{detail}</p>
      </div>
      {resolvedDiagnostic ? <CopyErrorDetailsButton report={resolvedDiagnostic} /> : null}
    </div>
  );
}
