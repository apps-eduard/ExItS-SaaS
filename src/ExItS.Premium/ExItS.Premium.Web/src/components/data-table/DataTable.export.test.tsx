import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { DataTable } from "./DataTable";
import type { DataTableColumn } from "./types";

type LedgerRow = { id: string; name: string; amount: number; date: string };

const columns: DataTableColumn<LedgerRow>[] = [
  { id: "name", header: "Customer", accessor: (row) => row.name },
  { id: "amount", header: "Amount", accessor: (row) => row.amount, sortType: "number" },
  { id: "date", header: "Date", accessor: (row) => row.date, sortType: "date" },
];

function rows(count: number): LedgerRow[] {
  return Array.from({ length: count }, (_, index) => ({
    id: `TX-${index + 1}`,
    name: index === 0 ? "Amina Cruz" : `Customer ${index + 1}`,
    amount: (index + 1) * 10,
    date: `2026-04-${String((index % 27) + 1).padStart(2, "0")}`,
  }));
}

function Harness({ initial = rows(12), error = null as string | null }: { initial?: LedgerRow[]; error?: string | null }) {
  const [data, setData] = useState(initial);
  return (
    <DataTable
      title="Transactions"
      columns={columns}
      rows={data}
      error={error}
      selectionLabel={(row) => row.id}
      onRowAction={(row, actionId) => {
        if (actionId === "delete") setData((current) => current.filter((item) => item.id !== row.id));
      }}
    />
  );
}

describe("DataTable export dialog", () => {
  it("exports the current page as a csv download and keeps selected rows available across the filter", async () => {
    const user = userEvent.setup();
    const blobs: Blob[] = [];
    const names: string[] = [];
    vi.spyOn(URL, "createObjectURL").mockImplementation((value) => {
      blobs.push(value as Blob);
      return "blob:export";
    });
    vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function click(this: HTMLAnchorElement) {
      names.push(this.download);
    });

    render(<Harness />);
    await user.click(screen.getByRole("button", { name: "Export" }));
    expect(screen.getByRole("dialog", { name: "Export" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Excel (.xlsx)" })).toBeChecked();
    expect(screen.getByRole("radio", { name: "Current page (10)" })).toBeChecked();
    expect(screen.getByRole("radio", { name: "All filtered records (12)" })).toBeEnabled();
    expect(screen.getByRole("radio", { name: "Selected records (0)" })).toBeDisabled();
    expect(screen.getByText(/Excel file with 10 records from current page/)).toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: "CSV (.csv)" }));
    expect(screen.getByRole("radio", { name: "CSV (.csv)" })).toBeChecked();
    await user.click(screen.getByRole("button", { name: "Export 10" }));
    await waitFor(() => expect(blobs).toHaveLength(1));
    expect(names[0]).toMatch(/\.csv$/);
    const text = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(String(reader.result));
      reader.onerror = () => reject(reader.error);
      reader.readAsText(blobs[0]);
    });
    expect(text.includes("Customer,Amount,Date")).toBe(true);
    expect(names[0]).toMatch(/^exits-transactions-\d{4}-\d{2}-\d{2}\.csv$/);
    const lines = text.replace(/^\uFEFF/, "").trim().split("\r\n");
    expect(lines[0]).toBe("Customer,Amount,Date");
    expect(lines).toHaveLength(11);
    expect(lines[1]).toContain("Amina Cruz,10,2026-04-01");
    vi.restoreAllMocks();
  });

  it("refuses an empty result and does not export a failed table", async () => {
    const user = userEvent.setup();
    const { unmount } = render(<Harness initial={[]} />);
    await user.click(screen.getByRole("button", { name: "Export" }));
    const dialog = screen.getByRole("dialog", { name: "Export" });
    expect(within(dialog).getByText(/No records in this scope/)).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Export" })).toBeDisabled();
    unmount();

    render(<Harness error="The sample ledger could not be read." />);
    await user.click(screen.getByRole("button", { name: "Export" }));
    const failed = screen.getByRole("dialog", { name: "Export" });
    expect(within(failed).getByText("The sample ledger could not be read.")).toBeInTheDocument();
    expect(within(failed).getByRole("button", { name: "Export 10" })).toBeDisabled();
  });
});
