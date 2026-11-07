import { CheckSquare, Home, MoreHorizontal, Wallet } from "lucide-react";
import { LayoutGroup, motion, useReducedMotion } from "motion/react";
import { useTranslation } from "react-i18next";
import { Link, useLocation } from "react-router-dom";
import { useUnavailableNotice } from "@/features/home/UnavailableAction";
import { springSnappy } from "@/motion/presets";
import { cn } from "@/lib/utils";
import { useSession } from "@/session/SessionProvider";

function MobileNav() {
  const { t } = useTranslation();
  const location = useLocation();
  const { session } = useSession();
  const notify = useUnavailableNotice();
  const reduced = useReducedMotion();
  const isPersonal =
    (session?.accountClass ?? "").toLowerCase() === "personal" &&
    !session?.organizationContextLocked;
  const onHome = location.pathname === "/";

  if (!isPersonal) {
    return (
      <nav
        className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-card/95 px-3 pt-1.5 pb-[max(0.5rem,env(safe-area-inset-bottom))] backdrop-blur-sm supports-[backdrop-filter]:bg-card/90 lg:hidden"
        aria-label={t("nav.mobile")}
      >
        <ul className="mx-auto grid max-w-md grid-cols-1 gap-1">
          <li>
            <Link
              to="/"
              aria-current={onHome ? "page" : undefined}
              className={cn(
                "relative flex min-h-12 flex-col items-center justify-center gap-0.5 rounded-xl px-1 text-[11px] font-medium transition-colors duration-150",
                onHome
                  ? "text-primary"
                  : "text-muted-foreground",
              )}
            >
              {onHome && !reduced ? (
                <motion.span
                  layoutId="mobile-nav-active"
                  className="absolute inset-0 rounded-xl bg-primary/10"
                  transition={springSnappy}
                />
              ) : onHome ? (
                <span className="absolute inset-0 rounded-xl bg-primary/10" />
              ) : null}
              <Home className="relative size-[1.125rem]" strokeWidth={2} aria-hidden />
              <span className="relative">{t("nav.home")}</span>
            </Link>
          </li>
        </ul>
      </nav>
    );
  }

  const items = [
    { id: "home", label: t("nav.home"), icon: Home, available: true },
    { id: "utang", label: t("nav.utang"), icon: Wallet, available: false },
    { id: "todo", label: t("nav.todo"), icon: CheckSquare, available: false },
    { id: "more", label: t("nav.more"), icon: MoreHorizontal, available: false },
  ] as const;

  return (
    <nav
      className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-card/95 px-2 pt-1 pb-[max(0.35rem,env(safe-area-inset-bottom))] supports-[backdrop-filter]:bg-card/92 lg:hidden"
      aria-label={t("nav.mobile")}
    >
      <LayoutGroup id="mobile-nav">
        <ul className="mx-auto grid max-w-md grid-cols-4 gap-0.5">
          {items.map((item) => {
            const Icon = item.icon;
            const isActive = item.id === "home" && onHome;
            if (!item.available) {
              return (
                <li key={item.id}>
                  <button
                    type="button"
                    onClick={notify}
                    className="flex min-h-11 w-full flex-col items-center justify-center gap-0.5 rounded-lg px-1 text-[11px] font-medium text-muted-foreground/65 transition-opacity duration-150 active:opacity-70"
                    title={t("homeData.quickActionUnavailable")}
                  >
                    <Icon className="size-[1.125rem]" strokeWidth={2} aria-hidden />
                    {item.label}
                  </button>
                </li>
              );
            }
            return (
              <li key={item.id}>
                <Link
                  to="/"
                  aria-current={isActive ? "page" : undefined}
                  className={cn(
                    "relative flex min-h-11 flex-col items-center justify-center gap-0.5 rounded-lg px-1 text-[11px] font-semibold transition-colors duration-150 active:opacity-80",
                    isActive
                      ? "text-primary"
                      : "text-muted-foreground hover:text-foreground",
                  )}
                >
                  {isActive && !reduced ? (
                    <motion.span
                      layoutId="mobile-nav-active"
                      className="absolute inset-x-1 inset-y-0.5 rounded-lg bg-primary/10"
                      transition={springSnappy}
                    />
                  ) : isActive ? (
                    <span className="absolute inset-x-1 inset-y-0.5 rounded-lg bg-primary/10" />
                  ) : null}
                  <Icon
                    className="relative size-[1.125rem]"
                    strokeWidth={isActive ? 2.25 : 2}
                    aria-hidden
                  />
                  <span className="relative">{item.label}</span>
                </Link>
              </li>
            );
          })}
        </ul>
      </LayoutGroup>
    </nav>
  );
}

export { MobileNav };
