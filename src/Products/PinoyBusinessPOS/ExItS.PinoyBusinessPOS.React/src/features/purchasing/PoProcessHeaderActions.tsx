import type { ReactNode } from "react";
import { Eye, History } from "lucide-react";
import { StatusChip, type StatusChipTone } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { PoDocumentExportActions } from "@/features/purchasing/PoDocumentExportActions";
import { useI18n } from "@/i18n/I18nProvider";

export type PoProcessHeaderActionsProps = {
  statusLabel: string;
  statusTone: StatusChipTone;
  onTimeline: () => void;
  onPreview: () => void;
  onPrint: () => void | Promise<void>;
  onCsv: () => void | Promise<void>;
  onXlsx: () => void | Promise<void>;
  onPdf: () => void | Promise<void>;
  /** When false, Timeline stays visible but disabled. Default true. */
  timelineEnabled?: boolean;
  /** When false, Preview stays visible but disabled. Default true. */
  previewEnabled?: boolean;
  /** When false, Print/Export stay visible but disabled. Default true. */
  exportEnabled?: boolean;
  /** Optional trailing chip/note (e.g. outstanding qty). */
  trailing?: ReactNode;
  timelineTestId?: string;
  previewTestId?: string;
};

/**
 * Canonical PO process header utilities for buyer and seller:
 * Status · Timeline · Preview · Print · Export ▾
 * Always present; disable individual actions when unavailable.
 */
export function PoProcessHeaderActions({
  statusLabel,
  statusTone,
  onTimeline,
  onPreview,
  onPrint,
  onCsv,
  onXlsx,
  onPdf,
  timelineEnabled = true,
  previewEnabled = true,
  exportEnabled = true,
  trailing,
  timelineTestId = "po-timeline-open",
  previewTestId = "po-document-preview-open",
}: PoProcessHeaderActionsProps) {
  const { t } = useI18n();

  return (
    <div className="flex flex-wrap items-center gap-2" data-testid="po-process-header-actions">
      <StatusChip tone={statusTone}>{statusLabel}</StatusChip>
      {trailing}
      <Button
        type="button"
        intent="neutral"
        appearance="outline"
        shape="soft"
        disabled={!timelineEnabled}
        onClick={onTimeline}
        data-testid={timelineTestId}
      >
        <History className="size-4 shrink-0" aria-hidden />
        {t("purchasing.timeline")}
      </Button>
      <Button
        type="button"
        intent="neutral"
        appearance="outline"
        shape="soft"
        disabled={!previewEnabled}
        onClick={onPreview}
        data-testid={previewTestId}
      >
        <Eye className="size-4 shrink-0" aria-hidden />
        {t("summary.preview")}
      </Button>
      <PoDocumentExportActions
        printLabel={t("exitsTable.print")}
        exportLabel={t("purchasing.export")}
        csvLabel={t("exitsTable.exportCsv")}
        xlsxLabel={t("exitsTable.exportExcel")}
        pdfLabel={t("exitsTable.exportPdf")}
        disabled={!exportEnabled}
        onPrint={onPrint}
        onCsv={onCsv}
        onXlsx={onXlsx}
        onPdf={onPdf}
      />
    </div>
  );
}
