import { LanguageControl } from "@/components/exits/LanguageControl";
import { useI18n } from "@/i18n/I18nProvider";
import { PreferencesSectionPanel } from "@/features/preferences/PreferencesSectionPanel";

/**
 * Language & Region — personal locale.
 * Future (not in this task): date/number formatting preferences.
 * Text direction follows language/i18n automatically.
 */
export function LanguageRegionPreferences() {
  const { t } = useI18n();

  return (
    <PreferencesSectionPanel
      title={t("preferences.section.languageRegion")}
      titleId="preferences-language-region-heading"
      testId="preferences-section-language-region"
    >
      <div className="divide-y divide-border px-4">
        <LanguageControl />
      </div>
    </PreferencesSectionPanel>
  );
}
