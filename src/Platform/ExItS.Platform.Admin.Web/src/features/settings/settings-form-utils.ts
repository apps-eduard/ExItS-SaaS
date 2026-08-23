import { PlatformApiError } from "@/api/platform-http";

export const settingsControlClassName =
  "w-full min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring";

export function isPlatformSettingsForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}
