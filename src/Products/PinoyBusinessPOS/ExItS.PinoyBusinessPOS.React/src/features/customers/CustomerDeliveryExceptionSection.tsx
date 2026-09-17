import { Truck } from "lucide-react";
import { BranchFulfillmentSwitch } from "@/features/branches/BranchFulfillmentSwitch";
import type { MessageKey } from "@/i18n/messages";

type CustomerDeliveryExceptionSectionProps = {
  allowBeyond: boolean;
  canEdit: boolean;
  pending: boolean;
  t: (key: MessageKey) => string;
  onToggle: (next: boolean) => void;
};

/**
 * Seller-managed org-customer delivery distance exception.
 * Does not bypass service area, entitlement, readiness, or fee rules.
 * Renders as a Store customer details overview card item.
 */
export function CustomerDeliveryExceptionSection({
  allowBeyond,
  canEdit,
  pending,
  t,
  onToggle,
}: CustomerDeliveryExceptionSectionProps) {
  return (
    <div className="branch-mgmt-overview__item" data-testid="customer-delivery-section">
      <dt>
        <Truck className="branch-mgmt-overview__icon branch-mgmt-overview__icon--primary" aria-hidden />
        {t("customers.delivery.title")}
      </dt>
      <dd className="flex flex-col gap-2">
        <BranchFulfillmentSwitch
          checked={allowBeyond}
          disabled={!canEdit}
          pending={pending}
          label={t("customers.delivery.allowBeyond")}
          hint={
            allowBeyond
              ? t("customers.delivery.exceptionHint")
              : t("customers.delivery.normalHint")
          }
          testId="customer-delivery-distance-exception"
          onCheckedChange={onToggle}
        />
      </dd>
    </div>
  );
}
