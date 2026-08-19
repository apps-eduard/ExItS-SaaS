import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/use-preferences";
import type { Language, ThemeMode } from "@/lib/preferences/ui-preferences";

export function AuthLayout({ children }: { children: ReactNode }) {
  const { t, theme, language, setTheme, setLanguage } = usePreferences();

  return (
    <div className="min-h-dvh overflow-x-hidden bg-background lg:grid lg:grid-cols-[minmax(17rem,38%)_minmax(0,1fr)]">
      <aside className="relative hidden overflow-hidden bg-primary px-10 py-12 text-primary-foreground lg:flex lg:flex-col lg:justify-between">
        <div>
          <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-[0.18em] uppercase">
            ExItS
          </p>
          <p className="mt-6 text-[length:var(--exits-text-2xl)] font-bold leading-tight">
            {t("auth.product")}
          </p>
          <p className="mt-2 max-w-sm text-[length:var(--exits-text-md)] text-primary-foreground/85">
            {t("auth.productSubtitle")}
          </p>
        </div>
        <p className="max-w-sm text-[length:var(--exits-text-sm)] text-primary-foreground/75">
          {t("auth.panelHint")}
        </p>
      </aside>

      <div className="flex min-h-dvh flex-col">
        <header className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 sm:px-6">
          <div className="min-w-0 lg:hidden">
            <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-[0.16em] text-primary uppercase">
              ExItS
            </p>
            <p className="truncate text-[length:var(--exits-text-sm)] text-muted">
              {t("auth.product")} · {t("auth.productSubtitle")}
            </p>
          </div>
          <div className="ml-auto flex flex-wrap justify-end gap-1">
            {(["system", "light", "dark"] as const).map((mode) => (
              <Button
                key={mode}
                type="button"
                size="sm"
                className="h-8 min-h-8 px-2.5 text-[length:var(--exits-text-xs)]"
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
              className="h-8 min-h-8 px-2.5 text-[length:var(--exits-text-xs)]"
              variant={language === "en" ? "default" : "outline"}
              aria-pressed={language === "en"}
              onClick={() => setLanguage("en" satisfies Language)}
            >
              {t("preferences.language.en")}
            </Button>
            <Button
              type="button"
              size="sm"
              className="h-8 min-h-8 px-2.5 text-[length:var(--exits-text-xs)]"
              variant={language === "fil-PH" ? "default" : "outline"}
              aria-pressed={language === "fil-PH"}
              onClick={() => setLanguage("fil-PH")}
            >
              {t("preferences.language.fil")}
            </Button>
          </div>
        </header>

        <div className="flex flex-1 justify-center px-4 pb-8 sm:items-center sm:px-6">
          <div className="w-full max-w-[24.5rem]">{children}</div>
        </div>
      </div>
    </div>
  );
}
