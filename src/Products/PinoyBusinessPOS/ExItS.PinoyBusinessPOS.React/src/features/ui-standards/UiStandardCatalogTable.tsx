import { StatusChip } from "@/components/exits/StatusChip";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import type { UiStandardCatalogRow } from "@/features/ui-standards/ui-standard-catalog";

export type UiStandardCatalogTableProps = {
  rows: ReadonlyArray<UiStandardCatalogRow>;
};

/**
 * Compact filterable index of locked/pilot standards (ExitsTable shell).
 * Complements the detailed classic Tables panel — does not replace it.
 */
export function UiStandardCatalogTable({ rows }: UiStandardCatalogTableProps) {
  if (rows.length === 0) {
    return null;
  }

  return (
    <div className="flex min-w-0 flex-col gap-2" data-testid="ui-standard-catalog-table">
      <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">Standards catalog</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        Locked and pilot standards — filter with Search / category above. Deep ExitsTable demos live
        under Detailed catalog → Tables.
      </p>
      <ExitsTableContainer>
        <ExitsTable>
          <ExitsTableHeader>
            <ExitsTableRow>
              <ExitsTableHead>Component</ExitsTableHead>
              <ExitsTableHead>Standard</ExitsTableHead>
              <ExitsTableHead>Category</ExitsTableHead>
              <ExitsTableHead>Status</ExitsTableHead>
              <ExitsTableHead>Summary</ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            {rows.map((row) => (
              <ExitsTableRow key={row.id} data-testid={`ui-standard-catalog-row-${row.id}`}>
                <ExitsTableCell className="font-medium">{row.component}</ExitsTableCell>
                <ExitsTableCell>{row.standard}</ExitsTableCell>
                <ExitsTableCell className="capitalize">{row.category}</ExitsTableCell>
                <ExitsTableCell>
                  <StatusChip tone={row.status === "Locked" ? "success" : "info"}>
                    {row.status}
                  </StatusChip>
                </ExitsTableCell>
                <ExitsTableCell>{row.summary}</ExitsTableCell>
              </ExitsTableRow>
            ))}
          </ExitsTableBody>
        </ExitsTable>
      </ExitsTableContainer>
    </div>
  );
}
