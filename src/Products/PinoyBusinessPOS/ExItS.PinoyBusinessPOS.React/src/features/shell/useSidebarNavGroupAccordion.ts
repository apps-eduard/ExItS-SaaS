import { useCallback, useEffect, useMemo, useState } from "react";
import {
  areAllGroupsExpanded,
  readSidebarNavGroupStore,
  resolveGroupExpandedMap,
  writeSidebarNavGroupStore,
  type SidebarNavGroupExpandedMap,
  type SidebarNavGroupScope,
} from "@/features/shell/sidebar-nav-group-accordion";

/**
 * Per-scope accordion state for Org Admin / Operations sidenav groups.
 * Keeps the active-route group open; persists preferences in localStorage.
 */
export function useSidebarNavGroupAccordion(options: {
  scope: SidebarNavGroupScope;
  groupIds: readonly string[];
  activeGroupId: string | null;
}) {
  const { scope, groupIds, activeGroupId } = options;
  const groupIdsKey = groupIds.join("\0");

  const [expandedMap, setExpandedMap] = useState<SidebarNavGroupExpandedMap>(() => {
    const stored = readSidebarNavGroupStore()[scope];
    const resolved = resolveGroupExpandedMap(stored, groupIds);
    if (activeGroupId) {
      resolved[activeGroupId] = true;
    }
    return resolved;
  });

  // Sync when the set of groups changes (permissions / branch type).
  useEffect(() => {
    setExpandedMap((current) => {
      const stored = readSidebarNavGroupStore()[scope];
      const merged = resolveGroupExpandedMap({ ...stored, ...current }, groupIds);
      if (activeGroupId) {
        merged[activeGroupId] = true;
      }
      return merged;
    });
    // groupIdsKey tracks identity of groupIds array contents
    // eslint-disable-next-line react-hooks/exhaustive-deps -- intentional key
  }, [scope, groupIdsKey]);

  // Always reveal the group that owns the current route.
  useEffect(() => {
    if (!activeGroupId) {
      return;
    }
    setExpandedMap((current) => {
      if (current[activeGroupId] === true) {
        return current;
      }
      return { ...current, [activeGroupId]: true };
    });
  }, [activeGroupId]);

  const persist = useCallback(
    (next: SidebarNavGroupExpandedMap) => {
      const store = readSidebarNavGroupStore();
      writeSidebarNavGroupStore({ ...store, [scope]: next });
    },
    [scope],
  );

  const setGroupExpanded = useCallback(
    (groupId: string, expanded: boolean) => {
      setExpandedMap((current) => {
        const next = { ...current, [groupId]: expanded };
        // Never hide the active route's group.
        if (activeGroupId) {
          next[activeGroupId] = true;
        }
        persist(next);
        return next;
      });
    },
    [activeGroupId, persist],
  );

  const toggleGroup = useCallback(
    (groupId: string) => {
      setExpandedMap((current) => {
        const currentlyExpanded = current[groupId] !== false;
        // Active group cannot be collapsed.
        if (currentlyExpanded && groupId === activeGroupId) {
          return current;
        }
        const next = { ...current, [groupId]: !currentlyExpanded };
        if (activeGroupId) {
          next[activeGroupId] = true;
        }
        persist(next);
        return next;
      });
    },
    [activeGroupId, persist],
  );

  const allExpanded = useMemo(
    () => areAllGroupsExpanded(expandedMap, groupIds),
    [expandedMap, groupIds],
  );

  const expandAll = useCallback(() => {
    const next: SidebarNavGroupExpandedMap = {};
    for (const id of groupIds) {
      next[id] = true;
    }
    setExpandedMap(next);
    persist(next);
  }, [groupIds, persist]);

  const collapseAll = useCallback(() => {
    const next: SidebarNavGroupExpandedMap = {};
    for (const id of groupIds) {
      // Keep the active route's group visible.
      next[id] = id === activeGroupId;
    }
    setExpandedMap(next);
    persist(next);
  }, [activeGroupId, groupIds, persist]);

  const toggleAll = useCallback(() => {
    if (allExpanded) {
      collapseAll();
    } else {
      expandAll();
    }
  }, [allExpanded, collapseAll, expandAll]);

  const isGroupExpanded = useCallback(
    (groupId: string) => expandedMap[groupId] !== false,
    [expandedMap],
  );

  return {
    expandedMap,
    isGroupExpanded,
    toggleGroup,
    setGroupExpanded,
    allExpanded,
    expandAll,
    collapseAll,
    toggleAll,
  };
}
