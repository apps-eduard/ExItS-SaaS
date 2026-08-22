import { PlatformAntiforgeryDefaults } from "@/api/platform-antiforgery";

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

function isMutationMethod(method: string): boolean {
  return method === "POST" || method === "PUT" || method === "PATCH" || method === "DELETE";
}

export function clearPlatformAntiforgeryToken(): void {
  inMemoryAntiforgeryToken = null;
}

async function bootstrapAntiforgeryToken(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<AntiforgeryBootstrap> {
  const response = await fetch(`${baseUrl}${PlatformAntiforgeryDefaults.tokenPath}`, {
    method: "GET",
    credentials: "include",
    headers: {
      Accept: "application/json",
      "X-Correlation-Id": createCorrelationId(),
    },
    signal,
  });

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
  return payload;
}

async function ensureAntiforgeryToken(baseUrl: string, signal?: AbortSignal): Promise<void> {
  if (inMemoryAntiforgeryToken) {
    return;
  }
  await bootstrapAntiforgeryToken(baseUrl, signal);
}

async function parseSuccessResponseBody<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  if (typeof response.text === "function") {
    const bodyText = await response.text();
    if (bodyText.trim().length === 0) {
      return undefined as T;
    }
    return JSON.parse(bodyText) as T;
  }

  return (await response.json()) as T;
}

export async function platformRequest<T>(
  baseUrl: string,
  options: PlatformRequestOptions,
): Promise<T> {
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
    await ensureAntiforgeryToken(baseUrl, options.signal);
    if (inMemoryAntiforgeryToken) {
      headers.set(inMemoryAntiforgeryHeaderName, inMemoryAntiforgeryToken);
    }
  }

  const response = await fetch(`${baseUrl}${options.path}`, {
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

  return parseSuccessResponseBody<T>(response);
}
