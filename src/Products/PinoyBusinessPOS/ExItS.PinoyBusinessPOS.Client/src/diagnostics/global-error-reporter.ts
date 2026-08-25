import {
  buildOperationalErrorReport,
  buildClientErrorReportFromReact,
} from "@/diagnostics/client-error-report";
import type { NormalizePosErrorInput } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";

type GlobalErrorListener = (report: PosErrorReportInput) => void;

const listeners = new Set<GlobalErrorListener>();
let lastReportKey = "";
let lastReportAt = 0;

const DEDUPE_MS = 2500;

function reportKey(report: PosErrorReportInput): string {
  const err = report.error;
  const message =
    err instanceof Error ? err.message : typeof err === "string" ? err : report.friendlyMessage ?? "";
  return `${report.source}|${report.pathname ?? ""}|${report.operation ?? ""}|${message}`;
}

function shouldEmit(report: PosErrorReportInput): boolean {
  const key = reportKey(report);
  const now = Date.now();
  if (key === lastReportKey && now - lastReportAt < DEDUPE_MS) {
    return false;
  }
  lastReportKey = key;
  lastReportAt = now;
  return true;
}

function emit(report: PosErrorReportInput): void {
  if (!shouldEmit(report)) {
    return;
  }
  for (const listener of listeners) {
    listener(report);
  }
}

/** Subscribe to global client error reports (overlay host). Returns unsubscribe. */
export function subscribeGlobalClientErrors(listener: GlobalErrorListener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function toReport(input: NormalizePosErrorInput | PosErrorReportInput): PosErrorReportInput {
  if ("occurredAt" in input && input.occurredAt) {
    return input as PosErrorReportInput;
  }
  return buildOperationalErrorReport(input as NormalizePosErrorInput);
}

/** Report a runtime/API/network error to the global copyable overlay (deduped). */
export function reportGlobalClientError(
  input: NormalizePosErrorInput | PosErrorReportInput,
): void {
  const report = toReport(input);
  emit(report);
  console.error(`[ExItS] ${report.source}`, report.error ?? report.friendlyMessage);
}

/** Report uncaught window / React runtime failures. */
export function reportGlobalRuntimeError(input: {
  source: PosErrorReportInput["source"];
  error: unknown;
  componentStack?: string | null;
  friendlyMessage?: string;
}): void {
  const report = buildClientErrorReportFromReact(input);
  emit(report);
  console.error(`[ExItS] ${input.source}`, input.error);
}

export function isAbortError(error: unknown): boolean {
  if (!error || typeof error !== "object") {
    return false;
  }
  const name = "name" in error ? String(error.name) : "";
  return name === "AbortError";
}
