import { Languages } from "lucide-react";
import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { LocalePreference } from "@/lib/preferences/ui-preferences";

const LOCALE_OPTIONS: {
  value: LocalePreference;
  labelKey: "locale.en" | "locale.filPH" | "locale.cebPH" | "locale.iloPH" | "locale.hilPH";
}[] = [
  { value: "en", labelKey: "locale.en" },
  { value: "fil-PH", labelKey: "locale.filPH" },
  { value: "ceb-PH", labelKey: "locale.cebPH" },
  { value: "ilo-PH", labelKey: "locale.iloPH" },
  { value: "hil-PH", labelKey: "locale.hilPH" },
];

export function LanguageControl() {
  const { t } = useI18n();
  const { preferences, setLocale } = usePreferences();

  return (
    <SettingsSelect<LocalePreference>
      label={t("locale.label")}
      value={preferences.locale}
      onChange={setLocale}
      options={LOCALE_OPTIONS.map((option, index) => ({
        value: option.value,
        label: t(option.labelKey),
        icon:
          index === 0 ? <Languages className="size-3.5 shrink-0" aria-hidden="true" /> : undefined,
      }))}
    />
  );
}
