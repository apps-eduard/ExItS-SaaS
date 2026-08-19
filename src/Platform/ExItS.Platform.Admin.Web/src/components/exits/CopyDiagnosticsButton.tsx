import { useEffect, useState } from "react";
import { Check, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/use-preferences";
import { copyDiagnosticReport } from "@/lib/diagnostics/copy-diagnostic-text";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type CopyState = "idle" | "copied" | "failed";

export function CopyDiagnosticsButton({ diagnostic }: { diagnostic: DiagnosticRecord }) {
  const { t } = usePreferences();
  const [state, setState] = useState<CopyState>("idle");

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
    <div className="flex flex-col items-end gap-1">
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
        <p className="text-[length:var(--exits-text-xs)] text-destructive">
          {t("diagnostics.copyFailed")}
        </p>
      ) : null}
    </div>
  );
}
