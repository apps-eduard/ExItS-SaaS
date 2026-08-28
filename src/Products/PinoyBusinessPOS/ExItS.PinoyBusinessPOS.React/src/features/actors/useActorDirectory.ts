import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  resolveOrganizationActorDisplayNames,
  type OrganizationActorDisplayName,
} from "@/api/platform/actor-directory-client";

export function sortActorIds(actorIds: Array<string | null | undefined>): string[] {
  return [
    ...new Set(
      actorIds
        .filter((id): id is string => typeof id === "string" && id.trim().length > 0)
        .map((id) => id.trim()),
    ),
  ].sort();
}

export function actorDirectoryQueryKey(organizationId: string, actorIds: string[]) {
  return ["pos-actor-directory", organizationId, ...actorIds] as const;
}

/**
 * Org-scoped React Query cache for actor display names.
 * Pass every actor id visible on the current detail/history surface.
 */
export function useActorDirectory(
  organizationId: string | null | undefined,
  actorIds: Array<string | null | undefined>,
) {
  const actorIdsKey = sortActorIds(actorIds).join("|");
  const sortedIds = useMemo(
    () => (actorIdsKey ? actorIdsKey.split("|") : []),
    [actorIdsKey],
  );

  const query = useQuery({
    queryKey: actorDirectoryQueryKey(organizationId ?? "", sortedIds),
    enabled: Boolean(organizationId) && sortedIds.length > 0,
    staleTime: 5 * 60_000,
    queryFn: ({ signal }) =>
      resolveOrganizationActorDisplayNames(organizationId!, sortedIds, signal),
  });

  const byId = useMemo(() => {
    const map = new Map<string, OrganizationActorDisplayName>();
    for (const item of query.data ?? []) {
      map.set(item.actorId.toLowerCase(), item);
    }
    return map;
  }, [query.data]);

  function resolve(actorId: string | null | undefined): OrganizationActorDisplayName | null {
    if (!actorId) {
      return null;
    }
    return byId.get(actorId.toLowerCase()) ?? null;
  }

  return {
    ...query,
    sortedIds,
    resolve,
    isResolving: query.isLoading || query.isFetching,
  };
}
