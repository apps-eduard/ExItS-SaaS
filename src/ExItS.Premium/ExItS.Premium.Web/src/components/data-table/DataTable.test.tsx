import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { DataTable } from "./DataTable";
import type { DataTableColumn } from "./types";

type LedgerRow = {
  id: string;
  reference: string;
  customer: string;
  category: string;
  amount: number;
  date: string;
};

const columns: DataTableColumn<LedgerRow>[] = [
  { id: "reference", header: "Transaction ID", accessor: (row) => row.reference, width: 140, pin: "start" },
  { id: "customer", header: "Customer", accessor: (row) => row.customer, searchValue: (row) => row.customer },
  { id: "category", header: "Category", accessor: (row) => row.category },
  { id: "date", header: "Date", accessor: (row) => row.date, sortType: "date" },
  { id: "amount", header: "Amount", accessor: (row) => row.amount, sortType: "number", align: "end" },
];

function makeRows(count: number): LedgerRow[] {
  return Array.from({ length: count }, (_, index) => ({
    id: `TX-${index + 1}`,
    reference: `TX-${index + 1}`,
    customer: index === 0 ? "Amina Cruz" : `Customer ${index + 1}`,
    category: index % 2 === 0 ? "Sales" : "Supplies",
    amount: (index + 1) * 10,
    date: `2026-01-${String((index % 27) + 1).padStart(2, "0")}`,
  }));
}

function Harness({ initial = makeRows(12) }: { initial?: LedgerRow[] }) {
  const [rows, setRows] = useState(initial);
  const [notice, setNotice] = useState("");
  return (
    <>
      <p role="status">{notice}</p>
      <DataTable
        title="Transactions"
        columns={columns}
        rows={rows}
        selectionLabel={(row) => row.reference}
        searchLabel="Search transactions"
        filters={[{ id: "category", label: "Category", columnId: "category", options: [{ value: "Sales", label: "Sales" }, { value: "Supplies", label: "Supplies" }] }]}
        rowActions={() => [
          { id: "view", label: "View", icon: "info" },
          { id: "edit", label: "Edit", icon: "edit_note" },
          { id: "delete", label: "Delete", icon: "delete", tone: "danger" },
        ]}
        onRowAction={(row, actionId) => {
          if (actionId === "delete") {
            setRows((current) => current.filter((item) => item.id !== row.id));
            setNotice(`Deleted ${row.reference}`);
            return;
          }
          setNotice(`${actionId} ${row.reference}`);
        }}
        renderBulkActions={(ids) => (
          <button type="button" onClick={() => setNotice(`Reviewed ${ids.length}`)}>
            Review selected
          </button>
        )}
      />
    </>
  );
}

describe("DataTable", () => {
  it("sorts, shift-sorts, and keeps a selected row checked", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("checkbox", { name: "Select TX-1" }));
    await user.click(screen.getByRole("button", { name: /Amount/ }));
    const amountHeader = screen.getByRole("columnheader", { name: /Amount/ });
    expect(amountHeader).toHaveAttribute("aria-sort", "ascending");
    expect(screen.getByRole("checkbox", { name: "Select TX-1" })).toBeChecked();

    fireEvent.click(screen.getByRole("button", { name: /Date/ }), { shiftKey: true });
    expect(screen.getByRole("columnheader", { name: /Date/ })).toHaveAttribute("aria-sort", "ascending");
    expect(screen.getByRole("checkbox", { name: "Select TX-1" })).toBeChecked();
    expect(screen.getByText("1 selected")).toBeInTheDocument();
  });

  it("filters with search and a column filter, then clears them", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.type(screen.getByRole("searchbox", { name: "Search transactions" }), "Amina");
    expect(screen.getByText(/1 of 12 records/)).toBeInTheDocument();
    await user.clear(screen.getByRole("searchbox", { name: "Search transactions" }));
    await user.type(screen.getByRole("searchbox", { name: "Filter Category" }), "Supplies");
    expect(screen.getByText("Customer 2")).toBeInTheDocument();
    expect(screen.queryByText("Amina Cruz")).not.toBeInTheDocument();
    await user.type(screen.getByRole("searchbox", { name: "Search transactions" }), "zzzz-none");
    expect(screen.getByText("No matching records")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Clear filters" }));
    expect(screen.getByText("Amina Cruz")).toBeInTheDocument();
  });

  it("selects the current page, pages forward, and resizes and hides a column", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("checkbox", { name: "Select all rows on this page" }));
    expect(screen.getByText("10 selected")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Review selected" }));
    expect(screen.getByText("Reviewed 10")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Next" }));
    expect(screen.getByRole("button", { name: "Page 2" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByText("Customer 11")).toBeInTheDocument();
    expect(screen.queryByText("Amina Cruz")).not.toBeInTheDocument();
    expect(screen.queryByRole("checkbox", { name: "Select TX-1" })).not.toBeInTheDocument();

    const resize = screen.getByRole("separator", { name: "Resize Amount" });
    resize.focus();
    await user.keyboard("{ArrowRight}");
    expect(resize).toHaveAttribute("aria-valuenow", "176");

    await user.click(screen.getByText("Columns"));
    await user.click(screen.getByRole("checkbox", { name: "Category" }));
    expect(screen.queryByRole("button", { name: /Category/ })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Restore defaults" }));
    expect(screen.getByRole("button", { name: /Category/ })).toBeInTheDocument();
  });

  it("runs view and delete without leaving the page", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const row = screen.getByText("Amina Cruz").closest("tr");
    expect(row).toBeTruthy();
    await user.click(within(row as HTMLElement).getByRole("button", { name: "Actions for TX-1" }));
    await user.click(screen.getByRole("menuitem", { name: "View" }));
    expect(screen.getByText("view TX-1")).toBeInTheDocument();
    await user.click(within(row as HTMLElement).getByRole("button", { name: "Actions for TX-1" }));
    await user.click(screen.getByRole("menuitem", { name: "Delete" }));
    expect(screen.getByText("Deleted TX-1")).toBeInTheDocument();
    expect(screen.queryByText("Amina Cruz")).not.toBeInTheDocument();
  });

  it("shows loading, empty, and error retry states", async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <DataTable title="Ledger" columns={columns} rows={[]} loading selectionMode="none" searchable={false} enableColumnFilters={false} />,
    );
    expect(screen.getByLabelText("Loading records")).toBeInTheDocument();
    rerender(
      <DataTable
        title="Ledger"
        columns={columns}
        rows={[]}
        selectionMode="none"
        searchable={false}
        enableColumnFilters={false}
        emptyTitle="No transactions yet"
        emptyDescription="Nothing recorded."
      />,
    );
    expect(screen.getByText("No transactions yet")).toBeInTheDocument();
    const onRetry = viRetry();
    rerender(
      <DataTable
        title="Ledger"
        columns={columns}
        rows={makeRows(2)}
        selectionMode="none"
        searchable={false}
        enableColumnFilters={false}
        error="The sample ledger could not be read."
        onRetry={onRetry.fn}
      />,
    );
    expect(screen.getByRole("alert")).toHaveTextContent("The sample ledger could not be read.");
    await user.click(screen.getByRole("button", { name: "Retry" }));
    expect(onRetry.calls).toBe(1);
  });
});

function viRetry() {
  const state = { calls: 0, fn: () => undefined as void };
  state.fn = () => {
    state.calls += 1;
  };
  return state;
}
