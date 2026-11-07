import { createCorrelationId } from "./correlation";
import { getDurableInstallationDeviceId } from "./installation-device-id";
import { getPosAccessToken } from "./pos-access-token";

export const POS_API_BASE_PATH = "/pos-api";

export type PosWorkspaceScope = {
  organizationId: string;
  branchId?: string | null;
};

export type PosProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
};

export class PosApiError extends Error {
  readonly status: number;
  readonly problem: PosProblemDetails;

  constructor(status: number, problem: PosProblemDetails) {
    super(problem.detail ?? problem.title ?? `POS API request failed (${status})`);
    this.name = "PosApiError";
    this.status = status;
    this.problem = problem;
  }
}

function parseProblem(payload: unknown): PosProblemDetails {
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
  };
}

export async function posRequest<T>(options: {
  method?: "GET" | "POST";
  path: string;
  workspace: PosWorkspaceScope;
  signal?: AbortSignal;
}): Promise<T> {
  const requestCorrelationId = createCorrelationId();
  const headers = new Headers({
    Accept: "application/json",
    "X-Correlation-Id": requestCorrelationId,
    "X-Pos-Organization-Id": options.workspace.organizationId,
  });
  if (options.workspace.branchId) {
    headers.set("X-Pos-Branch-Id", options.workspace.branchId);
  }
  const token = getPosAccessToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  const installation = getDurableInstallationDeviceId();
  if (installation.ok) {
    headers.set("X-Pos-Installation-Device-Id", installation.installationDeviceId);
  }

  const response = await fetch(`${POS_API_BASE_PATH}${options.path}`, {
    method: options.method ?? "GET",
    credentials: "include",
    headers,
    signal: options.signal,
  });

  if (!response.ok) {
    let problem: PosProblemDetails = { status: response.status };
    try {
      problem = { ...problem, ...parseProblem(await response.json()) };
    } catch {
      // status-only
    }
    throw new PosApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}
