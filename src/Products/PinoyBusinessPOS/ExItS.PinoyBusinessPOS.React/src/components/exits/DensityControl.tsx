import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { DensityPreference } from "@/lib/preferences/ui-preferences";

export function DensityControl() {
  const { t } = useI18n();
  const { preferences, setDensity } = usePreferences();

  return (
    <SettingsSelect<DensityPreference>
      label={t("density.label")}
      value={preferences.density}
      onChange={setDensity}
      variant="segmented"
      compactLabels
      options={[
        { value: "compact", label: t("density.compact") },
        { value: "balance", label: t("density.balance") },
        { value: "comfort", label: t("density.comfort") },
      ]}
    />
  );
}
