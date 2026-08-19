export type ApiProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
  traceId?: string;
};

export class ApiClientError extends Error {
  readonly status: number;
  readonly problem: ApiProblemDetails;
  readonly requestCorrelationId: string;
  readonly source: "platform" | "pos";

  constructor(
    source: "platform" | "pos",
    status: number,
    problem: ApiProblemDetails,
    requestCorrelationId: string,
  ) {
    super(problem.detail ?? problem.title ?? `${source} API request failed (${status})`);
    this.name = "ApiClientError";
    this.source = source;
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

function parseProblem(payload: unknown): ApiProblemDetails {
  if (typeof payload !== "object" || payload === null) {
    return {};
  }
  const record = payload as Record<string, unknown>;
  return {
    title: typeof record.title === "string" ? record.title : undefined,
    status: typeof record.status === "number" ? record.status : undefined,
    detail: typeof record.detail === "string" ? record.detail : undefined,
    errorCode: typeof record.errorCode === "string" ? record.errorCode : undefined,
    traceId: typeof record.traceId === "string" ? record.traceId : undefined,
  };
}

export type ApiRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  body?: unknown;
  signal?: AbortSignal;
};

export function getPlatformApiBaseUrl(): string {
  return import.meta.env.VITE_PLATFORM_API_BASE_URL ?? "http://127.0.0.1:8091";
}

export function getPosApiBaseUrl(): string {
  return import.meta.env.VITE_POS_API_BASE_URL ?? "http://127.0.0.1:8092";
}

export function getAppVersion(): string {
  return import.meta.env.VITE_APP_VERSION ?? "0.0.1-impl-02";
}

async function apiRequest<T>(
  source: "platform" | "pos",
  baseUrl: string,
  options: ApiRequestOptions,
): Promise<T> {
  const requestCorrelationId = createCorrelationId();
  const headers = new Headers({
    Accept: "application/json",
    "X-Correlation-Id": requestCorrelationId,
  });

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${baseUrl}${options.path}`, {
    method: options.method ?? "GET",
    credentials: source === "platform" ? "include" : "omit",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  });

  if (!response.ok) {
    let problem: ApiProblemDetails = { status: response.status };
    try {
      problem = { ...problem, ...parseProblem(await response.json()) };
    } catch {
      // Non-JSON error bodies still surface as a status-only problem.
    }
    throw new ApiClientError(source, response.status, problem, requestCorrelationId);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function platformRequest<T>(options: ApiRequestOptions): Promise<T> {
  return apiRequest<T>("platform", getPlatformApiBaseUrl(), options);
}

export function posRequest<T>(options: ApiRequestOptions): Promise<T> {
  return apiRequest<T>("pos", getPosApiBaseUrl(), options);
}
