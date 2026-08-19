export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export function parsePagedResult<T>(payload: unknown): PagedResult<T> {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid paged result.");
  }

  const record = payload as Record<string, unknown>;
  const items = record.items ?? record.Items;
  const totalCount = record.totalCount ?? record.TotalCount;
  const page = record.page ?? record.Page;
  const pageSize = record.pageSize ?? record.PageSize;

  if (!Array.isArray(items) || typeof totalCount !== "number" || !Number.isFinite(totalCount)) {
    throw new Error("Invalid paged result.");
  }

  return {
    items: items as T[],
    totalCount,
    page: typeof page === "number" ? page : 1,
    pageSize: typeof pageSize === "number" ? pageSize : items.length,
  };
}
