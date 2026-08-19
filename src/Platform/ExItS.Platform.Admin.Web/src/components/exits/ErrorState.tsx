import { Button } from "@/components/ui/button";
import { CopyDiagnosticsButton } from "@/components/exits/CopyDiagnosticsButton";
import { usePreferences } from "@/hooks/use-preferences";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

function correlationPreview(diagnostic: DiagnosticRecord): string | undefined {
  const value = diagnostic.requestCorrelationId ?? diagnostic.serverTraceId;
  if (!value) {
    return undefined;
  }
  return value.length > 8 ? value.slice(0, 8) : value;
}

export function ErrorState({
  diagnostic,
  title,
  description,
  headingLevel = "h2",
  onRetry,
  onClose,
  onReload,
}: {
  diagnostic: DiagnosticRecord;
  title?: string;
  description?: string;
  headingLevel?: "h1" | "h2";
  onRetry?: () => void;
  onClose?: () => void;
  onReload?: () => void;
}) {
  const { t } = usePreferences();
  const correlation = correlationPreview(diagnostic);

  const HeadingTag = headingLevel;
  return (
    <section
      role="alert"
      className="max-w-xl rounded-[var(--exits-density-radius)] border border-destructive bg-surface p-[var(--exits-density-space-unit)] text-foreground"
    >
      <div className="flex items-start justify-between gap-3">
        <HeadingTag className="text-[length:var(--exits-text-lg)] font-semibold">
          {title ?? t("diagnostics.title")}
        </HeadingTag>
        <CopyDiagnosticsButton diagnostic={diagnostic} />
      </div>
      <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
        {description ?? diagnostic.message}
      </p>
      <p className="mt-3 font-mono text-[length:var(--exits-text-xs)] text-muted">
        <span className="sr-only">{t("diagnostics.reference")}: </span>
        {diagnostic.errorReference}
        {correlation ? (
          <>
            {" • "}
            <span className="sr-only">{t("diagnostics.correlation")}: </span>
            {correlation}
          </>
        ) : null}
      </p>
      {onRetry || onClose || onReload ? (
        <div className="mt-4 flex flex-wrap gap-2">
          {onRetry ? (
            <Button type="button" onClick={onRetry}>
              {t("diagnostics.retry")}
            </Button>
          ) : null}
          {onReload ? (
            <Button type="button" onClick={onReload}>
              {t("diagnostics.reload")}
            </Button>
          ) : null}
          {onClose ? (
            <Button type="button" variant="outline" onClick={onClose}>
              {t("diagnostics.close")}
            </Button>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
