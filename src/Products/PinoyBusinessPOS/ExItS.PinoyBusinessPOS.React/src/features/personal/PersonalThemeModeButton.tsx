import { symbol } from "@/components/icons/MaterialSymbol";

const LightModeIcon = symbol("light_mode");
const DarkModeIcon = symbol("dark_mode");
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { isDocumentThemeDark } from "@/lib/primary-palette-accents";

/** Navbar sun/moon control. Shows the current mode and switches light and dark. */
export function PersonalThemeModeButton({ className }: { className?: string }) {
  const { t } = useI18n();
  const { preferences, setTheme } = usePreferences();
  const dark =
    preferences.theme === "dark" || (preferences.theme !== "light" && isDocumentThemeDark());

  return (
    <button
      type="button"
      className={cn(
        "personal-theme-mode inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full",
        className,
      )}
      data-testid="personal-theme-mode"
      aria-label={dark ? t("theme.light") : t("theme.dark")}
      aria-pressed={dark}
      onClick={() => setTheme(dark ? "light" : "dark")}
    >
      {dark ? <DarkModeIcon className="size-5" aria-hidden /> : <LightModeIcon className="size-5" aria-hidden />}
    </button>
  );
}
