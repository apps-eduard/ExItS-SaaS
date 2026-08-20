import { Languages } from "lucide-react";
import { useState } from "react";
import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { LocalePreference } from "@/lib/preferences/ui-preferences";

export function LanguageControl() {
  const { t } = useI18n();
  const { preferences, setLocale } = usePreferences();
  const [open, setOpen] = useState(false);

  return (
    <SettingsSelect<LocalePreference>
      label={t("locale.label")}
      value={preferences.locale}
      open={open}
      onOpenChange={setOpen}
      onChange={setLocale}
      options={[
        {
          value: "en",
          label: t("locale.en"),
          icon: <Languages className="size-3.5 shrink-0" aria-hidden="true" />,
        },
        {
          value: "fil-PH",
          label: t("locale.filPH"),
        },
      ]}
    />
  );
}
