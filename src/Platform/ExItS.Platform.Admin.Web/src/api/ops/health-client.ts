import { createCorrelationId } from "@/api/platform-http";

export type HealthReportedStatus = "Healthy" | "Degraded" | "Unhealthy" | "Unknown";

export type HealthCheckSnapshot = {
  httpStatus: number;
  reportedStatus: HealthReportedStatus;
  rawBody: string;
};

export type PlatformHealthSnapshot = {
  liveness: HealthCheckSnapshot;
  readiness: HealthCheckSnapshot;
};

function parseReportedStatus(body: string): HealthReportedStatus {
  const normalized = body.trim();
  if (normalized === "Healthy" || normalized === "Degraded" || normalized === "Unhealthy") {
    return normalized;
  }
  return "Unknown";
}

function isHtmlDocumentBody(body: string): boolean {
  const head = body.slice(0, 200).toLowerCase();
  return head.includes("<!doctype") || head.includes("<html");
}

/** Keep widget labels short; never surface SPA HTML fallbacks. */
export function sanitizeHealthRawBody(body: string): string {
  const trimmed = body.trim();
  if (trimmed.length === 0 || isHtmlDocumentBody(trimmed) || trimmed.length > 120) {
    return "";
  }
  return trimmed;
}

async function fetchHealthPath(
  baseUrl: string,
  path: string,
  signal?: AbortSignal,
): Promise<HealthCheckSnapshot> {
  const requestCorrelationId = createCorrelationId();
  const response = await fetch(`${baseUrl}${path}`, {
    method: "GET",
    credentials: "include",
    headers: {
      Accept: "text/plain, application/json",
      "X-Correlation-Id": requestCorrelationId,
    },
    signal,
  });

  const rawBody = sanitizeHealthRawBody(await response.text());
  const snapshot: HealthCheckSnapshot = {
    httpStatus: response.status,
    reportedStatus: parseReportedStatus(rawBody),
    rawBody,
  };

  if (response.status === 200 || response.status === 503) {
    // SPA/nginx HTML fallback often returns 200 with no parseable status.
    if (snapshot.reportedStatus === "Unknown" && rawBody.length === 0) {
      throw new Error(`Health endpoint ${path} returned an unusable body.`);
    }
    return snapshot;
  }

  throw new Error(`Health endpoint ${path} returned HTTP ${response.status}.`);
}

export function getPlatformHealth(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<PlatformHealthSnapshot> {
  return Promise.all([
    fetchHealthPath(baseUrl, "/health", signal),
    fetchHealthPath(baseUrl, "/health/ready", signal),
  ]).then(([liveness, readiness]) => ({ liveness, readiness }));
}
