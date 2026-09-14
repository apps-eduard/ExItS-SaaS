import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { ChevronRight } from "lucide-react";
import { StatusChip, type StatusChipTone } from "@/components/exits/StatusChip";
import {
  resolveCustomerListConnectionBadge,
  type CustomerListConnectionInput,
  type CustomerListConnectionOverlay,
} from "@/features/customers/customer-list-connection";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

export type CustomerListCardKind = "personal" | "b2b" | "local";

/** Relationship / connection state — never merge with account status. */
export type CustomerListRelationshipStatus =
  | "Pending"
  | "Connected"
  | "Declined"
  | "Inactive";

/** Abnormal account status only — Active is intentionally omitted from the card. */
export type CustomerListAbnormalAccountStatus = "Suspended" | "Disabled";

export type CustomerListCardProps = {
  href: string;
  testId: string;
  name: string;
  nameTestId?: string;
  kind: CustomerListCardKind;
  relationshipStatus: CustomerListRelationshipStatus;
  /** Raw account/membership status (Active, Suspended, Disabled, …). Active is not shown. */
  accountStatus?: string | null;
  exitsId: string | null;
  exitsIdTestId?: string;
  /** Incoming connection needs cashier action (B2B). */
  actionRequired?: boolean;
  extraKindBadges?: ReactNode;
  className?: string;
};

export function resolveAbnormalAccountStatus(
  accountStatus: string | null | undefined,
): CustomerListAbnormalAccountStatus | null {
  switch ((accountStatus ?? "").trim().toLowerCase()) {
    case "suspended":
      return "Suspended";
    case "disabled":
      return "Disabled";
    default:
      return null;
  }
}

export function relationshipStatusLabelKey(
  status: CustomerListRelationshipStatus,
): MessageKey {
  switch (status) {
    case "Pending":
      return "customers.listCard.relationship.Pending";
    case "Connected":
      return "customers.listCard.relationship.Connected";
    case "Declined":
      return "customers.listCard.relationship.Declined";
    case "Inactive":
      return "customers.listCard.relationship.Inactive";
  }
}

export function relationshipStatusTone(
  status: CustomerListRelationshipStatus,
): StatusChipTone {
  switch (status) {
    case "Pending":
      return "warning";
    case "Connected":
      return "success";
    case "Declined":
      return "danger";
    case "Inactive":
      return "neutral";
  }
}

export function abnormalAccountStatusLabelKey(
  status: CustomerListAbnormalAccountStatus,
): MessageKey {
  return status === "Suspended"
    ? "customers.listCard.account.Suspended"
    : "customers.listCard.account.Disabled";
}

export function abnormalAccountStatusTone(
  status: CustomerListAbnormalAccountStatus,
): StatusChipTone {
  return status === "Suspended" ? "warning" : "danger";
}

function kindLabelKey(kind: CustomerListCardKind): MessageKey {
  switch (kind) {
    case "personal":
      return "customers.badge.personal";
    case "b2b":
      return "customers.badge.b2b";
    case "local":
      return "customers.badge.local";
  }
}

function kindTone(kind: CustomerListCardKind): StatusChipTone {
  return kind === "b2b" ? "success" : kind === "personal" ? "info" : "neutral";
}

function buildHelperLines(input: {
  t: (key: MessageKey) => string;
  name: string;
  kind: CustomerListCardKind;
  relationshipStatus: CustomerListRelationshipStatus;
  abnormalAccount: CustomerListAbnormalAccountStatus | null;
  actionRequired: boolean;
}): string[] {
  const { t, name, kind, relationshipStatus, abnormalAccount, actionRequired } = input;
  const displayName = name.trim() || t("customers.title");

  if (abnormalAccount === "Suspended") {
    if (kind === "personal") {
      return [
        t("customers.listCard.personalSuspendedTitle"),
        t("customers.listCard.personalSuspendedDetail"),
      ];
    }
    return [
      t("customers.listCard.businessSuspendedTitle"),
      t("customers.listCard.businessSuspendedDetail"),
    ];
  }

  if (abnormalAccount === "Disabled") {
    if (kind === "personal") {
      return [
        t("customers.listCard.personalDisabledTitle"),
        t("customers.listCard.personalDisabledDetail"),
      ];
    }
    return [
      t("customers.listCard.businessDisabledTitle"),
      t("customers.listCard.businessDisabledDetail"),
    ];
  }

  if (relationshipStatus === "Pending") {
    if (actionRequired) {
      return [t("customers.business.actionRequired").replace("{name}", displayName)];
    }
    return [t("customers.business.waitingForAccept").replace("{name}", displayName)];
  }

  return [];
}

