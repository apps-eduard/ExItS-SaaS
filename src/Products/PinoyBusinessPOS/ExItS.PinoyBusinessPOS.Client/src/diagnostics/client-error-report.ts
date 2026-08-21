import {
  redactDiagnosticText,
  safeDiagnosticError,
  safeDiagnosticLocation,
} from "@/diagnostics/diagnostic-redaction";

export type ClientErrorSource = "react-error-boundary" | "window-error" | "unhandled-rejection";

export type ClientErrorReportInput = {
  source: ClientErrorSource;
  error: unknown;
  componentStack?: string | null;
  /** May be a full href; search/hash are stripped before reporting. */
  url?: string;
  pathname?: string;
  mode?: string;
  occurredAt?: string;
};

/**
 * Structured report for pasting into Cursor chat so the agent can locate and fix the failure.
 * Intentionally omits query strings, fragments, tokens, and arbitrary object dumps.
 */
export function formatClientErrorReport(input: ClientErrorReportInput): string {
  const occurredAt = input.occurredAt ?? new Date().toISOString();
  const location = safeDiagnosticLocation(
    input.url ?? (typeof window !== "undefined" ? window.location.href : null),
    input.pathname,
  );
  const mode =
    input.mode ?? (typeof import.meta !== "undefined" ? import.meta.env.MODE : "(unknown)");
  const { name, message, stack } = safeDiagnosticError(input.error);
  const componentStack = input.componentStack
    ? redactDiagnosticText(input.componentStack.trim())
    : null;

  const lines = [
    "## ExItS POS React — client error report",
    "",
    "Paste this whole block into Cursor chat to request a fix.",
    "",
    `Source: ${input.source}`,
    `When (UTC): ${occurredAt}`,
    `URL: ${location.url}`,
    `Pathname: ${location.pathname}`,
    `Build mode: ${mode}`,
    `Error name: ${name}`,
    `Error message: ${message}`,
    "",
    "### Stack",
    stack ?? "(no stack)",
  ];

  if (componentStack) {
    lines.push("", "### React component stack", componentStack);
  }

  lines.push(
    "",
    "### Fix hints for Cursor",
    `- Project: ExItS Pinoy Business POS React/PWA (worktree ExItS-SaaS-pos-react-client)`,
    `- Branch: feat/pos-react-client`,
    `- Start at pathname + stack frames under src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/src`,
    `- Prefer the top-most app frame (not node_modules) as the failure site`,
    `- Do not request tokens, offline payloads, or customer/Personal record dumps`,
    "",
  );

  return lines.join("\n");
}
