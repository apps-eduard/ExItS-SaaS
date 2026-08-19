import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/use-preferences";
import type { Language, ThemeMode } from "@/lib/preferences/ui-preferences";

export function AuthLayout({ children }: { children: ReactNode }) {
  const { t, theme, language, setTheme, setLanguage } = usePreferences();

  return (
    <div className="min-h-dvh overflow-x-hidden bg-background px-4 py-6 sm:px-6 sm:py-10">
      <div className="mx-auto flex w-full max-w-md flex-col gap-4">
        <div className="flex flex-wrap gap-2">
          {(["system", "light", "dark"] as const).map((mode) => (
            <Button
              key={mode}
              type="button"
              size="sm"
              variant={theme === mode ? "default" : "outline"}
              aria-pressed={theme === mode}
              onClick={() => setTheme(mode satisfies ThemeMode)}
            >
              {t(`preferences.theme.${mode}`)}
            </Button>
          ))}
          <Button
            type="button"
            size="sm"
            variant={language === "en" ? "default" : "outline"}
            aria-pressed={language === "en"}
            onClick={() => setLanguage("en" satisfies Language)}
          >
            {t("preferences.language.en")}
          </Button>
          <Button
            type="button"
            size="sm"
            variant={language === "fil-PH" ? "default" : "outline"}
            aria-pressed={language === "fil-PH"}
            onClick={() => setLanguage("fil-PH")}
          >
            {t("preferences.language.fil")}
          </Button>
        </div>
        {children}
      </div>
    </div>
  );
}
