import { useMemo, useState } from "react";
import { Badge, type BadgeTone } from "../../components/badge/Badge";
import { Button } from "../../components/button/Button";
import { DataTable, sortRows, type DataTableColumn, type DataTableDensity, type DataTableSelectionMode } from "../../components/data-table";
import { SegmentedControl } from "../../components/segmented/SegmentedControl";
import type { MenuItem } from "../../components/menu/DropdownMenu";
import {
  TRANSACTION_CATEGORIES,
  TRANSACTION_STATUSES,
  createTransactions,
  formatLedgerDate,
  formatPeso,
  type Transaction,
  type TransactionStatus,
} from "./sample";
import styles from "./DataTablePlayground.module.scss";

type Scenario = "sample" | "large" | "loading" | "empty" | "error";

const STATUS_TONE: Record<TransactionStatus, BadgeTone> = {
  Posted: "success",
  Pending: "warning",
  Failed: "danger",
  Refunded: "info",
};

const ROW_ACTIONS: MenuItem[] = [
  { id: "view", label: "View", icon: "info" },
  { id: "edit", label: "Edit", icon: "edit_note" },
  { id: "delete", label: "Delete", icon: "delete", tone: "danger" },
];

function transactionColumns(): DataTableColumn<Transaction>[] {
  return [
    {
      id: "reference",
      header: "Transaction ID",
      accessor: (row) => row.reference,
      width: 150,
      minWidth: 120,
      pin: "start",
    },
    {
      id: "customer",
      header: "Customer",
      accessor: (row) => row.customer,
      searchValue: (row) => `${row.customer} ${row.description}`,
      width: 240,
      minWidth: 160,
      wrap: true,
      cell: (row) => (
        <span className={styles.person}>
          <span>{row.customer}</span>
          <span className={styles.meta}>{row.description}</span>
        </span>
      ),
    },
    {
      id: "date",
      header: "Date",
      accessor: (row) => row.date,
      sortType: "date",
      filterValue: (row) => `${row.date} ${formatLedgerDate(row.date)}`,
      width: 140,
      minWidth: 120,
      cell: (row) => formatLedgerDate(row.date),
    },
    {
      id: "category",
      header: "Category",
      accessor: (row) => row.category,
      width: 140,
      minWidth: 110,
    },
    {
      id: "amount",
      header: "Amount",
      accessor: (row) => row.amount,
      sortType: "number",
      align: "end",
      width: 140,
      minWidth: 110,
      filterValue: (row) => `${row.amount} ${formatPeso(row.amount)}`,
      cell: (row) => <span className={styles.amount}>{formatPeso(row.amount)}</span>,
    },
    {
      id: "status",
      header: "Status",
      accessor: (row) => row.status,
      width: 130,
      minWidth: 110,
      cell: (row) => (
        <Badge tone={STATUS_TONE[row.status]} appearance="soft" indicator>
          {row.status}
        </Badge>
      ),
    },
  ];
}

const PREVIEW_COLUMNS: DataTableColumn<Transaction>[] = [
  { id: "reference", header: "Transaction ID", accessor: (row) => row.reference, width: 140 },
  { id: "customer", header: "Customer", accessor: (row) => row.customer, width: 180 },
  {
    id: "amount",
    header: "Amount",
    accessor: (row) => row.amount,
    sortType: "number",
    align: "end",
    width: 120,
    cell: (row) => formatPeso(row.amount),
  },
];

