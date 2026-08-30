import type { MessageKey } from "@/i18n/messages";

type BranchDetailsFormProps = {
  name: string;
  contactPhone: string;
  timeZoneId: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
  countryCode: string;
  t: (key: MessageKey) => string;
  onChange: (field: string, value: string) => void;
};

export function BranchDetailsForm({
  name,
  contactPhone,
  timeZoneId,
  addressLine1,
  addressLine2,
  city,
  region,
  postalCode,
  countryCode,
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
              className="catalog-form-select font-normal"
              value={timeZoneId}
              onChange={(e) => onChange("timeZoneId", e.target.value)}
              placeholder="Asia/Manila"
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
              className="catalog-form-select font-normal"
              value={countryCode}
              onChange={(e) => onChange("countryCode", e.target.value)}
              data-testid="branch-country"
            />
          </label>
        </div>
      </section>
    </div>
  );
}
