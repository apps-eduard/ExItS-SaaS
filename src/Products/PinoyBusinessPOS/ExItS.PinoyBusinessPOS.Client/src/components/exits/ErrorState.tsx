import { AlertCircle } from "lucide-react";
import type { ReactNode } from "react";
import { CopyDiagnosticsButton } from "@/components/exits/CopyDiagnosticsButton";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

export function ErrorState({
  title,
  body,
  record,
  action,
}: {
  title: string;
  body: string;
  record: DiagnosticRecord;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-start gap-3" role="alert">
      <span className="flex size-10 items-center justify-center rounded-full bg-[var(--exits-danger-bg)] text-destructive">
        <AlertCircle className="size-5" aria-hidden="true" />
      </span>
      <div>
        <h3 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">{title}</h3>
        <p className="m-0 mt-1 text-muted">{body}</p>
        <p className="m-0 mt-2 font-semibold text-muted">
          {record.errorReference}
          {record.requestCorrelationId ? ` • ${record.requestCorrelationId}` : ""}
        </p>
      </div>
      <div className="flex flex-wrap gap-2">
        <CopyDiagnosticsButton record={record} />
        {action}
      </div>
    </div>
  );
}
