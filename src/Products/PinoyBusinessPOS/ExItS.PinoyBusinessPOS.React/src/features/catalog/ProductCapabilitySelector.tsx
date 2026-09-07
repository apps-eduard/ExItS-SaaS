import type { ProductCapabilityFlags } from "@/features/catalog/product-business-usage";
import { capabilitiesAreValid } from "@/features/catalog/product-business-usage";
import { useI18n } from "@/i18n/I18nProvider";

type Props = {
  value: ProductCapabilityFlags;
  onChange: (next: ProductCapabilityFlags) => void;
  disabled?: boolean;
};

/**
 * Overlapping product capabilities — Can be sold / ingredient / produced may combine
 * (e.g. Sugar sellable + ingredient). Distinct from exclusive BusinessUsage radios.
 */
export function ProductCapabilitySelector({ value, onChange, disabled = false }: Props) {
  const { t } = useI18n();

  function toggle(key: keyof ProductCapabilityFlags) {
    const next = { ...value, [key]: !value[key] };
    if (!capabilitiesAreValid(next)) {
      // Keep at least one capability selected.
      return;
    }
    onChange(next);
  }

  const options: {
    key: keyof ProductCapabilityFlags;
    labelKey: "catalog.capability.canBeSold" | "catalog.capability.canBeIngredient" | "catalog.capability.isProduced";
    hintKey: "catalog.capability.canBeSoldHint" | "catalog.capability.canBeIngredientHint" | "catalog.capability.isProducedHint";
    testId: string;
  }[] = [
    {
      key: "canBeSold",
      labelKey: "catalog.capability.canBeSold",
      hintKey: "catalog.capability.canBeSoldHint",
      testId: "catalog-capability-sold",
    },
    {
      key: "canBeUsedAsIngredient",
      labelKey: "catalog.capability.canBeIngredient",
      hintKey: "catalog.capability.canBeIngredientHint",
      testId: "catalog-capability-ingredient",
    },
    {
      key: "isProduced",
      labelKey: "catalog.capability.isProduced",
      hintKey: "catalog.capability.isProducedHint",
      testId: "catalog-capability-produced",
    },
  ];

  return (
    <fieldset
      className="catalog-product-capabilities m-0 min-w-0 border-0 p-0"
      disabled={disabled}
      data-testid="catalog-product-capabilities"
    >
      <legend className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
        {t("catalog.capability.question")}
      </legend>
      <div className="flex flex-col gap-2">
        {options.map((option) => {
          const id = `catalog-capability-${option.key}`;
          return (
            <label
              key={option.key}
              htmlFor={id}
              className="flex cursor-pointer gap-3 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] px-3 py-2.5"
            >
              <input
                id={id}
                type="checkbox"
                checked={value[option.key]}
                data-testid={option.testId}
                onChange={() => toggle(option.key)}
                className="mt-1 shrink-0"
              />
              <span className="min-w-0">
                <span className="block text-[length:var(--exits-text-sm)] font-semibold">
                  {t(option.labelKey)}
                </span>
                <span className="mt-0.5 block text-[length:var(--exits-text-sm)] text-muted">
                  {t(option.hintKey)}
                </span>
              </span>
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}
