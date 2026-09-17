import { useI18n } from "@/i18n/I18nProvider";
import {
  PreferencesEmptyState,
  PreferencesSectionPanel,
} from "@/features/preferences/PreferencesSectionPanel";

/**
 * Accessibility — personal a11y preferences.
 * Reduced motion currently follows OS/browser only (no persisted user toggle yet).
 * Future: Reduced motion override, High contrast — do not invent controls here.
 */
export function AccessibilityPreferences() {
  const { t } = useI18n();

  return (
    <PreferencesSectionPanel
      title={t("preferences.section.accessibility")}
      titleId="preferences-accessibility-heading"
      testId="preferences-section-accessibility"
    >
      <PreferencesEmptyState
        message={t("preferences.accessibilityEmpty")}
        testId="preferences-accessibility-empty"
      />
    </PreferencesSectionPanel>
  );
}
