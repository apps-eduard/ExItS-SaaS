import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";
import type { LocalePreference } from "@/lib/preferences/ui-preferences";

const OPTIONS: Array<{ value: LocalePreference; labelKey: "locale.en" | "locale.filPH" }> = [
  { value: "en", labelKey: "locale.en" },
  { value: "fil-PH", labelKey: "locale.filPH" },
];

export function LanguageControl() {
  const { t } = useI18n();
  const { preferences, setLocale } = usePreferences();

  return (
    <fieldset className="m-0 min-w-0 border-0 p-0">
      <legend className="mb-2 text-[length:var(--exits-text-sm)] font-semibold text-muted">
        {t("locale.label")}
      </legend>
      <div className="grid grid-cols-2 gap-2" role="radiogroup" aria-label={t("locale.label")}>
        {OPTIONS.map(({ value, labelKey }) => {
          const selected = preferences.locale === value;
          return (
            <Button
              key={value}
              type="button"
              variant={selected ? "default" : "outline"}
              aria-pressed={selected}
              onClick={() => setLocale(value)}
            >
              {t(labelKey)}
            </Button>
          );
        })}
      </div>
    </fieldset>
  );
}
