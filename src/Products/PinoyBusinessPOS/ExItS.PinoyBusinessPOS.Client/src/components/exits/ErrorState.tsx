import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";
import { CopyErrorDetailsButton } from "@/diagnostics/CopyErrorDetailsButton";

export function ErrorState({
  title,
  detail,
  diagnostic,
}: {
  title: string;
  detail: string;
  diagnostic?: PosErrorReportInput;
}) {
  return (
    <div
      role="alert"
      className="flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-destructive px-4 py-4"
      data-testid="error-state"
    >
      <div className="flex flex-col gap-1">
        <p className="m-0 font-semibold">{title}</p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{detail}</p>
      </div>
      {diagnostic ? <CopyErrorDetailsButton report={diagnostic} /> : null}
    </div>
  );
}
