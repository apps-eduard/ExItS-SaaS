import {
  createCorrelationId,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform-http";
import { PlatformAntiforgeryDefaults } from "@/api/platform-antiforgery";
import { maybeNotifyAuthenticationLost } from "@/api/auth/session-expiry";
import { platformRequest, type PlatformRequestOptions } from "@/api/platform-http";

function notifyAuthLostFromMultipart(
  status: number,
  errorCode: string | undefined,
  path: string,
): void {
  maybeNotifyAuthenticationLost({ status, errorCode, path });
}

const inFlightMutations = new Map<string, Promise<unknown>>();

function mutationDedupeKey(method: string, path: string, body: unknown): string {
  return `${method}:${path}:${body === undefined ? "" : JSON.stringify(body)}`;
}

export async function globalCatalogMutationRequest<T>(
  baseUrl: string,
  options: Omit<PlatformRequestOptions, "method"> & {
    method: "POST" | "PUT" | "PATCH" | "DELETE";
  },
): Promise<T> {
  const key = mutationDedupeKey(options.method, options.path, options.body);
  const existing = inFlightMutations.get(key);
  if (existing) {
    return existing as Promise<T>;
  }

  const pending = platformRequest<T>(baseUrl, options).finally(() => {
    inFlightMutations.delete(key);
  });
  inFlightMutations.set(key, pending);
  return pending;
}

export function resetGlobalCatalogMutationInFlight(): void {
  inFlightMutations.clear();
}

function parseProblem(payload: unknown): PlatformProblemDetails {
  if (typeof payload !== "object" || payload === null) {
    return {};
  }
  const record = payload as Record<string, unknown>;
  return {
    title: typeof record.title === "string" ? record.title : undefined,
    status: typeof record.status === "number" ? record.status : undefined,
    detail: typeof record.detail === "string" ? record.detail : undefined,
    errorCode:
      typeof record.errorCode === "string"
        ? record.errorCode
        : typeof record.ErrorCode === "string"
          ? record.ErrorCode
          : undefined,
    traceId:
      typeof record.traceId === "string"
        ? record.traceId
        : typeof record.TraceId === "string"
          ? record.TraceId
          : undefined,
  };
}

async function bootstrapAntiforgeryForMultipart(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<{ headerName: string; token: string }> {
  return platformRequest<{ headerName: string; token: string }>(baseUrl, {
    path: PlatformAntiforgeryDefaults.tokenPath,
    signal,
  });
}

export async function globalCatalogMultipartRequest<T>(
  baseUrl: string,
  options: {
    method: "PUT" | "POST";
    path: string;
    formData: FormData;
    signal?: AbortSignal;
  },
): Promise<T> {
  const requestCorrelationId = createCorrelationId();
  const bootstrap = await bootstrapAntiforgeryForMultipart(baseUrl, options.signal);
  const headers = new Headers({
    Accept: "application/json",
    "X-Correlation-Id": requestCorrelationId,
    [bootstrap.headerName || PlatformAntiforgeryDefaults.headerName]: bootstrap.token,
  });

  const response = await fetch(`${baseUrl}${options.path}`, {
    method: options.method,
    credentials: "include",
    headers,
    body: options.formData,
    signal: options.signal,
  });

  if (!response.ok) {
    let problem: PlatformProblemDetails = { status: response.status };
    try {
      problem = { ...problem, ...parseProblem(await response.json()) };
    } catch {
      // Non-JSON error bodies still surface as a status-only problem.
    }
    notifyAuthLostFromMultipart(response.status, problem.errorCode, options.path);
    throw new PlatformApiError(response.status, problem, {
      requestCorrelationId,
      method: options.method,
      path: options.path,
    });
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function globalCatalogImportUploadRequest<T>(
  baseUrl: string,
  options: {
    path: string;
    formData: FormData;
    idempotencyKey?: string;
    signal?: AbortSignal;
  },
): Promise<T> {
  const requestCorrelationId = createCorrelationId();
  const headers = new Headers({
    Accept: "application/json",
    "X-Correlation-Id": requestCorrelationId,
  });
  if (options.idempotencyKey) {
    headers.set("Idempotency-Key", options.idempotencyKey);
  }

  const response = await fetch(`${baseUrl}${options.path}`, {
    method: "POST",
    credentials: "include",
    headers,
    body: options.formData,
    signal: options.signal,
  });

  if (!response.ok) {
    let problem: PlatformProblemDetails = { status: response.status };
    try {
      problem = { ...problem, ...parseProblem(await response.json()) };
    } catch {
      // Non-JSON error bodies still surface as a status-only problem.
    }
    notifyAuthLostFromMultipart(response.status, problem.errorCode, options.path);
    throw new PlatformApiError(response.status, problem, {
      requestCorrelationId,
      method: "POST",
      path: options.path,
    });
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function globalProductImageUrl(
  baseUrl: string,
  productId: string,
  variant: "thumb" | "medium",
  version?: number,
): string {
  const path = `/api/v1/platform/global-catalog/products/${productId}/image/${variant}`;
  if (version == null) {
    return `${baseUrl}${path}`;
  }
  return `${baseUrl}${path}?v=${version}`;
}
