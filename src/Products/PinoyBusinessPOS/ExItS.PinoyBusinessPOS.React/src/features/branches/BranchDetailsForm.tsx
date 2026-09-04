import type { MessageKey } from "@/i18n/messages";
import {
  BRANCH_DEFAULT_COUNTRY_CODE,
  BRANCH_DEFAULT_TIME_ZONE,
} from "@/features/branches/branch-defaults";
import type { OrganizationBranchType } from "@/features/branches/branch-type";

type BranchDetailsFormProps = {
  name: string;
  contactPhone: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
  branchType: OrganizationBranchType;
  /** When false, Warehouse option is hidden (plan entitlement). */
  warehouseAllowed?: boolean;
  t: (key: MessageKey) => string;
  onChange: (field: string, value: string) => void;
};

export function BranchDetailsForm({
  name,
  contactPhone,
  addressLine1,
  addressLine2,
  city,
  region,
  postalCode,
  branchType,
  warehouseAllowed = true,
  t,
  onChange,
}: BranchDetailsFormProps) {
  return (
    <div className="flex flex-col gap-3" data-testid="branch-details-tab">
      <section className="catalog-form-section exits-animate-panel gap-3">
        <h2 className="catalog-form-section__title">{t("branches.detailsTitle")}</h2>
        <div className="catalog-form-section__grid">
          <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.name")}
            <input
              className="catalog-form-select font-normal"
              value={name}
              onChange={(e) => onChange("name", e.target.value)}
              data-testid="branch-name"
            />
          </label>
          <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.type")}
            <select
              className="catalog-form-select font-normal"
              value={branchType}
              onChange={(e) => onChange("branchType", e.target.value)}
              data-testid="branch-type"
            >
              <option value="Retail">{t("branches.type.retail")}</option>
              {warehouseAllowed || branchType === "Warehouse" ? (
                <option value="Warehouse">{t("branches.type.warehouse")}</option>
              ) : null}
            </select>
            <span className="font-normal text-muted">
              {!warehouseAllowed && branchType !== "Warehouse"
                ? t("branches.type.warehouseLocked")
                : branchType === "Warehouse"
                  ? t("branches.type.warehouseHelp")
                  : t("branches.type.retailHelp")}
            </span>
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.contactPhone")}
            <input
              className="catalog-form-select font-normal"
              value={contactPhone}
              onChange={(e) => onChange("contactPhone", e.target.value)}
              data-testid="branch-phone"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.timeZone")}
            <input
              className="catalog-form-select bg-[var(--exits-surface-muted)] font-normal"
              value={BRANCH_DEFAULT_TIME_ZONE}
              readOnly
              aria-readonly="true"
              data-testid="branch-timezone"
            />
          </label>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="branch-address-section"
      >
        <h2 className="catalog-form-section__title">{t("branches.addressTitle")}</h2>
        <div className="catalog-form-section__grid">
          <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.addressLine1")}
            <input
              className="catalog-form-select font-normal"
              value={addressLine1}
              onChange={(e) => onChange("addressLine1", e.target.value)}
              data-testid="branch-address1"
            />
          </label>
          <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.addressLine2")}
            <input
              className="catalog-form-select font-normal"
              value={addressLine2}
              onChange={(e) => onChange("addressLine2", e.target.value)}
              data-testid="branch-address2"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.city")}
            <input
              className="catalog-form-select font-normal"
              value={city}
              onChange={(e) => onChange("city", e.target.value)}
              data-testid="branch-city"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.region")}
            <input
              className="catalog-form-select font-normal"
              value={region}
              onChange={(e) => onChange("region", e.target.value)}
              data-testid="branch-region"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.postalCode")}
            <input
              className="catalog-form-select font-normal"
              value={postalCode}
              onChange={(e) => onChange("postalCode", e.target.value)}
              data-testid="branch-postal"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.countryCode")}
            <input
              className="catalog-form-select bg-[var(--exits-surface-muted)] font-normal"
              value={BRANCH_DEFAULT_COUNTRY_CODE}
              readOnly
              aria-readonly="true"
              data-testid="branch-country"
            />
          </label>
        </div>
      </section>
    </div>
  );
}
