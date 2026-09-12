import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { ControlShapePreference } from "@/lib/preferences/ui-preferences";

export function ControlShapeControl() {
  const { t } = useI18n();
  const { preferences, setControlShape } = usePreferences();

  return (
    <SettingsSelect<ControlShapePreference>
      label={t("appearance.controlShape.label")}
      value={preferences.controlShape}
      onChange={setControlShape}
      variant="segmented"
      testId="preferences-control-shape"
      options={[
        { value: "standard", label: t("appearance.controlShape.standard") },
        { value: "soft", label: t("appearance.controlShape.soft") },
        { value: "pill", label: t("appearance.controlShape.pill") },
      ]}
    />
  );
}
