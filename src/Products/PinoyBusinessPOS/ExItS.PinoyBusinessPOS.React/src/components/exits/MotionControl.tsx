import { Accessibility, Activity } from "lucide-react";
import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { MotionPreference } from "@/lib/preferences/ui-preferences";

export function MotionControl() {
  const { t } = useI18n();
  const { preferences, setMotion } = usePreferences();

  return (
    <SettingsSelect<MotionPreference>
      label={t("appearance.motion.label")}
      value={preferences.motion}
      onChange={setMotion}
      variant="segmented"
      testId="preferences-motion"
      options={[
        {
          value: "system",
          label: t("appearance.motion.system"),
          icon: <Activity className="size-3.5 shrink-0" aria-hidden="true" />,
        },
        {
          value: "reduced",
          label: t("appearance.motion.reduced"),
          icon: <Accessibility className="size-3.5 shrink-0" aria-hidden="true" />,
        },
      ]}
    />
  );
}
