import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";

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
  return crypto.randomUUID();
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
      problem = { ...problem, ...parseProblem(await response.json()) };
    } catch {
      // Non-JSON bootstrap failures still surface as status-only problems.
    }
    throw new PlatformApiError(response.status, problem);
  }

  const payload = (await response.json()) as AntiforgeryBootstrap;
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
      problem = { ...problem, ...parseProblem(await response.json()) };
    } catch {
      // Non-JSON error bodies still surface as a status-only problem.
    }
    const apiError = new PlatformApiError(response.status, problem, requestCorrelationId);
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

  return (await response.json()) as T;
}

export async function platformRequest<T>(options: PlatformRequestOptions): Promise<T> {
  return executePlatformRequest<T>(options, { csrfRetried: false });
}
