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

let inMemoryAntiforgeryToken: string | null = null;
let inMemoryAntiforgeryHeaderName: string = PlatformAntiforgeryDefaults.headerName;
/** When the Platform antiforgery endpoint is missing (404) or account-scoped away (403), skip CSRF headers. */
let antiforgeryBootstrapState: "unknown" | "ready" | "unavailable" = "unknown";

function isMutationMethod(method: string): boolean {
  return method === "POST" || method === "PUT" || method === "PATCH" || method === "DELETE";
}

export function clearPlatformAntiforgeryToken(): void {
  inMemoryAntiforgeryToken = null;
  antiforgeryBootstrapState = "unknown";
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
  inMemoryAntiforgeryHeaderName = payload.headerName || PlatformAntiforgeryDefaults.headerName;
  inMemoryAntiforgeryToken = payload.token;
  antiforgeryBootstrapState = "ready";
  return payload;
}

async function ensureAntiforgeryToken(signal?: AbortSignal): Promise<void> {
  if (inMemoryAntiforgeryToken || antiforgeryBootstrapState === "unavailable") {
    return;
  }

  try {
    await bootstrapAntiforgeryToken(signal);
  } catch (error) {
    // Live-preview Platform images may omit the token route (404). Organization/Personal
    // sessions are also blocked from /platform/antiforgery/* by account-scope (403) even
    // though auth context mutations under /platform/auth/* remain allowed without CSRF.
    if (error instanceof PlatformApiError && (error.status === 404 || error.status === 403)) {
      antiforgeryBootstrapState = "unavailable";
      return;
    }
    throw error;
  }
}

export async function platformRequest<T>(options: PlatformRequestOptions): Promise<T> {
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
    if (inMemoryAntiforgeryToken) {
      headers.set(inMemoryAntiforgeryHeaderName, inMemoryAntiforgeryToken);
    }
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
    throw new PlatformApiError(response.status, problem, requestCorrelationId);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
