import {
  formatPosErrorReport,
  type PosErrorReportInput,
} from "@/diagnostics/pos-error-report";
import {
  normalizePosError,
  normalizeReactClientError,
  type NormalizePosErrorInput,
} from "@/diagnostics/normalize-pos-error";

export type ClientErrorReportInput = PosErrorReportInput;

/** @deprecated Prefer PosErrorReportInput */
export type { PosErrorReportInput, PosErrorSource } from "@/diagnostics/pos-error-report";

export function formatClientErrorReport(input: ClientErrorReportInput): string {
  return formatPosErrorReport(input);
}

export function buildClientErrorReportFromReact(input: {
  source: PosErrorReportInput["source"];
  error: unknown;
  componentStack?: string | null;
  url?: string;
  pathname?: string;
  mode?: string;
  occurredAt?: string;
}): PosErrorReportInput {
  return normalizeReactClientError({
    source: input.source,
    error: input.error,
    componentStack: input.componentStack,
    pathname: input.pathname,
  });
}

export function buildOperationalErrorReport(input: NormalizePosErrorInput): PosErrorReportInput {
  return normalizePosError(input);
}
