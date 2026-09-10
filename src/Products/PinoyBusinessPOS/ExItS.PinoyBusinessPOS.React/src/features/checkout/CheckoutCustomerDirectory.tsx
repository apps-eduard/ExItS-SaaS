import { CheckoutCustomerIdentity } from "@/features/checkout/CheckoutCustomerIdentity";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import {
  checkoutOptionKey,
  isCheckoutBusiness,
} from "@/features/checkout/checkout-customer-option";
import type { CustomerListConnectionOverlay } from "@/features/customers/customer-list-connection";
import type { KindFilter } from "@/features/customers/customers-kind";
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
  kindFilter?: KindFilter;
  onKindFilterChange?: (kind: KindFilter) => void;
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
  kindFilter = "all",
  onKindFilterChange,
}: CheckoutCustomerDirectoryProps) {
  const { t } = useI18n();
  const walkInLabel = t("checkout.walkInCustomer");
  const kindFiltered =
    kindFilter === "people"
      ? customers.filter((c) => c.kind === "Customer")
      : kindFilter === "businesses"
        ? customers.filter((c) => c.kind === "Business")
        : customers;
  const visible = visibleCheckoutCustomers(kindFiltered, searchValue);
  const idle = searchValue.trim().length === 0;

  return (
    <div className="checkout-customer-directory">
      {onKindFilterChange ? (
        <div
          className="mb-2 flex flex-wrap gap-1.5"
          role="tablist"
          aria-label={t("checkout.customerKindFilter")}
          data-testid="checkout-customer-kind-tabs"
        >
          {(
            [
              ["all", t("checkout.customerKindAll")],
              ["people", t("checkout.customerKindPeople")],
              ["businesses", t("checkout.customerKindBusinesses")],
            ] as const
          ).map(([key, label]) => (
            <button
              key={key}
              type="button"
              role="tab"
              aria-selected={kindFilter === key}
              className={cn(
                "exits-chip exits-chip--sm",
                kindFilter === key && "exits-chip--active",
              )}
              data-testid={`checkout-customer-kind-${key}`}
              disabled={disabled}
              onClick={() => onKindFilterChange(key)}
            >
              {label}
            </button>
          ))}
        </div>
      ) : null}

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
            const key = checkoutOptionKey(customer);
            const selected =
              selectedCustomer != null && checkoutOptionKey(selectedCustomer) === key;
            return (
              <li key={key}>
                <button
                  type="button"
                  className={cn(
                    "checkout-customer-row",
                    selected && "checkout-customer-row--selected",
                    isCheckoutBusiness(customer) && "checkout-customer-row--b2b",
                  )}
                  data-testid={
                    isCheckoutBusiness(customer)
                      ? `checkout-business-${customer.connectionId}`
                      : `checkout-customer-${customer.customerId}`
                  }
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
