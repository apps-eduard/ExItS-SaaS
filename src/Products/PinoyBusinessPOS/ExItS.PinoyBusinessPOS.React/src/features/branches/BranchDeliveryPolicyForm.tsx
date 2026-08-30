import type { MessageKey } from "@/i18n/messages";

type BranchDeliveryPolicyFormProps = {
  minimumOrder: string;
  baseFee: string;
  includedKm: string;
  additionalPerKm: string;
  maximumKm: string;
  freeThreshold: string;
  t: (key: MessageKey) => string;
  onChange: (field: string, value: string) => void;
};

export function BranchDeliveryPolicyForm({
  minimumOrder,
  baseFee,
  includedKm,
  additionalPerKm,
  maximumKm,
  freeThreshold,
  t,
  onChange,
}: BranchDeliveryPolicyFormProps) {
  return (
    <section
      className="catalog-form-section exits-animate-panel gap-3"
      data-testid="branch-delivery-policy"
    >
      <h2 className="catalog-form-section__title">{t("branches.deliveryPolicyTitle")}</h2>
      <div className="catalog-form-section__grid">
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.minimumOrder")}
          <input
            type="number"
            step="any"
            className="catalog-form-select font-normal"
            value={minimumOrder}
            onChange={(e) => onChange("minimumOrder", e.target.value)}
            data-testid="policy-minimum"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.baseFee")}
          <input
            type="number"
            step="any"
            className="catalog-form-select font-normal"
            value={baseFee}
            onChange={(e) => onChange("baseFee", e.target.value)}
            data-testid="policy-base-fee"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.includedKm")}
          <input
            type="number"
            step="any"
            className="catalog-form-select font-normal"
            value={includedKm}
            onChange={(e) => onChange("includedKm", e.target.value)}
            data-testid="policy-included-km"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.additionalPerKm")}
          <input
            type="number"
            step="any"
            className="catalog-form-select font-normal"
            value={additionalPerKm}
            onChange={(e) => onChange("additionalPerKm", e.target.value)}
            data-testid="policy-additional-km"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.maximumKm")}
          <input
            type="number"
            step="any"
            className="catalog-form-select font-normal"
            value={maximumKm}
            onChange={(e) => onChange("maximumKm", e.target.value)}
            data-testid="policy-maximum-km"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.freeThreshold")}
          <input
            type="number"
            step="any"
            className="catalog-form-select font-normal"
            value={freeThreshold}
            onChange={(e) => onChange("freeThreshold", e.target.value)}
            data-testid="policy-free-threshold"
          />
        </label>
      </div>
    </section>
  );
}
