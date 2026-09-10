import { Check } from "lucide-react";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import {
  isCheckoutBusiness,
  isCheckoutBusinessDirectoryRow,
} from "@/features/checkout/checkout-customer-option";
import {
  checkoutCustomerHasExItsCorrelation,
  checkoutCustomerTitle,
  isSeededWalkInCustomerName,
} from "@/features/customers/format-pos-customer-label";
import { CustomerListConnectionBadges } from "@/features/customers/CustomerListConnectionBadges";
import type { CustomerListConnectionOverlay } from "@/features/customers/customer-list-connection";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

function displayInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

type CheckoutCustomerIdentityProps = {
  customer: CheckoutCustomerOption;
  selected?: boolean;
  overlay?: CustomerListConnectionOverlay | null;
  className?: string;
};

export function CheckoutCustomerIdentity({
  customer,
  selected = false,
  overlay = null,
  className,
}: CheckoutCustomerIdentityProps) {
  const { t } = useI18n();
  const walkInLabel = t("checkout.walkInCustomer");
  const title = checkoutCustomerTitle(customer, walkInLabel);

  if (isCheckoutBusiness(customer) || isCheckoutBusinessDirectoryRow(customer)) {
    const orgId =
      customer.kind === "Business"
        ? customer.buyerPublicOrganizationId?.trim() || null
        : customer.linkedBuyerPublicOrganizationId?.trim() || null;
    const isDirectB2b = isCheckoutBusiness(customer);
    const pending =
      isDirectB2b && customer.status.trim().toLowerCase() === "pending";
    return (
      <span
        className={cn("checkout-customer-identity", className)}
        data-testid={isDirectB2b ? "checkout-b2b-identity" : "checkout-business-party-identity"}
      >
        <span className="checkout-customer-identity__avatar" aria-hidden>
          {displayInitial(title)}
        </span>
        <span className="checkout-customer-identity__body">
          <span className="checkout-customer-identity__name">{title}</span>
          {orgId ? <span className="checkout-customer-identity__meta">{orgId}</span> : null}
          <span className="checkout-customer-identity__chips">
            <StatusChip tone="success">{t("checkout.badge.b2b")}</StatusChip>
            {pending ? (
              <StatusChip tone="warning">{t("checkout.badge.pending")}</StatusChip>
            ) : isDirectB2b ? (
              <StatusChip tone="success">{t("checkout.badge.active")}</StatusChip>
            ) : null}
          </span>
        </span>
        {selected ? (
          <Check className="checkout-customer-identity__check size-4 shrink-0" aria-hidden />
        ) : null}
      </span>
    );
  }

  const correlated = checkoutCustomerHasExItsCorrelation(customer);
  const walkIn = !correlated && isSeededWalkInCustomerName(customer.displayName);
  const phone = customer.mobileNumber?.trim() || null;

  return (
    <span className={cn("checkout-customer-identity", className)}>
      <span className="checkout-customer-identity__avatar" aria-hidden>
        {displayInitial(title)}
      </span>
      <span className="checkout-customer-identity__body">
        <span className="checkout-customer-identity__name">{title}</span>
        {phone ? <span className="checkout-customer-identity__meta">{phone}</span> : null}
        {walkIn ? (
          <span className="checkout-customer-identity__chips">
            <StatusChip tone="info">{walkInLabel}</StatusChip>
          </span>
        ) : (
          <CustomerListConnectionBadges
            customer={customer}
            overlay={overlay}
            className="checkout-customer-identity__chips"
          />
        )}
      </span>
      {selected ? (
        <Check className="checkout-customer-identity__check size-4 shrink-0" aria-hidden />
      ) : null}
    </span>
  );
}
