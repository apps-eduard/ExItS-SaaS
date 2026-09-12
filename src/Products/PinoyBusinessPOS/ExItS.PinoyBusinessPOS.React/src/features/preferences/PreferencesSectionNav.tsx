import { NavLink } from "react-router-dom";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";
import {
  PREFERENCES_SECTIONS,
  preferencesSectionPath,
  type PreferencesSectionId,
} from "@/features/preferences/preferences-sections";

type PreferencesSectionNavProps = {
  activeSection: PreferencesSectionId;
};

/**
 * Preferences section icon top nav — link semantics (aria-current), not Tabs.
 * Icon-only + title tooltip + aria-label. Shared on desktop and mobile.
 */
export function PreferencesSectionNav({ activeSection }: PreferencesSectionNavProps) {
  const { t } = useI18n();

  return (
    <nav
      aria-label={t("preferences.menuLabel")}
      data-testid="preferences-section-nav"
      data-active-section={activeSection}
      data-variant="icon-top"
      className="preferences-section-nav min-w-0 border-b border-border pb-3"
    >
      <ul className="m-0 flex list-none flex-nowrap items-center justify-start gap-1 p-0">
        {PREFERENCES_SECTIONS.map((section) => {
          const Icon = section.icon;
          const label = t(section.labelKey);
          const to = preferencesSectionPath(section.id);
          return (
            <li key={section.id} className="shrink-0">
              <NavLink
                to={to}
                data-testid={section.testId}
                aria-label={label}
                title={label}
                className={({ isActive }) =>
                  cn(
                    "inline-flex size-[var(--exits-control-height)] min-h-[var(--exits-control-height)] min-w-[var(--exits-control-height)]",
                    "items-center justify-center rounded-full border border-transparent",
                    "no-underline transition-[background-color,border-color,color,box-shadow]",
                    "duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
                    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)]",
                    "focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
                    isActive
                      ? "border-[color-mix(in_srgb,var(--exits-primary)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))] text-[var(--exits-primary)]"
                      : "bg-transparent text-foreground hover:bg-[var(--exits-surface-muted)]",
                  )
                }
              >
                <Icon className="size-4 shrink-0 opacity-90" aria-hidden strokeWidth={2} />
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
