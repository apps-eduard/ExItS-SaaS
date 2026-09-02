import { Package, Truck } from "lucide-react";
import type { CustomerStorefrontDto } from "@/api/pos/pos-customer-orders-client";
import { StatusChip } from "@/components/exits/StatusChip";
import { SearchField } from "@/components/exits/SearchField";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { PersonalStoreIdentity } from "@/features/customer-ordering/PersonalStoreIdentity";
import { personalStoreDisplayName } from "@/features/customer-ordering/format-personal-store-label";
import { useI18n } from "@/i18n/I18nProvider";

type StorefrontHeaderProps = {
  storefront: CustomerStorefrontDto;
  branchId: string | null;
  onBranchChange: (branchId: string | null) => void;
  /** When true (branch QR entry), keep the exact branch — no silent switcher. */
  branchLocked?: boolean;
  search: string;
  onSearchChange: (value: string) => void;
  onSearchClear: () => void;
  categoryId: string;
  onCategoryChange: (categoryId: string) => void;
  relationshipLabel?: string | null;
};

export function StorefrontHeader({
  storefront,
  branchId,
  onBranchChange,
  branchLocked = false,
  search,
  onSearchChange,
  onSearchClear,
  categoryId,
  onCategoryChange,
  relationshipLabel,
}: StorefrontHeaderProps) {
  const { t } = useI18n();
  const storeName = personalStoreDisplayName(storefront.organizationDisplayName);
  const selectedBranch =
    storefront.branches.find((b) => b.branchId === branchId) ?? storefront.branches[0] ?? null;
  const pickupAvailable = storefront.branches.some((b) => b.pickupEnabled && b.pickupOperational);
  const deliveryAvailable =
    storefront.canCustomerDelivery &&
    storefront.branches.some((b) => b.deliveryEnabled && b.deliveryOperational);
  const showMeta =
    Boolean(selectedBranch) || pickupAvailable || deliveryAvailable || Boolean(selectedBranch?.onlineOrdersPaused);

  return (
    <header className="pc-store-card pc-store-card--static exits-animate-panel" data-testid="storefront-header">
      <PersonalStoreIdentity
        storeName={storeName}
        relationshipLabel={relationshipLabel}
        canCustomerOrder={storefront.canCustomerOrder}
        headingLevel="h2"
      />

      {showMeta ? (
        <div className="pc-store-card__meta">
          {selectedBranch ? (
            <span className="pc-store-card__meta-item">{selectedBranch.name}</span>
          ) : null}
          {pickupAvailable ? (
            <span className="pc-store-card__meta-item">
              <Package className="size-3.5 shrink-0" aria-hidden />
              {t("orders.pickup")}
            </span>
          ) : null}
          {deliveryAvailable ? (
            <span className="pc-store-card__meta-item">
              <Truck className="size-3.5 shrink-0" aria-hidden />
              {t("orders.delivery")}
            </span>
          ) : null}
          {selectedBranch?.onlineOrdersPaused ? (
            <StatusChip tone="warning">{t("orders.paused")}</StatusChip>
          ) : null}
        </div>
      ) : null}

      <div className="pc-storefront-toolbar">
        {storefront.branches.length > 1 && !branchLocked ? (
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
