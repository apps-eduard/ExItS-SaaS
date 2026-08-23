import { NavLink, useLocation, useNavigate } from "react-router-dom";
import {
  PLATFORM_SETTINGS_BASE_PATH,
  SETTINGS_SECTIONS,
  parsePlatformSettingsSection,
  settingsSectionHref,
} from "@/features/settings/settings-sections";
import { settingsControlClassName } from "@/features/settings/settings-form-utils";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function PlatformSettingsNav() {
  const { t } = usePreferences();
  const location = useLocation();
  const navigate = useNavigate();
  const current = parsePlatformSettingsSection(location.pathname);
  const currentSegment = current?.pathSegment ?? SETTINGS_SECTIONS[0]!.pathSegment;

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      "block min-w-0 rounded-[var(--exits-density-radius)] px-3 py-2 text-[length:var(--exits-text-sm)] font-medium whitespace-nowrap transition-colors lg:whitespace-normal",
      isActive
        ? "bg-surface-muted text-foreground shadow-sm lg:border-l-2 lg:border-primary lg:pl-[calc(0.75rem-2px)]"
        : "text-muted hover:bg-surface-muted/70 hover:text-foreground",
    );

  return (
    <nav aria-label={t("settings.workspace.nav")} className="min-w-0 max-w-full">
      <div className="md:hidden">
        <label className="grid gap-1" htmlFor="platform-settings-section">
          <span className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
            {t("settings.workspace.nav")}
          </span>
          <select
            className={settingsControlClassName}
            id="platform-settings-section"
            value={currentSegment}
            onChange={(event) => {
              navigate(`${PLATFORM_SETTINGS_BASE_PATH}/${event.target.value}`);
            }}
          >
            {SETTINGS_SECTIONS.map((section) => (
              <option key={section.id} value={section.pathSegment}>
                {t(section.navLabelKey)}
              </option>
            ))}
          </select>
        </label>
      </div>

      <ul className="hidden gap-1 overflow-x-auto overscroll-x-contain pb-1 [-ms-overflow-style:none] [scrollbar-width:none] md:flex lg:flex-col lg:gap-0.5 lg:overflow-visible lg:pb-0 [&::-webkit-scrollbar]:hidden">
        {SETTINGS_SECTIONS.map((section) => (
          <li key={section.id} className="shrink-0 lg:min-w-0 lg:shrink">
            <NavLink className={linkClass} end to={settingsSectionHref(section)}>
              {t(section.navLabelKey)}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
