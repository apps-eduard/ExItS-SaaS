import type { GlobalBusinessTypeItem } from "@/api/global-catalog/global-catalog-types";
import { globalCatalogControlClass } from "@/features/global-catalog/global-catalog-presentation";
import { usePreferences } from "@/hooks/use-preferences";

export function BusinessTypePicker({
  id,
  options,
  value,
  disabled,
  onChange,
}: {
  id: string;
  options: readonly GlobalBusinessTypeItem[];
  value: string;
  disabled?: boolean;
  onChange: (businessTypeId: string) => void;
}) {
  const { t } = usePreferences();

  return (
    <select
      id={id}
      className={globalCatalogControlClass}
      value={value}
      disabled={disabled}
      onChange={(event) => onChange(event.target.value)}
    >
      <option value="">{t("globalCatalog.templates.selectBusinessType")}</option>
      {options.map((option) => (
        <option key={option.id} value={option.id}>
          {option.name} ({option.code})
        </option>
      ))}
    </select>
  );
}
