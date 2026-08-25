import { Bell, Home, Mail, Users } from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

const items = [
  { to: "/", end: true, icon: Home, labelKey: "nav.home" as const },
  { to: "/personal/people", end: false, icon: Users, labelKey: "nav.people" as const },
  { to: "/personal/invitations", end: false, icon: Mail, labelKey: "nav.invitations" as const },
  { to: "/personal/notifications", end: false, icon: Bell, labelKey: "nav.notifications" as const },
];

export function PersonalShell() {
  const { t } = useI18n();

  return (
    <div className="flex min-h-dvh flex-col bg-background pt-[env(safe-area-inset-top)] pl-[env(safe-area-inset-left)] pr-[env(safe-area-inset-right)]">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-[var(--exits-z-notice)] focus:rounded-md focus:bg-surface focus:px-3 focus:py-2"
      >
        {t("app.skipToContent")}
      </a>
      <AppTopBar />
      <main
        id="main"
        className="mx-auto w-full max-w-5xl flex-1 px-4 py-5 pb-[calc(4.75rem+env(safe-area-inset-bottom))]"
      >
        <Outlet />
      </main>
      <nav
        aria-label={t("nav.personal")}
        className="fixed inset-x-0 bottom-0 z-[var(--exits-z-topbar)] border-t border-border bg-surface/95 backdrop-blur-sm"
      >
        <ul className="mx-auto grid max-w-5xl grid-cols-4 gap-1 px-2 pb-[max(0.5rem,env(safe-area-inset-bottom))] pt-2">
          {items.map((item) => {
            const Icon = item.icon;
            return (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    cn(
                      "flex min-h-[var(--exits-touch-target-min)] flex-col items-center justify-center gap-1 rounded-[var(--exits-radius-md)] px-1 text-[length:var(--exits-text-xs)] font-semibold text-muted",
                      isActive && "bg-surface-muted text-foreground",
                    )
                  }
                >
                  <Icon className="size-5" aria-hidden="true" />
                  <span className="truncate">{t(item.labelKey)}</span>
                </NavLink>
              </li>
            );
          })}
        </ul>
      </nav>
    </div>
  );
}
