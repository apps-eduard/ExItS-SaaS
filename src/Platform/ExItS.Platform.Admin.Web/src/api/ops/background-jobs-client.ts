import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";

export type PlatformBackgroundJob = {
  id: string;
  source: string;
  jobType: string;
  status: string;
  totalCount?: number | null;
  processedCount?: number | null;
  importedCount?: number | null;
  skippedCount?: number | null;
  failedCount?: number | null;
  currentStage?: string | null;
  failureSummary?: string | null;
  requestedAtUtc?: string | null;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  attemptCount?: number | null;
  displayName?: string | null;
};

export type PlatformBackgroundJobDetail = {
  summary: PlatformBackgroundJob;
  requestedBy?: string | null;
  fileFormat?: string | null;
  fileSizeBytes?: number | null;
  idempotencyKey?: string | null;
  lastHeartbeatAtUtc?: string | null;
  previewSummary?: string | null;
};

export type ListBackgroundJobsOptions = {
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};

export const BACKGROUND_JOBS_PATH = "/api/v1/platform/operations/jobs";
export const BACKGROUND_JOBS_PAGE_SIZE = 20;

export const BACKGROUND_JOB_STATUSES = [
  "Validated",
  "Queued",
  "Processing",
  "Completed",
  "CompletedWithWarnings",
  "Failed",
] as const;

export function listBackgroundJobs(
  baseUrl: string,
  options: ListBackgroundJobsOptions = {},
): Promise<PagedResult<PlatformBackgroundJob>> {
  const params = new URLSearchParams();
  if (options.status) {
    params.set("status", options.status);
  }
  if (options.search) {
    params.set("search", options.search);
  }
  if (options.page && options.page > 1) {
    params.set("page", String(options.page));
  }
  if (options.pageSize) {
    params.set("pageSize", String(options.pageSize));
  }

  const query = params.toString();
  return platformRequest<unknown>(baseUrl, {
    path: query ? `${BACKGROUND_JOBS_PATH}?${query}` : BACKGROUND_JOBS_PATH,
    signal: options.signal,
  }).then((payload) => parsePagedResult<PlatformBackgroundJob>(payload));
}

export function getBackgroundJob(
  baseUrl: string,
  jobId: string,
  signal?: AbortSignal,
): Promise<PlatformBackgroundJobDetail> {
  return platformRequest<PlatformBackgroundJobDetail>(baseUrl, {
    path: `${BACKGROUND_JOBS_PATH}/${jobId}`,
    signal,
  });
}
