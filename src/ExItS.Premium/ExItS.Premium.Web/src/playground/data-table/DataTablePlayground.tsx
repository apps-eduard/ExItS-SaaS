import { DataTableScreenPreview } from "./DataTableScreenPreview";
import styles from "./DataTablePlayground.module.scss";

export function DataTablePlayground() {
  return (
    <div className={styles.page}>
      <header className={styles.intro}>
        <h1 className="type-page">Data Table</h1>
        <p className="type-secondary">
          Desktop ledger for transactions. Sort one column, or hold Shift to sort another. Search, column filters, and the category and status
          filters combine. Selection follows the record when the order changes. Resize a column from its edge, or hide columns and restore them.
          Export writes the visible columns for the current page, the full filtered list, or the selected records. Screen Preview loads that ledger
          in a real viewport.
        </p>
      </header>
      <DataTableScreenPreview />
    </div>
  );
}
