import { NavLink } from "react-router-dom";
import { CheckSquare, Home, ListOrdered, MoreHorizontal, Wallet } from "lucide-react";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

const tabs = [
  {
    to: "/personal",
    end: true,
    labelKey: "personal.nav.home",
    icon: Home,
    testId: "personal-nav-home",
  },
  {
    to: "/personal/utang",
    end: false,
    labelKey: "personal.nav.utang",
    icon: Wallet,
    testId: "personal-nav-utang",
  },
  {
    to: "/personal/todo",
    end: false,
    labelKey: "personal.nav.todo",
    icon: CheckSquare,
    testId: "personal-nav-todo",
  },
  {
    to: "/personal/orders",
    end: false,
    labelKey: "personal.nav.orders",
    icon: ListOrdered,
    testId: "personal-nav-orders",
  },
  {
    to: "/personal/more",
    end: false,
    labelKey: "personal.nav.more",
    icon: MoreHorizontal,
    testId: "personal-nav-more",
  },
] as const;

export function PersonalBottomNav() {
  const { t } = useI18n();

  return (
    <nav
      data-testid="personal-bottom-nav"
      aria-label={t("personal.nav.aria")}
      className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-surface pb-[env(safe-area-inset-bottom)]"
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
