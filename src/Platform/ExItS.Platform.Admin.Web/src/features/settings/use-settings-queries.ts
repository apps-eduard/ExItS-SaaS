import { useQuery } from "@tanstack/react-query";
import {
  getPlatformEmailSettings,
  getPlatformGeneralSettings,
  getPlatformRegionalSettings,
} from "@/api/settings/settings-client";
import { env } from "@/lib/env";

export const platformGeneralSettingsQueryKey = ["platform-settings", "general"] as const;
export const platformEmailSettingsQueryKey = ["platform-settings", "email"] as const;
export const platformRegionalSettingsQueryKey = ["platform-settings", "regional"] as const;

export function usePlatformGeneralSettingsQuery(enabled: boolean) {
  return useQuery({
    queryKey: platformGeneralSettingsQueryKey,
    enabled,
    queryFn: ({ signal }) => getPlatformGeneralSettings(env.platformApiBaseUrl, signal),
  });
}

export function usePlatformEmailSettingsQuery(enabled: boolean) {
  return useQuery({
    queryKey: platformEmailSettingsQueryKey,
    enabled,
    queryFn: ({ signal }) => getPlatformEmailSettings(env.platformApiBaseUrl, signal),
  });
}

export function usePlatformRegionalSettingsQuery(enabled: boolean) {
  return useQuery({
    queryKey: platformRegionalSettingsQueryKey,
    enabled,
    queryFn: ({ signal }) => getPlatformRegionalSettings(env.platformApiBaseUrl, signal),
  });
}
