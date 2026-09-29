import { NavLink } from "react-router-dom";
import { MoreHorizontal } from "lucide-react";
import { PERSONAL_PRIMARY_NAV_ITEMS } from "@/features/personal/personal-nav-config";
import { SHELL_DESKTOP_MIN_PX } from "@/features/shell/shell-breakpoints";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

const moreTab = {
  to: "/personal/more",
  end: false,
  labelKey: "personal.nav.more" as const,
  icon: MoreHorizontal,
  testId: "personal-nav-more",
};

/** Personal bottom nav — phone/tablet only; unmounted on desktop (>=1024). */
export function PersonalBottomNav() {
  const { t } = useI18n();
  const isDesktop = useMediaMin(SHELL_DESKTOP_MIN_PX);

  if (isDesktop) {
    return null;
  }

  const tabs = [...PERSONAL_PRIMARY_NAV_ITEMS, moreTab];

  return (
    <nav
      data-testid="personal-bottom-nav"
      aria-label={t("personal.nav.aria")}
      className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-surface pb-[env(safe-area-inset-bottom)] lg:hidden"
    >
      <ul className="mx-auto flex max-w-5xl items-stretch justify-between gap-1 px-2 pt-1">
        {tabs.map((tab) => {
          const Icon = tab.icon;
          return (
            <li key={tab.to} className="min-w-0 flex-1">
              <NavLink
                to={tab.to}
                end={tab.end}
                data-testid={tab.testId}
                className={({ isActive }) =>
                  cn(
                    "flex min-h-11 flex-col items-center justify-center gap-0.5 rounded-[var(--exits-radius-md)] px-1 py-1 text-center text-[length:var(--exits-text-xs)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                    isActive ? "font-semibold text-primary" : "font-medium text-muted hover:text-foreground",
                  )
                }
              >
                <Icon className="size-5 shrink-0" aria-hidden />
                <span className="max-w-full truncate">{t(tab.labelKey)}</span>
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