/**
 * Unified People / Businesses list card:
 * Body (start): name, ExItS ID, chips, helper
 * Chevron (end): vertically centered on the card
 */
export function CustomerListCard({
  href,
  testId,
  name,
  nameTestId,
  kind,
  relationshipStatus,
  accountStatus = null,
  exitsId,
  exitsIdTestId,
  actionRequired = false,
  extraKindBadges,
  className,
}: CustomerListCardProps) {
  const { t } = useI18n();
  const exitsText = exitsId?.trim() || null;
  const abnormalAccount = resolveAbnormalAccountStatus(accountStatus);
  const helperLines = buildHelperLines({
    t,
    name,
    kind,
    relationshipStatus,
    abnormalAccount,
    actionRequired,
  });

  return (
    <Link
      className={cn(
        "exits-list__card customer-row customers-card customer-list-card block min-w-0 text-foreground no-underline",
        className,
      )}
      to={href}
      data-testid={testId}
    >
      <span className="customer-list-card__body">
        <span
          className="customer-list-card__name exits-list__name truncate font-semibold"
          data-testid={nameTestId}
        >
          {name}
        </span>

        <span
          className="customer-list-card__exits-id"
          data-testid={exitsIdTestId ?? "customer-list-card-exits-id"}
        >
          {exitsText ?? t("checkout.directoryCredit.availableEmDash")}
        </span>

        <span className="customer-list-card__chips" data-testid="customer-list-card-chips">
          <StatusChip tone={kindTone(kind)}>{t(kindLabelKey(kind))}</StatusChip>
          <span
            className="customer-list-card__relationship"
            data-testid="customer-list-card-connection"
          >
            <StatusChip tone={relationshipStatusTone(relationshipStatus)}>
              {t(relationshipStatusLabelKey(relationshipStatus))}
            </StatusChip>
          </span>
          {abnormalAccount ? (
            <span
              className="customer-list-card__account"
              data-testid="customer-list-card-account"
            >
              <StatusChip tone={abnormalAccountStatusTone(abnormalAccount)}>
                {t(abnormalAccountStatusLabelKey(abnormalAccount))}
              </StatusChip>
            </span>
          ) : null}
          {extraKindBadges}
        </span>

        {helperLines.length > 0 ? (
          <span
            className="customer-list-card__hint"
            data-testid={
              abnormalAccount
                ? "customer-list-card-account-hint"
                : relationshipStatus === "Pending"
                  ? "customer-list-card-pending-hint"
                  : "customer-list-card-hint"
            }
          >
            {helperLines.map((line) => (
              <span key={line} className="customer-list-card__hint-line">
                {line}
              </span>
            ))}
          </span>
        ) : null}
      </span>

      <ChevronRight
        className="customer-list-card__chevron customer-row__chevron size-4 shrink-0 text-muted"
        aria-hidden
      />
    </Link>
  );
}

/**
 * Map B2B relationship field → list relationship chip.
 * Does not read or invent account Suspended/Disabled.
 */
export function resolveBusinessRelationshipStatus(
  relationshipStatus: string | null | undefined,
): CustomerListRelationshipStatus {
  switch ((relationshipStatus ?? "").trim().toLowerCase()) {
    case "pending":
      return "Pending";
    case "active":
      return "Connected";
    case "declined":
      return "Declined";
    case "disconnected":
    case "inactive":
      return "Inactive";
    default:
      return "Inactive";
  }
}

/** People list relationship from org overlay only (not ExItS ID alone). */
export function resolvePeopleRelationshipStatus(
  customer: CustomerListConnectionInput,
  overlay: CustomerListConnectionOverlay | null | undefined,
): CustomerListRelationshipStatus {
  const badge = resolveCustomerListConnectionBadge(customer, overlay);
  if (badge === "connected") {
    return "Connected";
  }
  if (badge === "pending") {
    return "Pending";
  }
  return "Inactive";
}
