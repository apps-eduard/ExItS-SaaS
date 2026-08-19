import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import { presentText } from "@/lib/diagnostics/diagnostic-redaction";

const FIELD_ORDER: ReadonlyArray<{
  label: string;
  read: (record: DiagnosticRecord) => string | undefined;
}> = [
  { label: "Application", read: (record) => presentText(record.application) },
  { label: "Route", read: (record) => presentText(record.route) },
  { label: "Operation", read: (record) => presentText(record.operation) },
  { label: "Error Reference", read: (record) => presentText(record.errorReference) },
  { label: "Error Type", read: (record) => presentText(record.errorType) },
  { label: "Category", read: (record) => presentText(record.category) },
  {
    label: "HTTP Status",
    read: (record) =>
      typeof record.httpStatus === "number" ? String(record.httpStatus) : undefined,
  },
  { label: "Error Code", read: (record) => presentText(record.errorCode) },
  { label: "Request Correlation ID", read: (record) => presentText(record.requestCorrelationId) },
  { label: "Server Trace ID", read: (record) => presentText(record.serverTraceId) },
  { label: "Timestamp", read: (record) => presentText(record.timestamp) },
  { label: "Locale", read: (record) => presentText(record.locale) },
  { label: "Theme", read: (record) => presentText(record.theme) },
  { label: "Density", read: (record) => presentText(record.density) },
  { label: "Browser/Platform", read: (record) => presentText(record.browserPlatform) },
  { label: "Message", read: (record) => presentText(record.message) },
  { label: "Component Stack", read: (record) => presentText(record.componentStack) },
];

export function buildDiagnosticReport(record: DiagnosticRecord): string {
  const sections = FIELD_ORDER.flatMap(({ label, read }) => {
    const value = read(record);
    return value ? [`${label}:`, value, ""] : [];
  });

  return [
    "EXITS ERROR DIAGNOSTICS",
    "",
    ...sections,
    "SECURITY:",
    "Sensitive credentials and protected request/response payloads excluded.",
  ].join("\n");
}
