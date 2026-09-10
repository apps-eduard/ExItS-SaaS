import { CheckoutCustomerIdentity } from "@/features/checkout/CheckoutCustomerIdentity";
import type { CheckoutCustomerOption, CheckoutPersonOption } from "@/features/checkout/checkout-customer-option";
import {
  checkoutOptionKey,
  isCheckoutBusiness,
  isCheckoutBusinessDirectoryRow,
  isCheckoutPerson,
} from "@/features/checkout/checkout-customer-option";
import {
  checkoutCreditStatusLabelKey,
  checkoutCreditStatusTone,
} from "@/features/checkout/checkout-utang-credit";
import type { CustomerListConnectionOverlay } from "@/features/customers/customer-list-connection";
import type { KindFilter } from "@/features/customers/customers-kind";
import {
  checkoutCustomerTitle,
  visibleCheckoutCustomers,
} from "@/features/customers/format-pos-customer-label";
import { Button } from "@/components/ui/button";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
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

function directoryCreditStatus(customer: CheckoutCustomerOption): string | null {
  if (isCheckoutBusiness(customer)) {
    return null;
  }
  if (!isCheckoutPerson(customer)) {
    return "NotConfigured";
  }
  return customer.creditStatus ?? "NotConfigured";
}

function directoryAvailableLabel(customer: CheckoutPersonOption): string {
  if ((customer.creditStatus ?? "").trim() !== "Approved") {
    return "—";
  }
  return formatPeso(customer.availableCredit ?? 0);
}

type CheckoutCustomerDirectoryProps = {
  searchId: string;
  searchTestId: string;
  searchLabel: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  customers: CheckoutCustomerOption[];
  customersLoading: boolean;
  customersError?: boolean;
  onRetryLoad?: () => void;
  selectedCustomer: CheckoutCustomerOption | null;
  overlay?: CustomerListConnectionOverlay | null;
  onSelect: (customer: CheckoutCustomerOption) => void;
  disabled?: boolean;
  kindFilter?: KindFilter;
  onKindFilterChange?: (kind: KindFilter) => void;
  /** When true, idle list keeps Local Validation walk-in seeds (Utang requires a person). */
  includeWalkInsWhenIdle?: boolean;
  /** Override idle empty copy (e.g. Utang people-only explanation). */
  idleEmptyMessage?: string;
  /** Utang: compact credit directory (does not hide ineligible customers). */
  showCreditStatus?: boolean;
};

