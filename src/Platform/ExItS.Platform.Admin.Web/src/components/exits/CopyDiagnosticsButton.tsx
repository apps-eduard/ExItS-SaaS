import { useEffect, useMemo, useState } from "react";
import { Check, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/use-preferences";
import {
  copyDiagnosticReport,
  formatDiagnosticForClipboard,
} from "@/lib/diagnostics/copy-diagnostic-text";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type CopyState = "idle" | "copied" | "failed";

export function CopyDiagnosticsButton({ diagnostic }: { diagnostic: DiagnosticRecord }) {
  const { t } = usePreferences();
  const [state, setState] = useState<CopyState>("idle");
  const reportText = useMemo(() => formatDiagnosticForClipboard(diagnostic), [diagnostic]);

  useEffect(() => {
    if (state !== "copied") {
      return;
    }
    const timer = window.setTimeout(() => setState("idle"), 1600);
    return () => window.clearTimeout(timer);
  }, [state]);

  async function onCopy() {
    const copied = await copyDiagnosticReport(diagnostic);
    setState(copied ? "copied" : "failed");
  }

  return (
    <div className="flex w-full flex-col items-stretch gap-1 sm:items-end">
      <Button
        type="button"
        variant="ghost"
        size="sm"
        aria-label={t("diagnostics.copy")}
        onClick={() => void onCopy()}
      >
        {state === "copied" ? (
          <Check aria-hidden="true" size={18} />
        ) : (
          <Copy aria-hidden="true" size={18} />
        )}
        <span>{state === "copied" ? t("diagnostics.copied") : t("diagnostics.copy")}</span>
      </Button>
      {state === "failed" ? (
        <>
          <p className="text-[length:var(--exits-text-xs)] text-destructive">
            {t("diagnostics.copyFailed")}
          </p>
          <label className="grid w-full gap-1">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("diagnostics.copyFallbackHint")}
            </span>
            <textarea
              readOnly
              aria-label={t("diagnostics.copyFallbackHint")}
              className="min-h-32 w-full rounded-[var(--exits-density-radius)] border border-input bg-surface px-2 py-1 font-mono text-[length:var(--exits-text-xs)]"
              value={reportText}
              onFocus={(event) => event.currentTarget.select()}
            />
          </label>
        </>
      ) : null}
    </div>
  );
}
