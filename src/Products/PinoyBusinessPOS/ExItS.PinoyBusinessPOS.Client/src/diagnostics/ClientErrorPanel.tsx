import { useCallback, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  formatClientErrorReport,
  type ClientErrorReportInput,
} from "@/diagnostics/client-error-report";
import { cn } from "@/lib/cn";

export type ClientErrorPanelProps = {
  input: ClientErrorReportInput;
  onReload?: () => void;
  onDismiss?: () => void;
  className?: string;
};

export function ClientErrorPanel({ input, onReload, onDismiss, className }: ClientErrorPanelProps) {
  const report = formatClientErrorReport(input);
  const [copyState, setCopyState] = useState<"idle" | "copied" | "failed">("idle");

  const copyReport = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(report);
      setCopyState("copied");
    } catch {
      try {
        const area = document.createElement("textarea");
        area.value = report;
        area.setAttribute("readonly", "");
        area.style.position = "fixed";
        area.style.left = "-9999px";
        document.body.appendChild(area);
        area.select();
        document.execCommand("copy");
        document.body.removeChild(area);
        setCopyState("copied");
      } catch {
        setCopyState("failed");
      }
    }
  }, [report]);

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
          Something went wrong
        </h1>
        <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          Copy the report below and paste it into Cursor chat so the AI can track and fix it.
        </p>
      </div>

      <p className="m-0 rounded-[var(--exits-radius-md)] bg-[var(--exits-surface-muted)] px-3 py-2 text-[length:var(--exits-text-sm)] font-medium wrap-break-word">
        {input.error instanceof Error ? input.error.message : String(input.error)}
      </p>

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
          Copyable AI report
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
        <Button
          type="button"
          className="min-h-11"
          data-testid="client-error-copy"
          onClick={() => void copyReport()}
        >
          {copyState === "copied"
            ? "Copied"
            : copyState === "failed"
              ? "Copy failed — select text"
              : "Copy report"}
        </Button>
        {onReload ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            data-testid="client-error-reload"
            onClick={onReload}
          >
            Reload
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
            Dismiss
          </Button>
        ) : null}
      </div>
    </div>
  );
}