export function CheckoutCustomerDirectory({
  searchId,
  searchTestId,
  searchLabel,
  searchValue,
  onSearchChange,
  customers,
  customersLoading,
  customersError = false,
  onRetryLoad,
  selectedCustomer,
  overlay = null,
  onSelect,
  disabled,
  kindFilter = "all",
  onKindFilterChange,
  includeWalkInsWhenIdle = false,
  idleEmptyMessage,
  showCreditStatus = false,
}: CheckoutCustomerDirectoryProps) {
  const { t } = useI18n();
  const walkInLabel = t("checkout.walkInCustomer");
  const kindFiltered =
    kindFilter === "people"
      ? customers.filter((c) => c.kind === "Customer" && !isCheckoutBusinessDirectoryRow(c))
      : kindFilter === "businesses"
        ? customers.filter((c) => isCheckoutBusinessDirectoryRow(c))
        : customers;
  const visible = visibleCheckoutCustomers(kindFiltered, searchValue, {
    includeWalkInsWhenIdle,
  });
  const idle = searchValue.trim().length === 0;
  const searchHint =
    kindFilter === "businesses"
      ? t("checkout.customerSearchHintBusinesses")
      : kindFilter === "people"
        ? t("checkout.customerSearchHintPeople")
        : t("checkout.customerSearchHint");
  const emptyCopy = idle
    ? idleEmptyMessage ??
      (kindFilter === "businesses"
        ? t("checkout.customerIdleEmptyBusinesses")
        : t("checkout.customerIdleEmpty"))
    : kindFilter === "businesses"
      ? t("checkout.customerEmptyBusinesses")
      : t("checkout.customerEmpty");

  return (
    <div className="checkout-customer-directory">
      <div
        className={cn(
          "mb-2 flex min-w-0 flex-wrap items-center gap-1.5",
          !onKindFilterChange && "mb-0",
        )}
      >
        {onKindFilterChange ? (
          <div
            className="flex flex-wrap items-center gap-1.5"
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
          containerClassName="checkout-customer-directory__search min-w-[10rem] flex-1"
          onChange={(event) => onSearchChange(event.target.value)}
          onClear={() => onSearchChange("")}
        />
      </div>
      <p className="checkout-customer-directory__hint">{searchHint}</p>

      {customersLoading ? (
        <p className="mb-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
          {t("checkout.customerLoading")}
        </p>
      ) : customersError ? (
        <div className="mt-2 flex flex-wrap items-center gap-2" data-testid="checkout-customer-load-error">
          <p className="mb-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("checkout.customerLoadError")}
          </p>
          {onRetryLoad ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-9"
              data-testid="checkout-customer-retry"
              disabled={disabled}
              onClick={onRetryLoad}
            >
              {t("checkout.customerRetry")}
            </Button>
          ) : null}
        </div>
      ) : visible.length === 0 ? (
        <p
          data-testid="checkout-customer-empty"
          className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
        >
          {emptyCopy}
        </p>
      ) : showCreditStatus ? (
        <div className="checkout-credit-directory" data-testid="checkout-credit-directory">
          <div className="checkout-credit-directory__head" aria-hidden>
            <span>{t("checkout.directoryCredit.colCustomer")}</span>
            <span>{t("checkout.directoryCredit.colType")}</span>
            <span>{t("checkout.directoryCredit.colStatus")}</span>
            <span>{t("checkout.directoryCredit.colAvailable")}</span>
          </div>
          <ul className="checkout-credit-directory__list" data-testid="checkout-customer-list">
            {visible.map((customer) => {
              const key = checkoutOptionKey(customer);
              const selected =
                selectedCustomer != null && checkoutOptionKey(selectedCustomer) === key;
              const isB2b = isCheckoutBusinessDirectoryRow(customer);
              const status = directoryCreditStatus(customer);
              const available =
                isCheckoutPerson(customer) && !isCheckoutBusiness(customer)
                  ? directoryAvailableLabel(customer)
                  : "—";
              return (
                <li key={key}>
                  <button
                    type="button"
                    className={cn(
                      "checkout-credit-directory__row",
                      selected && "checkout-credit-directory__row--selected",
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
                    <span
                      className="checkout-credit-directory__name"
                      data-testid="checkout-credit-directory-name"
                    >
                      {checkoutCustomerTitle(customer, walkInLabel)}
                    </span>
                    <span
                      className="checkout-credit-directory__type"
                      data-testid="checkout-credit-directory-type"
                    >
                      {isB2b
                        ? t("checkout.directoryCredit.typeB2b")
                        : t("checkout.directoryCredit.typePerson")}
                    </span>
                    <span
                      className="checkout-credit-directory__status"
                      data-testid="checkout-customer-credit-line"
                    >
                      {status ? (
                        <StatusChip tone={checkoutCreditStatusTone(status)}>
                          {t(checkoutCreditStatusLabelKey(status))}
                        </StatusChip>
                      ) : (
                        <span className="text-muted">
                          {t("checkout.directoryCredit.availableEmDash")}
                        </span>
                      )}
                    </span>
                    <span
                      className="checkout-credit-directory__available tabular-nums"
                      data-testid="checkout-credit-directory-available"
                    >
                      {available}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
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
                    isCheckoutBusinessDirectoryRow(customer) && "checkout-customer-row--b2b",
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
