import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import { presentText } from "@/lib/diagnostics/diagnostic-redaction";

const FIELD_ORDER: ReadonlyArray<{
  label: string;
  read: (record: DiagnosticRecord) => string | undefined;
}> = [
  { label: "Application", read: (record) => presentText(record.application) },
  { label: "App version", read: (record) => presentText(record.appVersion) },
  { label: "Route", read: (record) => presentText(record.route) },
  { label: "Error reference", read: (record) => presentText(record.errorReference) },
  { label: "Category", read: (record) => presentText(record.category) },
  {
    label: "HTTP status",
    read: (record) =>
      typeof record.httpStatus === "number" ? String(record.httpStatus) : undefined,
  },
  { label: "Error code", read: (record) => presentText(record.errorCode) },
  { label: "Correlation ID", read: (record) => presentText(record.requestCorrelationId) },
  { label: "Locale", read: (record) => presentText(record.locale) },
  { label: "Theme", read: (record) => presentText(record.theme) },
  { label: "Browser/platform", read: (record) => presentText(record.browserPlatform) },
  { label: "Timestamp", read: (record) => presentText(record.timestamp) },
  { label: "Message", read: (record) => presentText(record.message) },
];

export function buildDiagnosticReport(record: DiagnosticRecord): string {
  const sections = FIELD_ORDER.flatMap(({ label, read }) => {
    const value = read(record);
    return value ? [`${label}:`, value, ""] : [];
  });

  return [
    "EXITS MOBILE CLIENT DIAGNOSTICS",
    "",
    ...sections,
    "SECURITY:",
    "Allowlist only. Arbitrary error text, API problem title/detail, payloads, PII, secrets, and stack dumps excluded.",
  ].join("\n");
}
