import { BadgeCheck, Clock, Link2, UserRound } from "lucide-react";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  resolveCustomerListConnectionBadge,
  type CustomerListConnectionInput,
  type CustomerListConnectionOverlay,
} from "@/features/customers/customer-list-connection";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

const ICON_CLASS = "size-3 shrink-0";

type BadgeSpec = {
  kind: "no-exits" | "exits-id" | "connected" | "pending";
  tone: "info" | "success";
  icon: typeof UserRound;
  labelKey: MessageKey;
};

function badgesFor(
  kind: ReturnType<typeof resolveCustomerListConnectionBadge>,
): BadgeSpec[] {
  if (kind === "no-exits") {
    return [
      {
        kind: "no-exits",
        tone: "info",
        icon: UserRound,
        labelKey: "customers.listBadge.noExItsId",
      },
    ];
  }

  const exits: BadgeSpec = {
    kind: "exits-id",
    tone: "success",
    icon: BadgeCheck,
    labelKey: "customers.listBadge.exItsId",
  };

  if (kind === "connected") {
    return [
      exits,
      {
        kind: "connected",
        tone: "success",
        icon: Link2,
        labelKey: "customers.listBadge.connected",
      },
    ];
  }

  if (kind === "pending") {
    return [
      exits,
      {
        kind: "pending",
        tone: "info",
        icon: Clock,
        labelKey: "customers.listBadge.pending",
      },
    ];
  }

  return [exits];
}

type CustomerListConnectionBadgesProps = {
  customer: CustomerListConnectionInput;
  overlay?: CustomerListConnectionOverlay | null;
  className?: string;
};

export function CustomerListConnectionBadges({
  customer,
  overlay = null,
  className,
}: CustomerListConnectionBadgesProps) {
  const { t } = useI18n();
  const kind = resolveCustomerListConnectionBadge(customer, overlay);
  const badges = badgesFor(kind);

  return (
    <span className={cn("customer-list-badges", className)}>
      {badges.map((badge) => {
        const Icon = badge.icon;
        return (
          <StatusChip key={badge.kind} tone={badge.tone}>
            <span
              className="customer-list-badge"
              data-testid={`customer-list-badge-${badge.kind}`}
            >
              <Icon className={ICON_CLASS} aria-hidden />
              {t(badge.labelKey)}
            </span>
          </StatusChip>
        );
      })}
    </span>
  );
}
