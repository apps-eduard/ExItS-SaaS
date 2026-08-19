import { Home, Palette } from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { useI18n } from "@/i18n/I18nProvider";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { cn } from "@/lib/cn";

function NavItems({ className }: { className?: string }) {
  const { t } = useI18n();
  const items = [
    { to: "/", label: t("nav.home"), icon: Home, end: true },
    { to: "/appearance", label: t("nav.appearance"), icon: Palette, end: false },
  ] as const;

  return (
    <nav className={className} aria-label={t("app.name")}>
      {items.map(({ to, label, icon: Icon, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          className={({ isActive }) =>
            cn(
              "flex min-h-[var(--exits-touch-target-min)] min-w-[var(--exits-touch-target-min)] items-center gap-2 rounded-[var(--exits-radius-md)] px-3 py-2 font-semibold no-underline",
              isActive
                ? "bg-[var(--exits-primary-soft)] text-[var(--exits-primary)]"
                : "text-muted hover:bg-surface-muted hover:text-foreground",
            )
          }
        >
          <Icon className="size-5 shrink-0" aria-hidden="true" />
          <span>{label}</span>
        </NavLink>
      ))}
    </nav>
  );
}

export function AppShell() {
  const { t } = useI18n();
  const desktop = useMediaMin(1024);

  return (
    <div className="flex min-h-dvh bg-background" data-layout={desktop ? "desktop" : "phone"}>
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-[var(--exits-z-notice)] focus:rounded-md focus:bg-surface focus:px-3 focus:py-2"
      >
        {t("app.skipToContent")}
      </a>
      <aside
        data-density="compact"
        className="hidden w-60 shrink-0 border-r border-border bg-surface lg:flex lg:flex-col"
      >
        <div className="px-4 py-5">
          <p className="m-0 text-sm font-bold">{t("app.name")}</p>
          <p className="m-0 mt-1 text-xs text-muted">{t("shell.productPlaceholder")}</p>
        </div>
        <NavItems className="flex flex-col gap-1 px-3" />
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <AppTopBar />
        <main
          id="main"
          className="mx-auto w-full max-w-6xl flex-1 px-[var(--exits-page-padding)] py-5 pb-[calc(var(--exits-bottom-nav-height)+1.25rem)] lg:pb-8"
        >
          <Outlet />
        </main>
        <div
          data-density="compact"
          className="fixed inset-x-0 bottom-0 z-[var(--exits-z-nav)] border-t border-border bg-surface/95 backdrop-blur-sm lg:hidden"
        >
          <NavItems className="mx-auto flex max-w-6xl justify-around px-2 py-1 pb-[max(0.35rem,env(safe-area-inset-bottom))]" />
        </div>
      </div>
    </div>
  );
}
