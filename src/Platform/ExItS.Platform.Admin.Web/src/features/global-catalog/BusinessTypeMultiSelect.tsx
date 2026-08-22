import type { GlobalBusinessTypeItem } from "@/api/global-catalog/global-catalog-types";
import { usePreferences } from "@/hooks/use-preferences";

const controlClass =
  "min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 py-2 text-[length:var(--exits-text-sm)] text-foreground";

export function BusinessTypeMultiSelect({
  id,
  options,
  value,
  disabled,
  onChange,
}: {
  id: string;
  options: readonly GlobalBusinessTypeItem[];
  value: readonly string[];
  disabled?: boolean;
  onChange: (next: string[]) => void;
}) {
  const { t } = usePreferences();

  function toggle(businessTypeId: string) {
    if (value.includes(businessTypeId)) {
      onChange(value.filter((item) => item !== businessTypeId));
      return;
    }
    onChange([...value, businessTypeId]);
  }

  return (
    <fieldset className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3">
      <legend className="px-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("globalCatalog.field.businessTypes")}
      </legend>
      {options.length === 0 ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted">{t("globalCatalog.businessTypes.empty")}</p>
      ) : (
        <ul className="grid gap-1.5 sm:grid-cols-2">
          {options.map((option) => (
            <li key={option.id}>
              <label className={`flex items-center gap-2 ${controlClass}`} htmlFor={`${id}-${option.id}`}>
                <input
                  id={`${id}-${option.id}`}
                  type="checkbox"
                  checked={value.includes(option.id)}
                  disabled={disabled}
                  onChange={() => toggle(option.id)}
                />
                <span>
                  <span className="font-medium">{option.name}</span>
                  <span className="ml-1 font-mono text-[length:var(--exits-text-xs)] text-muted">
                    {option.code}
                  </span>
                </span>
              </label>
            </li>
          ))}
        </ul>
      )}
    </fieldset>
  );
}
