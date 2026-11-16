import type { ColumnSort, DataTableColumn, SortType } from "./types";

export function cellText<T>(column: DataTableColumn<T>, row: T, kind: "search" | "filter"): string {
  if (kind === "search" && column.searchValue) return column.searchValue(row);
  if (kind === "filter" && column.filterValue) return column.filterValue(row);
  if (kind === "search" && column.filterValue) return column.filterValue(row);
  const value = column.accessor(row);
  if (value instanceof Date) return value.toISOString();
  if (value == null) return "";
  return String(value);
}

function sortScalar<T>(column: DataTableColumn<T>, row: T): string | number {
  const value = column.accessor(row);
  const type: SortType = column.sortType ?? (typeof value === "number" ? "number" : value instanceof Date ? "date" : "text");
  if (type === "number") {
    const numeric = typeof value === "number" ? value : Number(String(value).replace(/[^0-9.-]/g, ""));
    return Number.isFinite(numeric) ? numeric : 0;
  }
  if (type === "date") {
    if (value instanceof Date) return value.getTime();
    const parsed = Date.parse(String(value ?? ""));
    return Number.isFinite(parsed) ? parsed : 0;
  }
  return String(value ?? "").toLocaleLowerCase();
}

function compareScalar(left: string | number, right: string | number) {
  if (typeof left === "number" && typeof right === "number") return left - right;
  return String(left).localeCompare(String(right), undefined, { numeric: true, sensitivity: "base" });
}

export function compareRows<T>(left: T, right: T, sorts: readonly ColumnSort[], columns: readonly DataTableColumn<T>[]) {
  for (const sort of sorts) {
    const column = columns.find((item) => item.id === sort.id);
    if (!column || column.sortable === false) continue;
    const result = compareScalar(sortScalar(column, left), sortScalar(column, right));
    if (result !== 0) return sort.direction === "asc" ? result : -result;
  }
  return 0;
}

export function sortRows<T>(rows: readonly T[], sorts: readonly ColumnSort[], columns: readonly DataTableColumn<T>[]) {
  if (sorts.length === 0) return [...rows];
  return [...rows].sort((left, right) => compareRows(left, right, sorts, columns));
}

/** Click cycles unsorted → ascending → descending → unsorted. Shift-click adds a sort level. */
export function nextSort(sorts: readonly ColumnSort[], columnId: string, additive: boolean): ColumnSort[] {
  const current = sorts.find((sort) => sort.id === columnId);
  if (!additive) {
    if (!current) return [{ id: columnId, direction: "asc" }];
    if (current.direction === "asc") return [{ id: columnId, direction: "desc" }];
    return [];
  }
  if (!current) return [...sorts, { id: columnId, direction: "asc" }];
  if (current.direction === "asc") {
    return sorts.map((sort) => (sort.id === columnId ? { id: columnId, direction: "desc" } : sort));
  }
  return sorts.filter((sort) => sort.id !== columnId);
}

export function filterRows<T>(
  rows: readonly T[],
  columns: readonly DataTableColumn<T>[],
  query: string,
  columnFilters: Readonly<Record<string, string>>,
  optionFilters: Readonly<Record<string, string>>,
) {
  const needle = query.trim().toLocaleLowerCase();
  return rows.filter((row) => {
    if (needle) {
      const haystack = columns.map((column) => cellText(column, row, "search")).join("\n").toLocaleLowerCase();
      if (!haystack.includes(needle)) return false;
    }
    for (const [columnId, raw] of Object.entries(columnFilters)) {
      const term = raw.trim().toLocaleLowerCase();
      if (!term) continue;
      const column = columns.find((item) => item.id === columnId);
      if (!column) continue;
      if (!cellText(column, row, "filter").toLocaleLowerCase().includes(term)) return false;
    }
    for (const [columnId, raw] of Object.entries(optionFilters)) {
      if (!raw) continue;
      const column = columns.find((item) => item.id === columnId);
      if (!column) continue;
      if (String(column.accessor(row) ?? "").toLocaleLowerCase() !== raw.toLocaleLowerCase()) return false;
    }
    return true;
  });
}

export function paginate<T>(rows: readonly T[], page: number, pageSize: number) {
  const size = Math.max(1, Math.floor(pageSize));
  const pageCount = Math.max(1, Math.ceil(rows.length / size));
  const current = Math.min(Math.max(1, Math.floor(page) || 1), pageCount);
  const start = (current - 1) * size;
  return {
    page: current,
    pageCount,
    start,
    rows: rows.slice(start, start + size),
  };
}

export function pageItems(page: number, pageCount: number): Array<number | "gap"> {
  if (pageCount <= 7) return Array.from({ length: pageCount }, (_, index) => index + 1);
  const wanted = new Set([1, pageCount, page - 1, page, page + 1].filter((value) => value >= 1 && value <= pageCount));
  const ordered = [...wanted].sort((left, right) => left - right);
  const items: Array<number | "gap"> = [];
  for (const value of ordered) {
    const previous = items[items.length - 1];
    if (typeof previous === "number" && value - previous > 1) items.push("gap");
    items.push(value);
  }
  return items;
}

export function toggleId(selected: readonly string[], id: string, mode: "single" | "multiple") {
  if (mode === "single") return selected[0] === id ? [] : [id];
  return selected.includes(id) ? selected.filter((item) => item !== id) : [...selected, id];
}

export function togglePage(selected: readonly string[], pageIds: readonly string[]) {
  const all = pageIds.length > 0 && pageIds.every((id) => selected.includes(id));
  if (all) return selected.filter((id) => !pageIds.includes(id));
  return [...selected, ...pageIds.filter((id) => !selected.includes(id))];
}
