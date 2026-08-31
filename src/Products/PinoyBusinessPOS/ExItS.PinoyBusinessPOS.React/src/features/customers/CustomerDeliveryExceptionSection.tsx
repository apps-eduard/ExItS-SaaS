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
 */
export function CustomerDeliveryExceptionSection({
  allowBeyond,
  canEdit,
  pending,
  t,
  onToggle,
}: CustomerDeliveryExceptionSectionProps) {
  return (
    <section
      className="catalog-form-section exits-animate-panel gap-3"
      data-testid="customer-delivery-section"
    >
      <h2 className="catalog-form-section__title">{t("customers.delivery.title")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {allowBeyond
          ? t("customers.delivery.exceptionOnLede")
          : t("customers.delivery.normalLede")}
      </p>
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
      {allowBeyond ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="customer-delivery-exception-status"
        >
          {t("customers.delivery.distanceExceptionBadge")}
        </p>
      ) : null}
    </section>
  );
}
