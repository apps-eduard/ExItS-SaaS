import { ChevronsDownUp, ChevronsUpDown, Equal } from "lucide-react";
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
      options={[
        {
          value: "compact",
          label: t("density.compact"),
          icon: <ChevronsDownUp className="size-3.5 shrink-0" aria-hidden="true" />,
        },
        {
          value: "balance",
          label: t("density.balance"),
          icon: <Equal className="size-3.5 shrink-0" aria-hidden="true" />,
        },
        {
          value: "comfort",
          label: t("density.comfort"),
          icon: <ChevronsUpDown className="size-3.5 shrink-0" aria-hidden="true" />,
        },
      ]}
    />
  );
}
