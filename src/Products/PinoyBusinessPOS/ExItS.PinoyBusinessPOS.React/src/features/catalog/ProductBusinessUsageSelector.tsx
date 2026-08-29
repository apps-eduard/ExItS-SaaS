import type { ProductBusinessUsage } from "@/features/catalog/product-business-usage";
import {
  PRODUCT_BUSINESS_USAGES,
  businessUsageHintKey,
  businessUsageLabelKey,
} from "@/features/catalog/product-business-usage";
import { useI18n } from "@/i18n/I18nProvider";

type Props = {
  value: ProductBusinessUsage | null;
  onChange: (next: ProductBusinessUsage) => void;
  /** When true, no option is pre-selected until the user chooses (supplier onboarding). */
  requireExplicitChoice?: boolean;
  name?: string;
  disabled?: boolean;
};

export function ProductBusinessUsageSelector({
  value,
  onChange,
  requireExplicitChoice = false,
  name = "businessUsage",
  disabled = false,
}: Props) {
  const { t } = useI18n();

  return (
    <fieldset className="catalog-business-usage m-0 min-w-0 border-0 p-0" disabled={disabled}>
      <legend className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
        {t("catalog.businessUsage.question")}
      </legend>
      <div className="flex flex-col gap-2">
        {PRODUCT_BUSINESS_USAGES.map((usage) => {
          const id = `${name}-${usage}`;
          const checked = value === usage;
          return (
            <label
              key={usage}
              htmlFor={id}
              className="flex cursor-pointer gap-3 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] px-3 py-2.5"
            >
              <input
                id={id}
                type="radio"
                name={name}
                value={usage}
                checked={checked}
                required={requireExplicitChoice && value === null}
                onChange={() => onChange(usage)}
                className="mt-1 shrink-0"
              />
              <span className="min-w-0">
                <span className="block text-[length:var(--exits-text-sm)] font-semibold">
                  {t(businessUsageLabelKey(usage))}
                </span>
                <span className="mt-0.5 block text-[length:var(--exits-text-sm)] text-muted">
                  {t(businessUsageHintKey(usage))}
                </span>
              </span>
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}
