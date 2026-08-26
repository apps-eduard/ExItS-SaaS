import { useMemo } from "react";
import { RefreshCw, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { CopyErrorDetailsButton } from "@/diagnostics/CopyErrorDetailsButton";
import {
  formatClientErrorReport,
  type ClientErrorReportInput,
} from "@/diagnostics/client-error-report";
import { safeDiagnosticError } from "@/diagnostics/diagnostic-redaction";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type ClientErrorPanelProps = {
  input: ClientErrorReportInput;
  onReload?: () => void;
  onDismiss?: () => void;
  className?: string;
};

export function ClientErrorPanel({ input, onReload, onDismiss, className }: ClientErrorPanelProps) {
  const { t } = useI18n();
  const report = useMemo(() => formatClientErrorReport(input), [input]);
  const summary = useMemo(
    () => input.friendlyMessage ?? safeDiagnosticError(input.error).message,
    [input.error, input.friendlyMessage],
  );

  return (
    <div
      role="alert"
      data-testid="client-error-panel"
      className={cn(
        "mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] border border-destructive bg-surface p-4 text-foreground",
        className,
      )}
    >
      <div>
        <h1 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {t("diagnostics.globalTitle")}
        </h1>
        <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("diagnostics.globalHint")}
        </p>
      </div>

      <p className="m-0 rounded-[var(--exits-radius-md)] bg-[var(--exits-surface-muted)] px-3 py-2 text-[length:var(--exits-text-sm)] font-medium wrap-break-word">
        {summary}
      </p>

      <CopyErrorDetailsButton report={{ ...input, friendlyMessage: summary }} />

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
          {t("diagnostics.copyableReport")}
        </span>
        <textarea
          readOnly
          data-testid="client-error-report"
          className="min-h-[14rem] w-full resize-y rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3 font-mono text-[length:var(--exits-text-xs)] leading-relaxed text-foreground"
          value={report}
          onFocus={(event) => event.currentTarget.select()}
        />
      </label>

      <div className="flex flex-wrap gap-2">
        {onReload ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            data-testid="client-error-reload"
            onClick={onReload}
          >
            <RefreshCw className="size-4 shrink-0" aria-hidden />
            {t("diagnostics.reload")}
          </Button>
        ) : null}
        {onDismiss ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            data-testid="client-error-dismiss"
            onClick={onDismiss}
          >
            <X className="size-4 shrink-0" aria-hidden />
            {t("diagnostics.dismiss")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
