import { buildOperationalErrorReport } from "@/diagnostics/client-error-report";
import type { NormalizePosErrorInput } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";

/** Build a redacted, copy-paste diagnostic report from any thrown/rejected error. */
export function buildErrorDiagnostic(
  error: unknown,
  partial?: Omit<NormalizePosErrorInput, "error">,
): PosErrorReportInput {
  return buildOperationalErrorReport({
    source: partial?.source ?? "network",
    pathname:
      partial?.pathname ??
      (typeof window !== "undefined" ? window.location.pathname : undefined),
    ...partial,
    error,
  });
}
