import { platformRequest } from "@/api/platform-http";

export type SystemHealthStatus =
  | "Healthy"
  | "Degraded"
  | "Unhealthy"
  | "Unavailable"
  | "Unknown"
  | "NotAvailable";

export type HostResourceSnapshot = {
  cpuPercent: number | null;
  memoryUsedBytes: number | null;
  memoryTotalBytes: number | null;
  storageUsedBytes: number | null;
  storageFreeBytes: number | null;
  storageTotalBytes: number | null;
  uptimeSeconds: number | null;
};

export type ServiceHealthSnapshot = {
  name: string;
  status: SystemHealthStatus;
  latencyMs: number | null;
  checkedAtUtc: string;
};

export type BuildMetadataSnapshot = {
  environment: string;
  applicationVersion: string | null;
  commitSha: string | null;
};

export type BackupHealthSnapshot = {
  status: SystemHealthStatus;
  lastSuccessfulAtUtc: string | null;
  ageSeconds: number | null;
};

export type SystemHealthSnapshot = {
  overallStatus: SystemHealthStatus;
  host: HostResourceSnapshot;
  services: ServiceHealthSnapshot[];
  build: BuildMetadataSnapshot;
  backup: BackupHealthSnapshot;
};

export const SYSTEM_HEALTH_PATH = "/api/v1/platform/operations/system-health";

export function getSystemHealth(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<SystemHealthSnapshot> {
  return platformRequest<SystemHealthSnapshot>(baseUrl, {
    path: SYSTEM_HEALTH_PATH,
    signal,
  });
}
