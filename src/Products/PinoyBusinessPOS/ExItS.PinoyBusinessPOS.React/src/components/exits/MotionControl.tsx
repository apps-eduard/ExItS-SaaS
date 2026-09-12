import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { MotionPreference } from "@/lib/preferences/ui-preferences";

export function MotionControl() {
  const { t } = useI18n();
  const { preferences, setMotion } = usePreferences();

  return (
    <div data-testid="preferences-motion">
      <SettingsSelect<MotionPreference>
        label={t("appearance.motion.label")}
        value={preferences.motion}
        onChange={setMotion}
        options={[
          { value: "system", label: t("appearance.motion.system") },
          { value: "reduced", label: t("appearance.motion.reduced") },
        ]}
      />
    </div>
  );
}
