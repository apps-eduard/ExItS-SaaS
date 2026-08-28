import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import { maybeNotifyAuthenticationLost } from "@/session/session-expiry";

export const PLATFORM_API_BASE_PATH = "/platform-api";

const ABSOLUTE_API_PATTERN = /^https?:\/\//i;

export type PlatformProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
  traceId?: string;
};

export class PlatformApiError extends Error {
  readonly status: number;
  readonly problem: PlatformProblemDetails;
  readonly requestCorrelationId?: string;

  constructor(status: number, problem: PlatformProblemDetails, requestCorrelationId?: string) {
    super(problem.detail ?? problem.title ?? `Platform API request failed (${status})`);
    this.name = "PlatformApiError";
    this.status = status;
    this.problem = problem;
    this.requestCorrelationId = requestCorrelationId;
  }

  get errorCode(): string | undefined {
    return this.problem.errorCode;
  }

  get traceId(): string | undefined {
    return this.problem.traceId;
  }
}

export function createCorrelationId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  if (typeof crypto !== "undefined" && typeof crypto.getRandomValues === "function") {
    const bytes = new Uint8Array(16);
    crypto.getRandomValues(bytes);
    bytes[6] = (bytes[6]! & 0x0f) | 0x40;
    bytes[8] = (bytes[8]! & 0x3f) | 0x80;
    const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20, 32)}`;
  }

  throw new Error("Secure randomness is unavailable for Platform API correlation ids.");
}

export function assertRelativePlatformBase(baseUrl: string): void {
  if (
    ABSOLUTE_API_PATTERN.test(baseUrl) ||
    baseUrl.includes("://") ||
    /:(?:8091)\b/.test(baseUrl)
  ) {
    throw new Error("Platform API calls must stay on the relative /platform-api origin.");
  }
}

function readStringField(record: Record<string, unknown>, key: string): string | undefined {
  const value = record[key];
  return typeof value === "string" ? value : undefined;
}

async function readJsonResponseBody(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text.trim()) {
    return undefined;
  }
  return JSON.parse(text) as unknown;
}

function parseProblem(payload: unknown): PlatformProblemDetails {
  if (typeof payload !== "object" || payload === null) {
    return {};
  }

  const record = payload as Record<string, unknown>;
  const extensions =
    typeof record.extensions === "object" && record.extensions !== null
      ? (record.extensions as Record<string, unknown>)
      : undefined;

  return {
    title: readStringField(record, "title"),
    status: typeof record.status === "number" ? record.status : undefined,
    detail: readStringField(record, "detail"),
    errorCode:
      readStringField(record, "errorCode") ??
      readStringField(record, "ErrorCode") ??
      (extensions ? readStringField(extensions, "errorCode") : undefined) ??
      (extensions ? readStringField(extensions, "ErrorCode") : undefined),
    traceId: readStringField(record, "traceId") ?? readStringField(record, "TraceId"),
  };
}

export type PlatformRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  body?: unknown;
  signal?: AbortSignal;
  skipAntiforgery?: boolean;
  /** When true, 401 responses do not trigger the central session-expiry transition. */
  skipSessionExpiry?: boolean;
};

type AntiforgeryBootstrap = {
  headerName: string;
  token: string;
};

type PlatformRequestState = {
  csrfRetried: boolean;
};

let inMemoryAntiforgeryToken: string | null = null;
let inMemoryAntiforgeryHeaderName: string = PlatformAntiforgeryDefaults.headerName;

function isMutationMethod(method: string): boolean {
  return method === "POST" || method === "PUT" || method === "PATCH" || method === "DELETE";
}

export function clearPlatformAntiforgeryToken(): void {
  inMemoryAntiforgeryToken = null;
}

export function hasPlatformAntiforgeryToken(): boolean {
  return inMemoryAntiforgeryToken !== null;
}

/** Best-effort CSRF bootstrap for cookie-session mutations (sign-in page / post-login). */
export async function prefetchPlatformAntiforgeryToken(options?: {
  force?: boolean;
}): Promise<boolean> {
  if (!options?.force && inMemoryAntiforgeryToken) {
    return true;
  }

  return refreshPlatformAntiforgeryToken();
}

/** Clear stale header token and bootstrap a fresh cookie/request-token pair (same-origin credentialed). */
export async function refreshPlatformAntiforgeryToken(): Promise<boolean> {
  clearPlatformAntiforgeryToken();
  try {
    await bootstrapAntiforgeryToken();
    return true;
  } catch {
    return false;
  }
}

export function isPlatformAntiforgeryValidationError(error: PlatformApiError): boolean {
  if (error.errorCode === PlatformAntiforgeryDefaults.invalidErrorCode) {
    return true;
  }

  if (error.status === 419) {
    return true;
  }

  if (error.status === 400) {
    const detail = error.problem.detail?.toLowerCase() ?? "";
    if (detail.includes("antiforgery")) {
      return true;
    }
  }

  return false;
}

async function bootstrapAntiforgeryToken(signal?: AbortSignal): Promise<AntiforgeryBootstrap> {
  assertRelativePlatformBase(PLATFORM_API_BASE_PATH);
  const response = await fetch(
    `${PLATFORM_API_BASE_PATH}${PlatformAntiforgeryDefaults.tokenPath}`,
    {
      method: "GET",
      credentials: "include",
      headers: {
        Accept: "application/json",
        "X-Correlation-Id": createCorrelationId(),
      },
      signal,
    },
  );

  if (!response.ok) {
    let problem: PlatformProblemDetails = { status: response.status };
    try {
      const payload = await readJsonResponseBody(response);
      if (payload !== undefined) {
        problem = { ...problem, ...parseProblem(payload) };
      }
    } catch {
      // Non-JSON bootstrap failures still surface as status-only problems.
    }
    throw new PlatformApiError(response.status, problem);
  }

  const payload = (await readJsonResponseBody(response)) as AntiforgeryBootstrap | undefined;
  if (!payload) {
    throw new PlatformApiError(response.status, {
      status: response.status,
      detail: "Browser antiforgery bootstrap returned an empty body.",
      errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
    });
  }
  if (!payload.token?.trim()) {
    throw new PlatformApiError(response.status, {
      status: response.status,
      detail: "Browser antiforgery bootstrap returned an empty token.",
      errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
    });
  }

  inMemoryAntiforgeryHeaderName = payload.headerName || PlatformAntiforgeryDefaults.headerName;
  inMemoryAntiforgeryToken = payload.token;
  return payload;
}

async function ensureAntiforgeryToken(signal?: AbortSignal): Promise<void> {
  if (inMemoryAntiforgeryToken) {
    return;
  }

  await bootstrapAntiforgeryToken(signal);
}

async function executePlatformRequest<T>(
  options: PlatformRequestOptions,
  state: PlatformRequestState,
): Promise<T> {
  assertRelativePlatformBase(PLATFORM_API_BASE_PATH);

  const method = options.method ?? "GET";
  const requestCorrelationId = createCorrelationId();
  const headers = new Headers({
    Accept: "application/json",
    "X-Correlation-Id": requestCorrelationId,
  });

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (isMutationMethod(method) && !options.skipAntiforgery) {
    await ensureAntiforgeryToken(options.signal);
    if (!inMemoryAntiforgeryToken) {
      throw new PlatformApiError(500, {
        status: 500,
        detail: "Browser antiforgery token is required but missing after bootstrap.",
        errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
      });
    }
    headers.set(inMemoryAntiforgeryHeaderName, inMemoryAntiforgeryToken);
  }

  const response = await fetch(`${PLATFORM_API_BASE_PATH}${options.path}`, {
    method,
    credentials: "include",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  });

  if (!response.ok) {
    let problem: PlatformProblemDetails = { status: response.status };
    try {
      const payload = await readJsonResponseBody(response);
      if (payload !== undefined) {
        problem = { ...problem, ...parseProblem(payload) };
      }
    } catch {
      // Non-JSON error bodies still surface as a status-only problem.
    }
    const apiError = new PlatformApiError(response.status, problem, requestCorrelationId);
    maybeNotifyAuthenticationLost({
      status: response.status,
      errorCode: problem.errorCode,
      detail: problem.detail,
      path: options.path,
      skipSessionExpiry: options.skipSessionExpiry,
    });
    if (
      !state.csrfRetried &&
      isMutationMethod(method) &&
      !options.skipAntiforgery &&
      isPlatformAntiforgeryValidationError(apiError)
    ) {
      clearPlatformAntiforgeryToken();
      state.csrfRetried = true;
      return executePlatformRequest<T>(options, state);
    }
    throw apiError;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const payload = await readJsonResponseBody(response);
  return payload as T;
}

export async function platformRequest<T>(options: PlatformRequestOptions): Promise<T> {
  return executePlatformRequest<T>(options, { csrfRetried: false });
}
