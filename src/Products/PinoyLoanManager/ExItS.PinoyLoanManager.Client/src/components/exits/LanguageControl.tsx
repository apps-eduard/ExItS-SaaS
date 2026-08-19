import { SegmentedControl, SegmentedOption } from "@/components/ui/segmented-control";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { LocalePreference } from "@/lib/preferences/ui-preferences";

const OPTIONS: Array<{ value: LocalePreference; labelKey: "locale.en" | "locale.filPH" }> = [
  { value: "en", labelKey: "locale.en" },
  { value: "fil-PH", labelKey: "locale.filPH" },
];

export function LanguageControl() {
  const { t } = useI18n();
  const { preferences, setLocale } = usePreferences();

  return (
    <SegmentedControl label={t("locale.label")}>
      {OPTIONS.map(({ value, labelKey }) => (
        <SegmentedOption
          key={value}
          selected={preferences.locale === value}
          onSelect={() => setLocale(value)}
        >
          {t(labelKey)}
        </SegmentedOption>
      ))}
    </SegmentedControl>
  );
}
