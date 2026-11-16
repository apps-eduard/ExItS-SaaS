import type { ReactNode } from "react";
import type { MenuItem } from "../menu/DropdownMenu";

export type SortDirection = "asc" | "desc";

export type ColumnSort = {
  id: string;
  direction: SortDirection;
};

export type SortType = "text" | "number" | "date";

export type DataTableDensity = "compact" | "standard" | "large";

export type DataTableSelectionMode = "none" | "single" | "multiple";

export type DataTableColumn<T> = {
  id: string;
  header: string;
  accessor: (row: T) => string | number | Date | null | undefined;
  /** Text used by global search. Defaults to the accessor. */
  searchValue?: (row: T) => string;
  /** Text used by the column filter. Defaults to the accessor. */
  filterValue?: (row: T) => string;
  cell?: (row: T) => ReactNode;
  /** Value written to CSV and Excel. Defaults to the accessor. */
  exportValue?: (row: T) => string | number | Date | null | undefined;
  sortType?: SortType;
  align?: "start" | "end";
  width?: number;
  minWidth?: number;
  sortable?: boolean;
  filterable?: boolean;
  hideable?: boolean;
  /** Keep the column stuck to the inline start while the table scrolls. */
  pin?: "start";
  wrap?: boolean;
};

export type DataTableFilter = {
  id: string;
  label: string;
  columnId: string;
  options: ReadonlyArray<{ value: string; label: string }>;
};

export type DataTableRowAction<T> = (row: T) => MenuItem[];
