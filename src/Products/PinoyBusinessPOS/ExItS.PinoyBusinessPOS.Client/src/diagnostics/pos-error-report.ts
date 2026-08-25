import { readPosApplicationName, readPosBuildLabel } from "@/diagnostics/pos-build-info";
import { redactDiagnosticText, safeDiagnosticLocation } from "@/diagnostics/diagnostic-redaction";

export type PosErrorSource =
  | "react-error-boundary"
  | "window-error"
  | "unhandled-rejection"
  | "api"
  | "workspace"
  | "network"
  | "session";

export type PosErrorReportInput = {
  source: PosErrorSource;
  occurredAt?: string;
  screen?: string;
  pathname?: string;
  url?: string;
  operation?: string;
  friendlyMessage?: string;
  httpMethod?: string;
  path?: string;
  status?: number;
  errorCode?: string;
  traceId?: string;
  correlationId?: string;
  accountClass?: string;
  organizationPublicId?: string;
  organizationName?: string;
  branchPublicId?: string;
  branchName?: string;
  posBuild?: string;
  platformRuntime?: string;
  online?: boolean;
  error?: unknown;
  componentStack?: string | null;
  mode?: string;
};

function displayValue(value: string | number | undefined | null): string {
  if (value === undefined || value === null) {
    return "Not available";
  }
  if (typeof value === "string" && value.trim().length === 0) {
    return "Not available";
  }
  return String(value);
}

function displayOnline(value: boolean | undefined): string {
  if (value === true) {
    return "online";
  }
  if (value === false) {
    return "offline";
  }
  return "Not available";
}

export function formatPosErrorReport(input: PosErrorReportInput): string {
  const occurredAt = input.occurredAt ?? new Date().toISOString();
  const location = safeDiagnosticLocation(
    typeof window !== "undefined" ? window.location.href : null,
    input.pathname,
  );
  const screen = input.screen ?? location.pathname;
  const componentStack = input.componentStack
    ? redactDiagnosticText(input.componentStack.trim())
    : null;
  const mode =
    input.mode ?? (typeof import.meta !== "undefined" ? import.meta.env.MODE : "(unknown)");

  const lines = [
    "ExItS POS Error Report",
    "",
    `App: ${readPosApplicationName()}`,
    `Time: ${occurredAt}`,
    `Screen: ${displayValue(screen)}`,
    `Operation: ${displayValue(input.operation)}`,
    `HTTP Method: ${displayValue(input.httpMethod)}`,
    `Path: ${displayValue(input.path)}`,
    `Status: ${displayValue(input.status)}`,
    `ErrorCode: ${displayValue(input.errorCode)}`,
    `TraceId: ${displayValue(input.traceId ?? input.correlationId)}`,
    `Friendly message: ${displayValue(input.friendlyMessage)}`,
    `Account class: ${displayValue(input.accountClass)}`,
    `Organization: ${displayValue(input.organizationName ?? input.organizationPublicId)}`,
    `Branch: ${displayValue(input.branchName ?? input.branchPublicId)}`,
    `POS Build: ${displayValue(input.posBuild ?? readPosBuildLabel())}`,
    `Platform Runtime: ${displayValue(input.platformRuntime)}`,
    `Online/Offline: ${displayOnline(input.online ?? (typeof navigator !== "undefined" ? navigator.onLine : undefined))}`,
    `Source: ${input.source}`,
    `Build mode: ${mode}`,
  ];

  if (input.error instanceof Error) {
    lines.push(
      "",
      "Error name:",
      input.error.name || "Error",
      "",
      "Error message:",
      redactDiagnosticText(input.error.message || "(no message)"),
    );
    if (input.error.stack) {
      lines.push("", "Stack:", redactDiagnosticText(input.error.stack));
    }
  }

  if (componentStack) {
    lines.push("", "React component stack:", componentStack);
  }

  lines.push("", "Safe to paste into Cursor: YES", "");
  return lines.join("\n");
}
