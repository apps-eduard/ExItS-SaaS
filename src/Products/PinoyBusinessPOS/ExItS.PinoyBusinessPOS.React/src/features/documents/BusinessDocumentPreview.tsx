import { useEffect, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { FileDown, Printer, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  exportBusinessDocumentPdf,
  printBusinessDocument,
} from "@/features/documents/print-business-document";
import { cn } from "@/lib/cn";
import { useBodyScrollLock } from "@/lib/use-body-scroll-lock";

/**
 * Large centered (desktop) / full-screen (mobile) preview for the canonical
 * BusinessDocument renderer. Print and PDF use the same mounted document root.
 */
export function BusinessDocumentPreview({
  open,
  onClose,
  title,
  children,
  closeLabel = "Close",
  printLabel = "Print",
  pdfLabel = "PDF / Export",
  showPdf = true,
  testId = "business-document-preview",
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  closeLabel?: string;
  printLabel?: string;
  pdfLabel?: string;
  showPdf?: boolean;
  testId?: string;
}) {
  useBodyScrollLock(open);

  useEffect(() => {
    if (!open) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
      }
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  if (!open || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <div
      className="exits-bizdoc-preview"
      data-testid={testId}
      role="presentation"
    >
      <div
        className="exits-bizdoc-preview__backdrop"
        role="presentation"
        data-testid={`${testId}-backdrop`}
        onClick={onClose}
      />
      <div
        className="exits-bizdoc-preview__panel"
        role="dialog"
        aria-modal="true"
        aria-label={title}
        data-testid={`${testId}-panel`}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="exits-bizdoc-preview__toolbar" data-testid={`${testId}-toolbar`}>
          <Button
            type="button"
            variant="ghost"
            className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
            data-testid={`${testId}-close`}
            onClick={onClose}
          >
            <X className="size-4 shrink-0" aria-hidden />
            {closeLabel}
          </Button>
          <div className="exits-bizdoc-preview__toolbar-actions">
            <Button
              type="button"
              variant="outline"
              className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
              data-testid={`${testId}-print`}
              onClick={() => printBusinessDocument()}
            >
              <Printer className="size-4 shrink-0" aria-hidden />
              {printLabel}
            </Button>
            {showPdf ? (
              <Button
                type="button"
                variant="outline"
                className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
                data-testid={`${testId}-pdf`}
                onClick={() => exportBusinessDocumentPdf()}
              >
                <FileDown className="size-4 shrink-0" aria-hidden />
                {pdfLabel}
              </Button>
            ) : null}
          </div>
        </div>
        <div
          className={cn("exits-bizdoc-preview__canvas")}
          data-testid={`${testId}-canvas`}
        >
          {children}
        </div>
      </div>
    </div>,
    document.body,
  );
}
