import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { StatusChip } from "@/components/exits/StatusChip";
import { BranchFulfillmentSwitch } from "@/features/branches/BranchFulfillmentSwitch";
import { OFFER_DELIVERY_SETTINGS_PATH } from "@/features/branches/offer-delivery-queries";
import type { MessageKey } from "@/i18n/messages";

export type OrgOfferDeliveryCardProps = {
  offerDelivery: boolean;
  canEdit: boolean;
  pending?: boolean;
  loading?: boolean;
  t: (key: MessageKey) => string;
  onCheckedChange: (next: boolean) => void;
  /** Optional compact layout for page header area. */
  compact?: boolean;
};

/**
 * Compact organization-level Offer Delivery control.
 * Edits canonical OrganizationFulfillmentSettings.OfferDelivery only.
 */
export function OrgOfferDeliveryCard({
  offerDelivery,
  canEdit,
  pending = false,
  loading = false,
  t,
  onCheckedChange,
  compact = false,
}: OrgOfferDeliveryCardProps) {
  return (
    <Card
      className={compact ? "flex flex-col gap-2 p-3" : "flex flex-col gap-3 p-3"}
      data-testid="org-offer-delivery-card"
      data-offer-delivery={offerDelivery ? "on" : "off"}
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0 flex-1">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("branches.connectedOrderDeliveryTitle")}
          </h2>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {offerDelivery
              ? t("branches.offerDeliveryOnDetail")
              : t("branches.offerDeliveryOffDetail")}
          </p>
        </div>
        <StatusChip
          tone={offerDelivery ? "success" : "warning"}
          appearance="outline"
          data-testid="org-offer-delivery-status"
        >
          {offerDelivery ? t("branches.offerDeliveryStatusOn") : t("branches.offerDeliveryStatusOff")}
        </StatusChip>
      </div>

      {canEdit ? (
        <BranchFulfillmentSwitch
          checked={offerDelivery}
          disabled={loading || pending}
          pending={pending}
          label={t("branches.offerDelivery")}
          hint={
            offerDelivery
              ? t("branches.offerDeliveryOnHint")
              : t("branches.offerDeliveryOffHint")
          }
          testId="org-offer-delivery-switch"
          onCheckedChange={onCheckedChange}
        />
      ) : (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="org-offer-delivery-readonly">
          {t("branches.offerDelivery")}
          {": "}
          {offerDelivery ? t("branches.offerDeliveryStatusOn") : t("branches.offerDeliveryStatusOff")}
        </p>
      )}

      {!offerDelivery && canEdit ? (
        <Button
          type="button"
          variant="outline"
          disabled={pending || loading}
          data-testid="org-offer-delivery-turn-on"
          onClick={() => onCheckedChange(true)}
        >
          {t("branches.offerDeliveryTurnOn")}
        </Button>
      ) : null}

      <Link
        to={OFFER_DELIVERY_SETTINGS_PATH}
        className="text-[length:var(--exits-text-sm)] font-medium underline underline-offset-2"
        data-testid="org-offer-delivery-open-settings"
      >
        {t("branches.offerDeliveryOpenConnectedCommerce")}
      </Link>
    </Card>
  );
}
