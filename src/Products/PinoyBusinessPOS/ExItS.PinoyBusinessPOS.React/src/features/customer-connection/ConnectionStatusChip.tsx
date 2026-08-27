import { Link2, Link2Off, Clock, Ban, MinusCircle, ShieldOff, AlertCircle } from "lucide-react";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  connectionStatusLabelKey,
  connectionStatusTone,
  type ConnectionRelationshipState,
} from "@/features/customer-connection/connection-state";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type ConnectionStatusChipProps = {
  state: ConnectionRelationshipState;
  audience: "personal" | "organization";
  testId?: string;
  showIcon?: boolean;
  className?: string;
};

function StatusIcon({ state }: { state: ConnectionRelationshipState }) {
  const className = "size-3.5 shrink-0";
  switch (state) {
    case "Linked":
      return <Link2 className={className} aria-hidden />;
    case "Pending":
      return <Clock className={className} aria-hidden />;
    case "Declined":
      return <MinusCircle className={className} aria-hidden />;
    case "Expired":
      return <Clock className={className} aria-hidden />;
    case "Revoked":
      return <Link2Off className={className} aria-hidden />;
    case "Blocked":
      return <Ban className={className} aria-hidden />;
    case "Unavailable":
      return <ShieldOff className={className} aria-hidden />;
    case "NotLinked":
    default:
      return <AlertCircle className={className} aria-hidden />;
  }
}

export function ConnectionStatusChip({
  state,
  audience,
  testId = "connection-status-chip",
  showIcon = true,
  className,
}: ConnectionStatusChipProps) {
  const { t } = useI18n();
  const label = t(connectionStatusLabelKey(state, audience));

  return (
    <span data-testid={testId} className={cn("inline-flex items-center", className)}>
      <StatusChip tone={connectionStatusTone(state, audience)}>
        <span className="inline-flex items-center gap-1.5">
          {showIcon ? <StatusIcon state={state} /> : null}
          <span>{label}</span>
        </span>
      </StatusChip>
    </span>
  );
}
