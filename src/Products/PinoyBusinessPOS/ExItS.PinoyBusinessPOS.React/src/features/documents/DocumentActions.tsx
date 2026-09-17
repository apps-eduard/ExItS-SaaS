import { Eye, FileDown, Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  exportBusinessDocumentPdf,
  printBusinessDocument,
} from "@/features/documents/print-business-document";

export function DocumentActions({
  previewLabel = "Preview",
  printLabel = "Print",
  pdfLabel = "PDF / Export",
  onPreview,
  showPdf = true,
  disabled = false,
  testId = "document-actions",
}: {
  previewLabel?: string;
  printLabel?: string;
  pdfLabel?: string;
  onPreview?: () => void;
  showPdf?: boolean;
  disabled?: boolean;
  testId?: string;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2 print:hidden" data-testid={testId}>
      {onPreview ? (
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          data-testid={`${testId}-preview`}
          onClick={onPreview}
        >
          <Eye className="size-4" aria-hidden />
          {previewLabel}
        </Button>
      ) : null}
      <Button
        type="button"
        variant="outline"
        disabled={disabled}
        data-testid={`${testId}-print`}
        onClick={() => printBusinessDocument()}
      >
        <Printer className="size-4" aria-hidden />
        {printLabel}
      </Button>
      {showPdf ? (
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          data-testid={`${testId}-pdf`}
          onClick={() => exportBusinessDocumentPdf()}
        >
          <FileDown className="size-4" aria-hidden />
          {pdfLabel}
        </Button>
      ) : null}
    </div>
  );
}
