import { PlatformAntiforgeryDefaults } from "@/api/platform-antiforgery";

import { createCorrelationId } from "@/lib/create-correlation-id";
import { sanitizeApiPath } from "@/lib/diagnostics/diagnostic-redaction";



export type PlatformProblemDetails = {

  title?: string;

  status?: number;

  detail?: string;

  errorCode?: string;

  traceId?: string;

  correlationId?: string;

};



export type PlatformApiErrorContext = {

  requestCorrelationId?: string;

  method?: string;

  path?: string;

};



export class PlatformApiError extends Error {

  readonly status: number;

  readonly problem: PlatformProblemDetails;

  readonly requestCorrelationId?: string;

  readonly method?: string;

  readonly path?: string;



  constructor(

    status: number,

    problem: PlatformProblemDetails,

    context?: string | PlatformApiErrorContext,

  ) {

    super(problem.detail ?? problem.title ?? `Platform API request failed (${status})`);

    this.name = "PlatformApiError";

    this.status = status;

    this.problem = problem;

    const resolved = typeof context === "string" ? { requestCorrelationId: context } : context;

    this.requestCorrelationId = resolved?.requestCorrelationId;

    this.method = resolved?.method;

    this.path = sanitizeApiPath(resolved?.path);

  }



  get errorCode(): string | undefined {

    return this.problem.errorCode;

  }



  get traceId(): string | undefined {

    return this.problem.traceId;

  }

}



export type PlatformNetworkFailureKind = "fetch_failed" | "timeout";



export class PlatformNetworkError extends Error {

  readonly method: string;

  readonly path: string;

  readonly requestCorrelationId: string;

  readonly networkFailureKind: PlatformNetworkFailureKind;



  constructor(options: {

    method: string;

    path: string;

    requestCorrelationId: string;

    networkFailureKind?: PlatformNetworkFailureKind;

  }) {

    super("The browser could not complete the request.");

    this.name = "PlatformNetworkError";

    this.method = options.method;

    this.path = sanitizeApiPath(options.path) ?? options.path;

    this.requestCorrelationId = options.requestCorrelationId;

    this.networkFailureKind = options.networkFailureKind ?? "fetch_failed";

  }

}



export { createCorrelationId } from "@/lib/create-correlation-id";`n`n


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

    correlationId:

      readStringField(record, "correlationId") ??

      readStringField(record, "CorrelationId") ??

      (extensions ? readStringField(extensions, "correlationId") : undefined),

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



function isAbortError(error: unknown): boolean {

  return error instanceof DOMException && error.name === "AbortError";

}



function readResponseCorrelationId(

  response: Response,

  problem: PlatformProblemDetails,

  requestCorrelationId: string,

): string {

  try {

    const headerValue = response.headers?.get?.("X-Correlation-Id");

    if (typeof headerValue === "string" && headerValue.trim().length > 0) {

      return headerValue;

    }

  } catch {

    // Test doubles and some adapters may omit Headers.

  }

  return problem.correlationId ?? requestCorrelationId;

}



async function bootstrapAntiforgeryToken(

  baseUrl: string,

  signal?: AbortSignal,

): Promise<AntiforgeryBootstrap> {

  const requestCorrelationId = createCorrelationId();

  const path = PlatformAntiforgeryDefaults.tokenPath;



  let response: Response;

  try {

    response = await fetch(`${baseUrl}${path}`, {

      method: "GET",

      credentials: "include",

      headers: {

        Accept: "application/json",

        "X-Correlation-Id": requestCorrelationId,

      },

      signal,

    });

  } catch (error) {

    if (isAbortError(error)) {

      throw error;

    }

    throw new PlatformNetworkError({

      method: "GET",

      path,

      requestCorrelationId,

    });

  }



  if (!response.ok) {

    let problem: PlatformProblemDetails = { status: response.status };

    try {

      problem = { ...problem, ...parseProblem(await response.json()) };

    } catch {

      // Non-JSON bootstrap failures still surface as status-only problems.

    }

    throw new PlatformApiError(response.status, problem, {

      requestCorrelationId,

      method: "GET",

      path,

    });

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

  const sanitizedPath = sanitizeApiPath(options.path) ?? options.path;

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



  let response: Response;

  try {

    response = await fetch(`${baseUrl}${options.path}`, {

      method,

      credentials: "include",

      headers,

      body: options.body === undefined ? undefined : JSON.stringify(options.body),

      signal: options.signal,

    });

  } catch (error) {

    if (isAbortError(error)) {

      throw error;

    }

    throw new PlatformNetworkError({

      method,

      path: sanitizedPath,

      requestCorrelationId,

    });

  }



  if (!response.ok) {

    let problem: PlatformProblemDetails = { status: response.status };

    try {

      problem = { ...problem, ...parseProblem(await response.json()) };

    } catch {

      // Non-JSON error bodies still surface as a status-only problem.

    }

    const responseCorrelationId = readResponseCorrelationId(

      response,

      problem,

      requestCorrelationId,

    );

    throw new PlatformApiError(response.status, problem, {

      requestCorrelationId: responseCorrelationId,

      method,

      path: sanitizedPath,

    });

  }



  return parseSuccessResponseBody<T>(response);

}


