import type { ReactNode } from "react";
import { Link2, Package, Truck } from "lucide-react";
import { StatusChip } from "@/components/exits/StatusChip";
import { ConnectionStatusChip } from "@/features/customer-connection/ConnectionStatusChip";
import {
  MerchantOrderingBadge,
  storeDisplayInitial,
} from "@/features/customer-ordering/personal-commerce-ui";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type HeadingLevel = "h1" | "h2" | "h3";

export type PersonalStoreIdentityProps = {
  storeName: string;
  relationshipLabel?: string | null;
  canCustomerOrder: boolean;
  orderingPending?: boolean;
  headingLevel?: HeadingLevel;
  headingId?: string;
  connectionTestId?: string;
  /**
   * Soft Glass store card badge layout:
   * row 1 = Connected + Delivery, row 2 = Linked date + Ordering available.
   */
  emphasisBadges?: boolean;
  linkedAtLabel?: string | null;
  fulfillment?: "delivery" | "pickup" | null;
  extra?: ReactNode;
};

export function PersonalStoreIdentity({
  storeName,
  relationshipLabel,
  canCustomerOrder,
  orderingPending = false,
  headingLevel = "h2",
  headingId,
  connectionTestId = "personal-store-identity-connection",
  emphasisBadges = false,
  linkedAtLabel = null,
  fulfillment = null,
  extra,
}: PersonalStoreIdentityProps) {
  const { t } = useI18n();
  const Heading = headingLevel;
  const name = storeName.trim() || "?";

  return (
    <div className="pc-store-card__top" data-testid="personal-store-identity">
      <span className="pc-store-card__avatar" aria-hidden>
        {storeDisplayInitial(name)}
      </span>
      <div className="pc-store-card__body">
        <Heading id={headingId} className="pc-store-card__name">
          {name}
        </Heading>
        {relationshipLabel ? (
          <p className="pc-store-card__linked-as m-0">
            <Link2 className="pc-store-card__link-icon size-3.5 shrink-0" aria-hidden />
            <span>{t("personal.merchants.linkedAs").replace("{name}", relationshipLabel)}</span>
          </p>
        ) : null}
        {emphasisBadges ? (
          <div
            className="pc-store-card__badge-rows"
            data-testid="personal-store-identity-badges"
          >
            <div className="pc-store-card__badge-row pc-store-card__badge-row--pair">
              <ConnectionStatusChip
                state="Linked"
                audience="personal"
                appearance="emphasis"
                className="pc-store-card__status"
                testId={connectionTestId}
              />
              {fulfillment ? (
                <StatusChip
                  tone="secondary"
                  appearance="emphasis"
                  shape="pill"
                  icon={
                    fulfillment === "delivery" ? (
                      <Truck className="size-3.5 shrink-0" aria-hidden />
                    ) : (
                      <Package className="size-3.5 shrink-0" aria-hidden />
                    )
                  }
                  className="pc-store-card__status pc-store-card__meta-chip"
                  data-testid="personal-store-fulfillment-chip"
                >
                  {fulfillment === "delivery" ? t("orders.delivery") : t("orders.pickup")}
                </StatusChip>
              ) : (
                <span className="pc-store-card__badge-spacer" aria-hidden />
              )}
            </div>
            <div className="pc-store-card__badge-row pc-store-card__badge-row--pair">
              {linkedAtLabel ? (
                <StatusChip
                  tone="info"
                  appearance="emphasis"
                  shape="pill"
                  className="pc-store-card__status pc-store-card__meta-chip"
                  data-testid="personal-store-linked-chip"
                >
                  {linkedAtLabel}
                </StatusChip>
              ) : (
                <span className="pc-store-card__badge-spacer" aria-hidden />
              )}
              {orderingPending ? (
                <MerchantOrderingBadge available={canCustomerOrder} pending />
              ) : (
                <StatusChip
                  tone={canCustomerOrder ? "success" : "warning"}
                  appearance="emphasis"
                  shape="pill"
                  className="pc-store-card__status pc-store-card__ordering-chip"
                  data-testid="personal-store-ordering-chip"
                >
                  {canCustomerOrder
                    ? t("personal.orderingAvailable")
                    : t("personal.orderingUnavailable")}
                </StatusChip>
              )}
            </div>
          </div>
        ) : (
          <div className="pc-store-card__badge-row">
            <ConnectionStatusChip
              state="Linked"
              audience="personal"
              appearance="soft"
              className="pc-store-card__status"
              testId={connectionTestId}
            />
            <MerchantOrderingBadge available={canCustomerOrder} pending={orderingPending} />
          </div>
        )}
        {extra}
      </div>
    </div>
  );
}

export function PersonalStoreIdentityCard({
  className,
  children,
  testId,
  ...identity
}: PersonalStoreIdentityProps & {
  className?: string;
  children?: ReactNode;
  testId?: string;
}) {
  return (
    <section
      className={cn("pc-store-card pc-store-card--static exits-animate-panel", className)}
      data-testid={testId}
    >
      <PersonalStoreIdentity {...identity} />
      {children}
    </section>
  );
}
