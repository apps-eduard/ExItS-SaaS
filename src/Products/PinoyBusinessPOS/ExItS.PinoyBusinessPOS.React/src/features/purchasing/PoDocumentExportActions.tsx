import { ChevronDown, Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";

export type PoDocumentExportActionsProps = {
  printLabel: string;
  exportLabel: string;
  csvLabel: string;
  xlsxLabel: string;
  pdfLabel: string;
  onPrint: () => void | Promise<void>;
  onCsv: () => void | Promise<void>;
  onXlsx: () => void | Promise<void>;
  onPdf: () => void | Promise<void>;
  disabled?: boolean;
};

/**
 * Compact document utilities: Print + Export ▾ (PDF / Excel / CSV).
 * Prefer PageHeader actions slot over a large toolbar above line items.
 */
export function PoDocumentExportActions({
  printLabel,
  exportLabel,
  csvLabel,
  xlsxLabel,
  pdfLabel,
  onPrint,
  onCsv,
  onXlsx,
  onPdf,
  disabled = false,
}: PoDocumentExportActionsProps) {
  const menu = useDismissibleOpen(false);

  return (
    <div className="po-document-export flex flex-wrap items-center gap-2" data-testid="po-document-export">
      <Button
        type="button"
        variant="outline"
        shape="soft"
        disabled={disabled}
        data-testid="po-document-print"
        aria-label={printLabel}
        title={printLabel}
        onClick={() => void onPrint()}
      >
        <Printer className="size-4" aria-hidden />
        {printLabel}
      </Button>

      <DropdownMenu
        align="end"
        open={menu.open}
        onOpenChange={menu.setOpen}
        menuLabel={exportLabel}
        trigger={(triggerProps) => (
          <Button
            type="button"
            variant="outline"
            shape="soft"
            id={triggerProps.id}
            disabled={disabled}
            aria-haspopup="menu"
            aria-expanded={triggerProps.expanded}
            aria-controls={triggerProps.controls}
            aria-label={exportLabel}
            title={exportLabel}
            data-testid="po-document-export-menu"
            onClick={triggerProps.onClick}
            onKeyDown={triggerProps.onKeyDown}
          >
            {exportLabel}
            <ChevronDown className="size-3.5 opacity-70" aria-hidden />
          </Button>
        )}
      >
        <MenuItem
          disabled={disabled}
          data-testid="po-document-export-pdf"
          onSelect={() => {
            menu.setOpen(false);
            void onPdf();
          }}
        >
          {pdfLabel}
        </MenuItem>
        <MenuItem
          disabled={disabled}
          data-testid="po-document-export-xlsx"
          onSelect={() => {
            menu.setOpen(false);
            void onXlsx();
          }}
        >
          {xlsxLabel}
        </MenuItem>
        <MenuItem
          disabled={disabled}
          data-testid="po-document-export-csv"
          onSelect={() => {
            menu.setOpen(false);
            void onCsv();
          }}
        >
          {csvLabel}
        </MenuItem>
      </DropdownMenu>
    </div>
  );
}
