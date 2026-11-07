import { PlatformAntiforgeryDefaults } from "./antiforgery";
import { createCorrelationId } from "./correlation";

export const PLATFORM_API_BASE_PATH = "/platform-api";

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

  constructor(status: number, problem: PlatformProblemDetails) {
    super(problem.detail ?? problem.title ?? `Platform API request failed (${status})`);
    this.name = "PlatformApiError";
    this.status = status;
    this.problem = problem;
  }

  get errorCode(): string | undefined {
    return this.problem.errorCode;
  }
}

function parseProblem(payload: unknown): PlatformProblemDetails {
  if (typeof payload !== "object" || payload === null) return {};
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

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text.trim()) return undefined;
  return JSON.parse(text) as unknown;
}

let antiforgeryToken: string | null = null;
let antiforgeryHeaderName: string = PlatformAntiforgeryDefaults.headerName;

export function clearPlatformAntiforgeryToken() {
  antiforgeryToken = null;
}

async function bootstrapAntiforgery(signal?: AbortSignal) {
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
    throw new PlatformApiError(response.status, {
      status: response.status,
      ...(await readJson(response).then(parseProblem).catch(() => ({}))),
    });
  }
  const payload = (await readJson(response)) as
    | { headerName?: string; token?: string }
    | undefined;
  if (!payload?.token?.trim()) {
    throw new PlatformApiError(response.status, {
      status: response.status,
      detail: "Antiforgery bootstrap returned an empty token.",
      errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
    });
  }
  antiforgeryHeaderName = payload.headerName || PlatformAntiforgeryDefaults.headerName;
  antiforgeryToken = payload.token;
}

export type PlatformRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  body?: unknown;
  signal?: AbortSignal;
  skipAntiforgery?: boolean;
};

export async function platformRequest<T>(options: PlatformRequestOptions): Promise<T> {
  const method = options.method ?? "GET";
  const isMutation = method !== "GET";
  const headers = new Headers({
    Accept: "application/json",
    "X-Correlation-Id": createCorrelationId(),
  });

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (isMutation && !options.skipAntiforgery) {
    if (!antiforgeryToken) {
      await bootstrapAntiforgery(options.signal);
    }
    if (!antiforgeryToken) {
      throw new PlatformApiError(500, {
        status: 500,
        detail: "Antiforgery token missing after bootstrap.",
        errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
      });
    }
    headers.set(antiforgeryHeaderName, antiforgeryToken);
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
      problem = { ...problem, ...parseProblem(await readJson(response)) };
    } catch {
      // status-only
    }
    throw new PlatformApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}
