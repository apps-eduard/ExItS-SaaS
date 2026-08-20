import { Languages } from "lucide-react";
import { SegmentedControl, SegmentedOption } from "@/components/ui/segmented-control";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";

export function LanguageControl() {
  const { t } = useI18n();
  const { preferences, setLocale } = usePreferences();

  return (
    <SegmentedControl label={t("locale.label")}>
      <SegmentedOption selected={preferences.locale === "en"} onSelect={() => setLocale("en")}>
        <Languages className="size-3.5 shrink-0" aria-hidden="true" />
        {t("locale.en")}
      </SegmentedOption>
      <SegmentedOption
        selected={preferences.locale === "fil-PH"}
        onSelect={() => setLocale("fil-PH")}
      >
        {t("locale.filPH")}
      </SegmentedOption>
    </SegmentedControl>
  );
}
