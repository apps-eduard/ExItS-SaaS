export type ConsentRequestGroup<T> = {
  key: string;
  primary: T;
  items: readonly T[];
  duplicateCount: number;
};

export function groupByKey<T>(
  items: readonly T[],
  getKey: (item: T) => string,
  sortItems?: (left: T, right: T) => number,
): ConsentRequestGroup<T>[] {
  const map = new Map<string, T[]>();
  for (const item of items) {
    const key = getKey(item);
    const list = map.get(key) ?? [];
    list.push(item);
    map.set(key, list);
  }

  return [...map.entries()].map(([key, groupItems]) => {
    const sorted = sortItems ? [...groupItems].sort(sortItems) : groupItems;
    return {
      key,
      primary: sorted[0],
      items: sorted,
      duplicateCount: sorted.length,
    };
  });
}

export function sortByNewestUtc<T>(getCreatedAtUtc: (item: T) => string) {
  return (left: T, right: T) =>
    new Date(getCreatedAtUtc(right)).getTime() - new Date(getCreatedAtUtc(left)).getTime();
}

export function siblingRequestIds(primaryId: string, items: ReadonlyArray<{ id: string }>): string[] {
  return items.filter((item) => item.id !== primaryId).map((item) => item.id);
}

export async function cascadeResolveSiblingRequests(
  siblingIds: readonly string[],
  resolve: (requestId: string) => Promise<unknown>,
): Promise<void> {
  if (siblingIds.length === 0) {
    return;
  }

  await Promise.all(
    siblingIds.map((requestId) =>
      resolve(requestId).catch(() => {
        /* sibling may already be resolved */
      }),
    ),
  );
}
