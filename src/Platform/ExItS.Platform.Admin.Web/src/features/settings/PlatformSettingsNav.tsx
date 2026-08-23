import { NavLink } from "react-router-dom";
import {
  SETTINGS_SECTIONS,
  settingsSectionHref,
} from "@/features/settings/settings-sections";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function PlatformSettingsNav() {
  const { t } = usePreferences();

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      "rounded-[var(--exits-density-radius)] px-2 py-1 text-[length:var(--exits-text-sm)] font-medium",
      isActive
        ? "bg-surface-muted text-foreground"
        : "text-muted hover:bg-surface-muted/70 hover:text-foreground",
    );

  return (
    <nav aria-label={t("settings.workspace.nav")} className="min-w-0">
      <ul className="flex flex-wrap gap-1">
        {SETTINGS_SECTIONS.map((section) => (
          <li key={section.id}>
            <NavLink className={linkClass} to={settingsSectionHref(section)}>
              {t(section.navLabelKey)}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
