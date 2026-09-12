import { DensityControl } from "@/components/exits/DensityControl";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { useI18n } from "@/i18n/I18nProvider";
import { PreferencesSectionPanel } from "@/features/preferences/PreferencesSectionPanel";

/**
 * Appearance — personal visual presentation.
 * Future (not in this task): Primary color, Control shape, Motion.
 */
export function AppearancePreferences() {
  const { t } = useI18n();

  return (
    <PreferencesSectionPanel
      title={t("preferences.section.appearance")}
      titleId="preferences-appearance-heading"
      testId="preferences-section-appearance"
    >
      <div className="divide-y divide-border px-4">
        <ThemeControl />
        <DensityControl />
      </div>
    </PreferencesSectionPanel>
  );
}
