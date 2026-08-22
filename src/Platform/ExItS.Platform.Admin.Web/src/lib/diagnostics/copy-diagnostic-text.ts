import {
  formatDiagnosticForClipboard,
  buildDiagnosticReport,
} from "@/lib/diagnostics/build-diagnostic-report";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

export async function copyDiagnosticText(text: string): Promise<boolean> {
  if (typeof navigator === "undefined" || typeof navigator.clipboard?.writeText !== "function") {
    return false;
  }
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}

export async function copyDiagnosticReport(diagnostic: DiagnosticRecord): Promise<boolean> {
  return copyDiagnosticText(formatDiagnosticForClipboard(diagnostic));
}

export { formatDiagnosticForClipboard, buildDiagnosticReport };
