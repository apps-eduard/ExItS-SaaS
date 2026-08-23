import type { LucideIcon } from "lucide-react";
import {
  Boxes,
  FlaskConical,
  Landmark,
  Send,
  Settings,
  Shield,
  Star,
} from "lucide-react";
import { NavLink } from "react-router-dom";
import {
  SETTINGS_SECTIONS,
  type SettingsSectionId,
  settingsSectionHref,
} from "@/features/settings/settings-sections";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

const SETTINGS_TAB_ICONS: Record<SettingsSectionId, LucideIcon> = {
  general: Settings,
  email: Send,
  security: Shield,
  integrations: Boxes,
  "feature-flags": Star,
  regional: Landmark,
  advanced: FlaskConical,
};

export function PlatformSettingsNav() {
  const { t } = usePreferences();

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      "inline-flex items-center gap-1 rounded-[var(--exits-density-radius)] px-2 py-1 text-[length:var(--exits-text-sm)] font-medium transition-colors",
      isActive
        ? "bg-surface-muted text-foreground"
        : "text-muted hover:bg-surface-muted/70 hover:text-foreground",
    );

  return (
    <nav aria-label={t("settings.workspace.nav")} className="min-w-0 border-b border-border px-3 py-2">
      <ul className="flex flex-wrap gap-1">
        {SETTINGS_SECTIONS.map((section) => {
          const Icon = SETTINGS_TAB_ICONS[section.id];
          return (
            <li key={section.id}>
              <NavLink className={linkClass} end to={settingsSectionHref(section)}>
                {({ isActive }) => (
                  <>
                    <Icon
                      aria-hidden="true"
                      className={cn("size-3.5 shrink-0", isActive ? "text-primary" : undefined)}
                      strokeWidth={isActive ? 2.25 : 2}
                    />
                    <span>{t(section.navLabelKey)}</span>
                  </>
                )}
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
