import { useQuery } from "@tanstack/react-query";
import {
  getPersonalFeatureDefinition,
  listPersonalFeatureDefinitions,
} from "@/api/personal-features/personal-features-client";
import { env } from "@/lib/env";

export const personalFeaturesListQueryKey = ["personal-features", "list"] as const;

export const personalFeatureDetailQueryKey = (featureCode: string) =>
  ["personal-features", "detail", featureCode] as const;

export function usePersonalFeaturesListQuery(enabled: boolean) {
  return useQuery({
    queryKey: personalFeaturesListQueryKey,
    enabled,
    queryFn: ({ signal }) => listPersonalFeatureDefinitions(env.platformApiBaseUrl, signal),
  });
}

export function usePersonalFeatureDetailQuery(featureCode: string | null, enabled: boolean) {
  return useQuery({
    queryKey: personalFeatureDetailQueryKey(featureCode ?? ""),
    enabled: enabled && featureCode != null && featureCode.length > 0,
    queryFn: ({ signal }) =>
      getPersonalFeatureDefinition(env.platformApiBaseUrl, featureCode!, signal),
  });
}
