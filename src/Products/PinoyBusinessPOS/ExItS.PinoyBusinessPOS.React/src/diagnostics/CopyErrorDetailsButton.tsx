import { useCallback, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { copyTextToClipboard } from "@/diagnostics/copy-text-to-clipboard";
import { formatPosErrorReport, type PosErrorReportInput } from "@/diagnostics/pos-error-report";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export function CopyErrorDetailsButton({
  report,
  className,
}: {
  report: PosErrorReportInput;
  className?: string;
}) {
  const { t } = useI18n();
  const text = useMemo(() => formatPosErrorReport(report), [report]);
  const [copyState, setCopyState] = useState<"idle" | "copied" | "failed">("idle");
  const [showTechnical, setShowTechnical] = useState(false);

  const copyReport = useCallback(async () => {
    const copied = await copyTextToClipboard(text);
    setCopyState(copied ? "copied" : "failed");
  }, [text]);

  return (
    <div className={cn("flex min-w-0 flex-col gap-2", className)}>
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="ghost"
          data-testid="copy-error-details"
          onClick={() => void copyReport()}
        >
          {copyState === "copied"
            ? t("diagnostics.copied")
            : copyState === "failed"
              ? t("diagnostics.copyFailed")
              : t("diagnostics.copy")}
        </Button>
        <Button
          type="button"
          variant="ghost"
          data-testid="toggle-technical-details"
          aria-expanded={showTechnical}
          onClick={() => setShowTechnical((current) => !current)}
        >
          {t("diagnostics.technicalDetails")}
        </Button>
      </div>
      {copyState === "failed" ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("diagnostics.copyFallbackHint")}
        </p>
      ) : null}
      {showTechnical ? (
        <textarea
          readOnly
          data-testid="technical-error-details"
          className="min-h-[12rem] w-full resize-y rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3 font-mono text-[length:var(--exits-text-xs)] leading-relaxed text-foreground"
          value={text}
          onFocus={(event) => event.currentTarget.select()}
        />
      ) : null}
    </div>
  );
}
