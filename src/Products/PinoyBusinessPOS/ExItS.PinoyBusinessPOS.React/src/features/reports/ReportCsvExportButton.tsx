import { useId, useState } from "react";
import { canExportData } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { Button } from "@/components/ui/button";
import { triggerReportCsvDownload, type ReportExportResult } from "@/features/reports/report-csv-export";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type ReportCsvExportButtonProps = {
  disabled?: boolean;
  onExport: (signal: AbortSignal) => Promise<ReportExportResult>;
};

export function ReportCsvExportButton({ disabled = false, onExport }: ReportCsvExportButtonProps) {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const statusId = useId();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [emptyNotice, setEmptyNotice] = useState(false);

  if (!canExportData(sessionGrant)) {
    return null;
  }

  async function handleClick() {
    if (busy) {
      return;
    }
    setError(null);
    setEmptyNotice(false);
    setBusy(true);
    const controller = new AbortController();
    try {
      const result = await onExport(controller.signal);
      if (result.rowCount === 0) {
        setEmptyNotice(true);
        return;
      }
      triggerReportCsvDownload(result);
    } catch (caught) {
      setError(describePosApiError(caught, t, "reports.export.failed"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <Button
        type="button"
        variant="outline"
        className="w-fit"
        data-testid="report-export-csv"
        disabled={disabled || busy}
        aria-busy={busy}
        aria-describedby={error || emptyNotice || busy ? statusId : undefined}
        onClick={() => void handleClick()}
      >
        {busy ? t("reports.export.preparing") : t("reports.export.csv")}
      </Button>
      {busy || error || emptyNotice ? (
        <p
          id={statusId}
          role={error ? "alert" : "status"}
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid={error ? "report-export-error" : "report-export-status"}
        >
          {error ?? (emptyNotice ? t("reports.export.noData") : t("reports.export.preparing"))}
        </p>
      ) : null}
    </div>
  );
}
