import type { CustomerStorefrontDto } from "@/api/pos/pos-customer-orders-client";
import { StatusChip } from "@/components/exits/StatusChip";
import { SearchField } from "@/components/exits/SearchField";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { storeDisplayInitial } from "@/features/customer-ordering/personal-commerce-ui";
import { useI18n } from "@/i18n/I18nProvider";

type StorefrontHeaderProps = {
  storefront: CustomerStorefrontDto;
  branchId: string | null;
  onBranchChange: (branchId: string | null) => void;
  search: string;
  onSearchChange: (value: string) => void;
  onSearchClear: () => void;
  categoryId: string;
  onCategoryChange: (categoryId: string) => void;
};

export function StorefrontHeader({
  storefront,
  branchId,
  onBranchChange,
  search,
  onSearchChange,
  onSearchClear,
  categoryId,
  onCategoryChange,
}: StorefrontHeaderProps) {
  const { t } = useI18n();
  const selectedBranch =
    storefront.branches.find((b) => b.branchId === branchId) ?? storefront.branches[0] ?? null;
  const pickupAvailable = storefront.branches.some((b) => b.pickupEnabled && b.pickupOperational);
  const deliveryAvailable =
    storefront.canCustomerDelivery &&
    storefront.branches.some((b) => b.deliveryEnabled && b.deliveryOperational);

  return (
    <header className="pc-storefront-hero exits-animate-panel" data-testid="storefront-header">
      <div className="pc-storefront-hero__identity">
        <span className="pc-storefront-hero__avatar" aria-hidden>
          {storeDisplayInitial(storefront.organizationDisplayName)}
        </span>
        <div className="min-w-0 flex-1">
          <h1 className="pc-storefront-hero__title">{storefront.organizationDisplayName}</h1>
          <p className="pc-storefront-hero__subtitle">
            {selectedBranch ? selectedBranch.name : t("personal.shopLede")}
          </p>
          <div className="pc-storefront-hero__chips mt-2">
            <StatusChip tone="success">{t("personal.orderingAvailable")}</StatusChip>
            {pickupAvailable ? <StatusChip tone="info">{t("orders.pickup")}</StatusChip> : null}
            {deliveryAvailable ? <StatusChip tone="info">{t("orders.delivery")}</StatusChip> : null}
            {selectedBranch?.onlineOrdersPaused ? (
              <StatusChip tone="warning">{t("orders.paused")}</StatusChip>
            ) : null}
          </div>
        </div>
      </div>

      <div className="pc-storefront-toolbar">
        {storefront.branches.length > 1 ? (
          <label className="pc-field">
            <span className="pc-field__label">{t("orders.branch")}</span>
            <select
              className="pc-field__control"
              data-testid="shop-branch-select"
              value={branchId ?? ""}
              onChange={(e) => onBranchChange(e.target.value || null)}
            >
              {storefront.branches.map((b) => (
                <option key={b.branchId} value={b.branchId}>
                  {b.name}
                  {b.onlineOrdersPaused ? ` (${t("orders.paused")})` : ""}
                </option>
              ))}
            </select>
          </label>
        ) : null}

        <SearchField
          label={t("orders.searchProducts")}
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          onClear={onSearchClear}
          placeholder={t("orders.searchProducts")}
        />

        {storefront.categories.length > 0 ? (
          <SellCategoryFilter
            categories={storefront.categories.map((c) => ({
              categoryId: c.categoryId,
              name: c.name,
            }))}
            activeCategoryId={categoryId}
            allLabel={t("catalogGlobal.allCategories")}
            listLabel={t("catalogGlobal.allCategories")}
            onSelect={onCategoryChange}
          />
        ) : null}
      </div>
    </header>
  );
}
