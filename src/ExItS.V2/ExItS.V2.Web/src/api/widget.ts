import { PlatformApiError } from "./platform-http";
import { PosApiError } from "./pos-http";

export type WidgetStatus =
  | "loading"
  | "ready"
  | "empty"
  | "error"
  | "unauthorized"
  | "forbidden"
  | "unavailable";

export type HomeWidget<T> = {
  status: WidgetStatus;
  data?: T;
  message?: string;
};

export function widgetError(error: unknown): HomeWidget<never> {
  if (error instanceof PlatformApiError || error instanceof PosApiError) {
    if (error.status === 401) {
      return { status: "unauthorized", message: error.message };
    }
    if (error.status === 403) {
      return { status: "forbidden", message: error.message };
    }
    return { status: "error", message: error.message };
  }
  return {
    status: "error",
    message: error instanceof Error ? error.message : "Request failed.",
  };
}

export function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null
    ? (value as Record<string, unknown>)
    : {};
}

export function num(
  record: Record<string, unknown>,
  camel: string,
  pascal: string,
): number {
  const value = record[camel] ?? record[pascal];
  return typeof value === "number" ? value : Number(value ?? 0) || 0;
}

export function str(
  record: Record<string, unknown>,
  camel: string,
  pascal: string,
): string {
  const value = record[camel] ?? record[pascal];
  return typeof value === "string" ? value : "";
}

export function nullableStr(
  record: Record<string, unknown>,
  camel: string,
  pascal: string,
): string | null {
  const value = record[camel] ?? record[pascal];
  return typeof value === "string" && value.trim() ? value : null;
}

export function bool(record: Record<string, unknown>, camel: string, pascal: string): boolean {
  return record[camel] === true || record[pascal] === true;
}
