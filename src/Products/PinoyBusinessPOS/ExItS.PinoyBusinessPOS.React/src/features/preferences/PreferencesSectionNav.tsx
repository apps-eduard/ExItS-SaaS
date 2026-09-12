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
 * Preferences section menu — link semantics (aria-current), not Tabs.
 * Desktop: compact vertical list. Mobile: single-row horizontal scroll.
 */
export function PreferencesSectionNav({ activeSection }: PreferencesSectionNavProps) {
  const { t } = useI18n();

  return (
    <nav
      aria-label={t("preferences.menuLabel")}
      data-testid="preferences-section-nav"
      data-active-section={activeSection}
      className="preferences-section-nav min-w-0"
    >
      <ul
        className={cn(
          "m-0 flex list-none gap-1 p-0",
          /* Mobile: one-row scroll */
          "flex-nowrap overflow-x-auto overscroll-x-contain pb-0.5 [scrollbar-width:thin]",
          /* Desktop (drawer wide enough): vertical rail */
          "sm:flex-col sm:overflow-visible sm:pb-0",
        )}
      >
        {PREFERENCES_SECTIONS.map((section) => {
          const Icon = section.icon;
          const to = preferencesSectionPath(section.id);
          return (
            <li key={section.id} className="shrink-0 sm:w-full">
              <NavLink
                to={to}
                data-testid={section.testId}
                className={({ isActive }) =>
                  cn(
                    "inline-flex w-full items-center gap-2 rounded-[var(--exits-radius-sm)]",
                    "px-2.5 py-2 text-start text-[length:var(--exits-text-sm)] font-medium",
                    "whitespace-nowrap no-underline transition-colors",
                    "duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
                    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)]",
                    "focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
                    isActive
                      ? "bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))] text-[var(--exits-primary)]"
                      : "text-foreground hover:bg-[var(--exits-surface-muted)]",
                  )
                }
              >
                <Icon className="size-4 shrink-0 opacity-90" aria-hidden strokeWidth={2} />
                <span className="min-w-0 truncate">{t(section.labelKey)}</span>
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