export function DataTableDemo() {
  const columns = useMemo(() => transactionColumns(), []);
  const sampleSeed = useMemo(() => createTransactions(36), []);
  const largeSeed = useMemo(() => createTransactions(2000, 7), []);
  const [scenario, setScenario] = useState<Scenario>("sample");
  const [density, setDensity] = useState<DataTableDensity>("standard");
  const [selectionMode, setSelectionMode] = useState<DataTableSelectionMode>("multiple");
  const [rows, setRows] = useState(sampleSeed);
  const [notice, setNotice] = useState("Local actions stay in this page.");
  const [sortMs, setSortMs] = useState<number | null>(null);

  function chooseScenario(next: Scenario) {
    setScenario(next);
    if (next === "sample") setRows(sampleSeed);
    if (next === "large") {
      setRows(largeSeed);
      const start = performance.now();
      sortRows(largeSeed, [{ id: "amount", direction: "desc" }, { id: "date", direction: "asc" }], columns);
      setSortMs(Math.round((performance.now() - start) * 10) / 10);
    } else {
      setSortMs(null);
    }
    if (next === "error") setNotice("The ledger failed to load. Retry restores the sample.");
  }

  function onRowAction(row: Transaction, actionId: string) {
    if (actionId === "view") {
      setNotice(`Viewing ${row.reference} · ${row.customer}`);
      return;
    }
    if (actionId === "edit") {
      setNotice(`Editing ${row.reference}. Nothing is sent to a server.`);
      return;
    }
    if (actionId === "delete") {
      setRows((current) => current.filter((item) => item.id !== row.id));
      setNotice(`Deleted ${row.reference} from this page only.`);
    }
  }

  function record() {
    const reference = `TX-${String(rows.length + 1).padStart(4, "0")}`;
    const row: Transaction = {
      id: reference,
      reference,
      customer: "New counter sale",
      description: "Recorded locally",
      date: "2026-11-16",
      category: "Sales",
      amount: 125,
      status: "Pending",
    };
    setRows((current) => [row, ...current]);
    setNotice(`Recorded ${reference}.`);
  }

  const activeRows = scenario === "empty" ? [] : rows;

  return (
    <div className={styles.page}>
      <div className={styles.controls}>
        <SegmentedControl
          label="Dataset"
          name="data-table-dataset"
          value={scenario}
          onChange={chooseScenario}
          options={[
            { value: "sample", label: "Sample" },
            { value: "large", label: "2,000 rows" },
            { value: "loading", label: "Loading" },
            { value: "empty", label: "Empty" },
            { value: "error", label: "Error" },
          ]}
        />
        <SegmentedControl
          label="Density"
          name="data-table-density"
          value={density}
          onChange={setDensity}
          options={[
            { value: "compact", label: "Compact" },
            { value: "standard", label: "Standard" },
            { value: "large", label: "Large" },
          ]}
        />
        <SegmentedControl
          label="Selection"
          name="data-table-selection"
          value={selectionMode}
          onChange={setSelectionMode}
          options={[
            { value: "multiple", label: "Multiple" },
            { value: "single", label: "Single" },
            { value: "none", label: "None" },
          ]}
        />
      </div>
      <p className={styles.note} role="status">
        {notice}
        {sortMs != null ? ` Multi-sort of 2,000 records took ${sortMs} ms.` : ""}
      </p>

      <DataTable
        key={scenario}
        title={scenario === "large" ? "Large ledger" : "Transactions"}
        columns={columns}
        rows={activeRows}
        density={density}
        selectionMode={selectionMode}
        selectionLabel={(row) => row.reference}
        searchLabel="Search transactions"
        searchPlaceholder="Customer, description, or ID"
        filters={[
          {
            id: "category",
            label: "Category",
            columnId: "category",
            options: TRANSACTION_CATEGORIES.map((category) => ({ value: category, label: category })),
          },
          {
            id: "status",
            label: "Status",
            columnId: "status",
            options: TRANSACTION_STATUSES.map((status) => ({ value: status, label: status })),
          },
        ]}
        primaryAction={
          <Button type="button" onClick={record} disabled={scenario === "loading" || scenario === "error" || scenario === "empty"}>
            Record transaction
          </Button>
        }
        renderBulkActions={(ids) => (
          <Button type="button" variant="secondary" onClick={() => setNotice(`Reviewed ${ids.length} selected ${ids.length === 1 ? "record" : "records"}.`)}>
            Review selected
          </Button>
        )}
        rowActions={() => ROW_ACTIONS}
        onRowAction={onRowAction}
        loading={scenario === "loading"}
        error={scenario === "error" ? "The sample ledger could not be read." : null}
        onRetry={() => chooseScenario("sample")}
        emptyTitle="No transactions yet"
        emptyDescription="Record a transaction to start this ledger. This sample does not call the API."
      />

      <section className={styles.block} aria-labelledby="data-table-states">
        <h2 id="data-table-states" className="type-section">
          States
        </h2>
        <div className={styles.states}>
          <DataTable
            title="Loading preview"
            columns={PREVIEW_COLUMNS}
            rows={sampleSeed.slice(0, 4)}
            density="compact"
            selectionMode="none"
            loading
            searchable={false}
            enableColumnFilters={false}
            enableColumnVisibility={false}
            enableColumnResize={false}
          />
          <DataTable
            title="Empty preview"
            columns={PREVIEW_COLUMNS}
            rows={[]}
            density="compact"
            selectionMode="none"
            searchable={false}
            enableColumnFilters={false}
            enableColumnVisibility={false}
            enableColumnResize={false}
            emptyTitle="No transactions yet"
            emptyDescription="An empty ledger stays inside the table frame."
          />
          <DataTable
            title="Error preview"
            columns={PREVIEW_COLUMNS}
            rows={sampleSeed.slice(0, 4)}
            density="compact"
            selectionMode="none"
            error="The preview ledger could not be read."
            onRetry={() => setNotice("Error preview retry stays on this page.")}
            searchable={false}
            enableColumnFilters={false}
            enableColumnVisibility={false}
            enableColumnResize={false}
          />
        </div>
      </section>
    </div>
  );
}
