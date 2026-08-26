import { PlatformApiError } from "@/api/platform-http";

export const settingsControlClassName =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full min-w-0 rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring";

export function isPlatformSettingsForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}
