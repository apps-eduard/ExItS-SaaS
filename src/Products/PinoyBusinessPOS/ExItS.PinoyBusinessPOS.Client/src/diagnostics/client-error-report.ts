export type ClientErrorSource = "react-error-boundary" | "window-error" | "unhandled-rejection";

export type ClientErrorReportInput = {
  source: ClientErrorSource;
  error: unknown;
  componentStack?: string | null;
  url?: string;
  pathname?: string;
  mode?: string;
  occurredAt?: string;
};

function asError(error: unknown): { name: string; message: string; stack: string | null } {
  if (error instanceof Error) {
    return {
      name: error.name || "Error",
      message: error.message || "(no message)",
      stack: error.stack ?? null,
    };
  }
  if (typeof error === "string") {
    return { name: "Error", message: error, stack: null };
  }
  try {
    return { name: "Error", message: JSON.stringify(error), stack: null };
  } catch {
    return { name: "Error", message: String(error), stack: null };
  }
}

/**
 * Structured report for pasting into Cursor chat so the agent can locate and fix the failure.
 */
export function formatClientErrorReport(input: ClientErrorReportInput): string {
  const occurredAt = input.occurredAt ?? new Date().toISOString();
  const url = input.url ?? (typeof window !== "undefined" ? window.location.href : "(unknown)");
  const pathname =
    input.pathname ?? (typeof window !== "undefined" ? window.location.pathname : "(unknown)");
  const mode =
    input.mode ?? (typeof import.meta !== "undefined" ? import.meta.env.MODE : "(unknown)");
  const { name, message, stack } = asError(input.error);
  const componentStack = input.componentStack?.trim() || null;

  const lines = [
    "## ExItS POS React — client error report",
    "",
    "Paste this whole block into Cursor chat to request a fix.",
    "",
    `Source: ${input.source}`,
    `When (UTC): ${occurredAt}`,
    `URL: ${url}`,
    `Pathname: ${pathname}`,
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
    "",
  );

  return lines.join("\n");
}
