import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import { presentText } from "@/lib/diagnostics/diagnostic-redaction";

function displayValue(value: string | undefined): string {
  return presentText(value) ?? "Not available";
}

function displayBoolean(value: boolean | undefined): string {
  if (value === true) {
    return "Yes";
  }
  if (value === false) {
    return "No";
  }
  return "Not available";
}

function displayOnline(value: boolean | undefined): string {
  if (value === true) {
    return "Yes";
  }
  if (value === false) {
    return "Offline";
  }
  return "Not available";
}

export function formatDiagnosticForClipboard(record: DiagnosticRecord): string {
  const lines = [
    "EXITS PLATFORM ERROR REPORT",
    "",
    `Error Reference: ${displayValue(record.errorReference)}`,
    `Time: ${displayValue(record.timestampUtc)}`,
    `Application: ${displayValue(record.application)}`,
    `Build: ${displayValue(record.buildSha)}`,
    `Environment: ${displayValue(record.environment)}`,
  ];

  if (record.localValidationEnabled) {
    lines.push(
      `Frontend Mode: ${displayValue(record.frontendMode)}`,
      `API Mode: ${displayValue(record.apiMode)}`,
      "Local Validation: Enabled",
    );
  }

  lines.push(
    "",
    "Page:",
    displayValue(record.pagePath),
    "",
    "Operation:",
    displayValue(record.operation),
    "",
    "Category:",
    displayValue(record.category),
    "",
    "Message:",
    displayValue(record.userMessage),
    "",
    "HTTP Method:",
    displayValue(record.httpMethod),
    "",
    "API Path:",
    displayValue(record.apiPath),
    "",
    "HTTP Status:",
    displayValue(
      record.httpStatusLabel ??
        (typeof record.httpStatus === "number" ? String(record.httpStatus) : undefined),
    ),
    "",
    "Error Code:",
    displayValue(record.errorCode),
    "",
    "Trace ID:",
    displayValue(record.traceId),
    "",
    "Correlation ID:",
    displayValue(record.correlationId),
    "",
    "Browser Online:",
    displayOnline(record.networkOnline),
    "",
    "Retryable:",
    displayBoolean(record.retryable),
    "",
    "Safe to paste into Cursor:",
    "YES",
  );

  if (presentText(record.browserName) || presentText(record.browserVersion)) {
    lines.push(
      "",
      "Browser:",
      [record.browserName, record.browserVersion].filter(Boolean).join(" ") || "Not available",
    );
  }

  return lines.join("\n");
}

/** @deprecated Use formatDiagnosticForClipboard */
export function buildDiagnosticReport(record: DiagnosticRecord): string {
  return formatDiagnosticForClipboard(record);
}
