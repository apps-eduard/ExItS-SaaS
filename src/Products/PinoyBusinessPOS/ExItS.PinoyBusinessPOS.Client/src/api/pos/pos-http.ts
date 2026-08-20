import { getPosAccessToken } from "@/api/platform/pos-access-token";
import { createCorrelationId } from "@/api/platform/platform-http";

export const POS_API_BASE_PATH = "/pos-api";

const ABSOLUTE_API_PATTERN = /^https?:\/\//i;

export type PosProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
  traceId?: string;
};

export class PosApiError extends Error {
  readonly status: number;
  readonly problem: PosProblemDetails;
  readonly requestCorrelationId?: string;

  constructor(status: number, problem: PosProblemDetails, requestCorrelationId?: string) {
    super(problem.detail ?? problem.title ?? `POS API request failed (${status})`);
    this.name = "PosApiError";
    this.status = status;
    this.problem = problem;
    this.requestCorrelationId = requestCorrelationId;
  }

  get errorCode(): string | undefined {
    return this.problem.errorCode;
  }
}

export function assertRelativePosBase(baseUrl: string): void {
  if (
    ABSOLUTE_API_PATTERN.test(baseUrl) ||
    baseUrl.includes("://") ||
    /:(?:8092)\b/.test(baseUrl)
  ) {
    throw new Error("POS API calls must stay on the relative /pos-api origin.");
  }
}

function readStringField(record: Record<string, unknown>, key: string): string | undefined {
  const value = record[key];
  return typeof value === "string" ? value : undefined;
}

function parseProblem(payload: unknown): PosProblemDetails {
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

export type PosWorkspaceScope = {
  organizationId: string;
  branchId: string;
};

export type PosRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  body?: unknown;
  /** When set, sent as multipart; do not also set `body`. Browser sets Content-Type boundary. */
  formData?: FormData;
  signal?: AbortSignal;
  workspace: PosWorkspaceScope;
};

function buildPosHeaders(
  workspace: PosWorkspaceScope,
  requestCorrelationId: string,
  accept: string,
): Headers {
  const headers = new Headers({
    Accept: accept,
    "X-Correlation-Id": requestCorrelationId,
    "X-Pos-Organization-Id": workspace.organizationId,
    "X-Pos-Branch-Id": workspace.branchId,
  });

  const accessToken = getPosAccessToken();
  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }
  return headers;
}

async function throwIfNotOk(response: Response, requestCorrelationId: string): Promise<void> {
  if (response.ok) {
    return;
  }
  let problem: PosProblemDetails = { status: response.status };
  try {
    problem = { ...problem, ...parseProblem(await response.json()) };
  } catch {
    // Non-JSON error bodies still surface as a status-only problem.
  }
  throw new PosApiError(response.status, problem, requestCorrelationId);
}

export async function posRequest<T>(options: PosRequestOptions): Promise<T> {
  assertRelativePosBase(POS_API_BASE_PATH);

  const method = options.method ?? "GET";
  const requestCorrelationId = createCorrelationId();
  const headers = buildPosHeaders(options.workspace, requestCorrelationId, "application/json");

  let body: BodyInit | undefined;
  if (options.formData !== undefined) {
    body = options.formData;
  } else if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
    body = JSON.stringify(options.body);
  }

  const response = await fetch(`${POS_API_BASE_PATH}${options.path}`, {
    method,
    credentials: "include",
    headers,
    body,
    signal: options.signal,
  });

  await throwIfNotOk(response, requestCorrelationId);

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function posRequestBlob(options: PosRequestOptions): Promise<Blob> {
  assertRelativePosBase(POS_API_BASE_PATH);

  const method = options.method ?? "GET";
  const requestCorrelationId = createCorrelationId();
  const headers = buildPosHeaders(options.workspace, requestCorrelationId, "*/*");

  const response = await fetch(`${POS_API_BASE_PATH}${options.path}`, {
    method,
    credentials: "include",
    headers,
    signal: options.signal,
  });

  await throwIfNotOk(response, requestCorrelationId);
  return response.blob();
}
