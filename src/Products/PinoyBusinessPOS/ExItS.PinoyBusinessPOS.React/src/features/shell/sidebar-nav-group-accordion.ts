import { z } from "zod";

export const SIDEBAR_NAV_GROUPS_STORAGE_KEY = "exits.pos-client.sidebar-nav-groups.v1";

export type SidebarNavGroupScope = "admin" | "operations";

/** true = group expanded (children visible). */
export type SidebarNavGroupExpandedMap = Record<string, boolean>;

const storeSchema = z.object({
  admin: z.record(z.string(), z.boolean()).default({}),
  operations: z.record(z.string(), z.boolean()).default({}),
});

export type SidebarNavGroupStore = z.infer<typeof storeSchema>;

const defaultStore: SidebarNavGroupStore = {
  admin: {},
  operations: {},
};

export function readSidebarNavGroupStore(): SidebarNavGroupStore {
  if (typeof window === "undefined") {
    return defaultStore;
  }
  try {
    const raw = window.localStorage.getItem(SIDEBAR_NAV_GROUPS_STORAGE_KEY);
    if (!raw) {
      return defaultStore;
    }
    const parsed = storeSchema.safeParse(JSON.parse(raw));
    return parsed.success ? parsed.data : defaultStore;
  } catch {
    return defaultStore;
  }
}

export function writeSidebarNavGroupStore(store: SidebarNavGroupStore): void {
  if (typeof window === "undefined") {
    return;
  }
  window.localStorage.setItem(SIDEBAR_NAV_GROUPS_STORAGE_KEY, JSON.stringify(store));
}

/**
 * Default: every known group starts expanded.
 * Missing keys default to expanded so new groups appear open.
 */
export function resolveGroupExpandedMap(
  stored: SidebarNavGroupExpandedMap,
  groupIds: readonly string[],
): SidebarNavGroupExpandedMap {
  const next: SidebarNavGroupExpandedMap = {};
  for (const id of groupIds) {
    next[id] = stored[id] !== false;
  }
  return next;
}

export function findActiveGroupId<TGroup extends { id: string; items: ReadonlyArray<{ id: string }> }>(
  groups: readonly TGroup[],
  activeItemId: string | null | undefined,
): string | null {
  if (!activeItemId) {
    return null;
  }
  for (const group of groups) {
    if (group.items.some((item) => item.id === activeItemId)) {
      return group.id;
    }
  }
  return null;
}

export function areAllGroupsExpanded(map: SidebarNavGroupExpandedMap, groupIds: readonly string[]): boolean {
  return groupIds.length > 0 && groupIds.every((id) => map[id] !== false);
}
