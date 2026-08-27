import { CheckoutCustomerIdentity } from "@/features/checkout/CheckoutCustomerIdentity";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import type { CustomerListConnectionOverlay } from "@/features/customers/customer-list-connection";
import {
  checkoutCustomerTitle,
  visibleCheckoutCustomers,
} from "@/features/customers/format-pos-customer-label";
import { Button } from "@/components/ui/button";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type CheckoutCustomerSelectedCardProps = {
  customer: CheckoutCustomerOption;
  overlay?: CustomerListConnectionOverlay | null;
  disabled?: boolean;
  onClear: () => void;
};

export function CheckoutCustomerSelectedCard({
  customer,
  overlay = null,
  disabled,
  onClear,
}: CheckoutCustomerSelectedCardProps) {
  const { t } = useI18n();

  return (
    <div className="checkout-customer-selected" data-testid="checkout-customer-selected">
      <p className="checkout-customer-selected__label">{t("checkout.customerSelected")}</p>
      <div className="checkout-customer-selected__card">
        <CheckoutCustomerIdentity customer={customer} overlay={overlay} selected />
        <Button
          type="button"
          variant="ghost"
          className="min-h-9 shrink-0"
          data-testid="checkout-customer-clear"
          disabled={disabled}
          onClick={onClear}
        >
          {t("checkout.customerClear")}
        </Button>
      </div>
    </div>
  );
}

type CheckoutCustomerDirectoryProps = {
  searchId: string;
  searchTestId: string;
  searchLabel: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  customers: CheckoutCustomerOption[];
  customersLoading: boolean;
  selectedCustomer: CheckoutCustomerOption | null;
  overlay?: CustomerListConnectionOverlay | null;
  onSelect: (customer: CheckoutCustomerOption) => void;
  disabled?: boolean;
};

export function CheckoutCustomerDirectory({
  searchId,
  searchTestId,
  searchLabel,
  searchValue,
  onSearchChange,
  customers,
  customersLoading,
  selectedCustomer,
  overlay = null,
  onSelect,
  disabled,
}: CheckoutCustomerDirectoryProps) {
  const { t } = useI18n();
  const walkInLabel = t("checkout.walkInCustomer");
  const visible = visibleCheckoutCustomers(customers, searchValue);
  const idle = searchValue.trim().length === 0;

  return (
    <div className="checkout-customer-directory">
      <SearchField
        id={searchId}
        label={searchLabel}
        placeholder={searchLabel}
        value={searchValue}
        disabled={disabled}
        data-testid={searchTestId}
        containerClassName="checkout-customer-directory__search"
        onChange={(event) => onSearchChange(event.target.value)}
        onClear={() => onSearchChange("")}
      />
      <p className="checkout-customer-directory__hint">{t("checkout.customerSearchHint")}</p>

      {customersLoading ? (
        <p className="mb-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
          {t("checkout.customerLoading")}
        </p>
      ) : visible.length === 0 ? (
        <p
          data-testid="checkout-customer-empty"
          className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
        >
          {idle ? t("checkout.customerIdleEmpty") : t("checkout.customerEmpty")}
        </p>
      ) : (
        <ul className="checkout-customer-list" data-testid="checkout-customer-list">
          {visible.map((customer) => {
            const selected = selectedCustomer?.customerId === customer.customerId;
            return (
              <li key={customer.customerId}>
                <button
                  type="button"
                  className={cn(
                    "checkout-customer-row",
                    selected && "checkout-customer-row--selected",
                  )}
                  data-testid={`checkout-customer-${customer.customerId}`}
                  disabled={disabled}
                  aria-pressed={selected}
                  aria-label={checkoutCustomerTitle(customer, walkInLabel)}
                  onClick={() => onSelect(customer)}
                >
                  <CheckoutCustomerIdentity
                    customer={customer}
                    overlay={overlay}
                    selected={selected}
                  />
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
