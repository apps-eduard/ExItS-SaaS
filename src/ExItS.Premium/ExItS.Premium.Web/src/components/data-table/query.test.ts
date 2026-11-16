import { describe, expect, it } from "vitest";
import { filterRows, nextSort, paginate, sortRows } from "./query";
import type { DataTableColumn } from "./types";

type Row = { id: string; name: string; amount: number; date: string };

const columns: DataTableColumn<Row>[] = [
  { id: "name", header: "Name", accessor: (row) => row.name },
  { id: "amount", header: "Amount", accessor: (row) => row.amount, sortType: "number" },
  { id: "date", header: "Date", accessor: (row) => row.date, sortType: "date" },
];

const rows: Row[] = [
  { id: "a", name: "Bea", amount: 20, date: "2026-03-02" },
  { id: "b", name: "Ana", amount: 5, date: "2026-01-15" },
  { id: "c", name: "Cia", amount: 20, date: "2026-02-01" },
];

describe("data table query", () => {
  it("sorts text, amounts, and dates, then adds a shift-sort level", () => {
    expect(sortRows(rows, [{ id: "name", direction: "asc" }], columns).map((row) => row.id)).toEqual(["b", "a", "c"]);
    expect(sortRows(rows, [{ id: "amount", direction: "asc" }], columns).map((row) => row.amount)).toEqual([5, 20, 20]);
    expect(sortRows(rows, [{ id: "date", direction: "desc" }], columns).map((row) => row.id)).toEqual(["a", "c", "b"]);

    const sorts = nextSort(nextSort([], "amount", false), "date", true);
    expect(sorts).toEqual([
      { id: "amount", direction: "asc" },
      { id: "date", direction: "asc" },
    ]);
    expect(sortRows(rows, sorts, columns).map((row) => row.id)).toEqual(["b", "c", "a"]);
    expect(nextSort([{ id: "amount", direction: "asc" }], "amount", false)).toEqual([{ id: "amount", direction: "desc" }]);
    expect(nextSort([{ id: "amount", direction: "desc" }], "amount", false)).toEqual([]);
  });

  it("combines search and column filters and reports the page after the result shrinks", () => {
    const filtered = filterRows(rows, columns, "bea", { amount: "20" }, {});
    expect(filtered.map((row) => row.id)).toEqual(["a"]);
    expect(paginate(filtered, 4, 10).page).toBe(1);
    expect(paginate(rows, 2, 2)).toMatchObject({ page: 2, pageCount: 2, rows: [rows[2]] });
  });

  it("sorts two thousand rows quickly enough to skip virtualization", () => {
    const many: Row[] = Array.from({ length: 2000 }, (_, index) => ({
      id: `id-${index}`,
      name: `Name ${index % 40}`,
      amount: (index * 17) % 5000,
      date: `2026-${String((index % 12) + 1).padStart(2, "0")}-01`,
    }));
    const start = performance.now();
    const sorted = sortRows(many, [{ id: "amount", direction: "desc" }, { id: "name", direction: "asc" }], columns);
    const elapsed = performance.now() - start;
    expect(sorted).toHaveLength(2000);
    expect(sorted[0]?.amount).toBeGreaterThanOrEqual(sorted[1]?.amount ?? 0);
    expect(elapsed).toBeLessThan(80);
  });
});
