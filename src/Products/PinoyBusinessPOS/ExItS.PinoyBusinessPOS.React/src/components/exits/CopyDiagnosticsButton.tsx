import { useState } from "react";
import { Button } from "@/components/ui/button";
import { buildDiagnosticReport } from "@/lib/diagnostics/build-diagnostic-report";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import { useI18n } from "@/i18n/I18nProvider";

export function CopyDiagnosticsButton({ record }: { record: DiagnosticRecord }) {
  const { t } = useI18n();
  const [copied, setCopied] = useState(false);

  async function copy(): Promise<void> {
    const text = buildDiagnosticReport(record);
    await navigator.clipboard.writeText(text);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1600);
  }

  return (
    <Button type="button" variant="outline" onClick={() => void copy()}>
      {copied ? t("diagnostics.copied") : t("diagnostics.copy")}
    </Button>
  );
}
