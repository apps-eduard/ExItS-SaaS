import { CheckSquare, Home, MoreHorizontal, Wallet } from "lucide-react";
import { LayoutGroup, motion, useReducedMotion } from "motion/react";
import { NavLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ExitsLogo } from "@/components/brand/exits-logo";
import { useUnavailableNotice } from "@/features/home/UnavailableAction";
import { springSnappy } from "@/motion/presets";
import { cn } from "@/lib/utils";
import { useSession } from "@/session/SessionProvider";

function Sidebar() {
  const { t } = useTranslation();
  const { session } = useSession();
  const notify = useUnavailableNotice();
  const reduced = useReducedMotion();
  const isPersonal =
    (session?.accountClass ?? "").toLowerCase() === "personal" &&
    !session?.organizationContextLocked;

  const personalItems = [
    { id: "home", label: t("nav.home"), to: "/", available: true, icon: Home },
    { id: "utang", label: t("nav.utang"), available: false, icon: Wallet },
    { id: "todo", label: t("nav.todo"), available: false, icon: CheckSquare },
    { id: "more", label: t("nav.more"), available: false, icon: MoreHorizontal },
  ] as const;

  return (
    <aside className="hidden w-60 shrink-0 flex-col border-r border-[var(--sidebar-border)] bg-[var(--sidebar)] lg:flex">
      <div className="flex h-14 items-center px-4 md:px-5">
        <ExitsLogo />
      </div>
      <nav className="flex flex-1 flex-col gap-0.5 px-2.5 py-2" aria-label="Primary">
        <LayoutGroup id="sidebar-nav">
          {(isPersonal
            ? personalItems
            : ([{ id: "home", label: t("nav.home"), to: "/", available: true, icon: Home }] as const)
          ).map((item) => {
            const Icon = item.icon;
            if (!item.available) {
              return (
                <button
                  key={item.id}
                  type="button"
                  onClick={notify}
                  className="inline-flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2 text-left text-sm font-medium text-muted-foreground/55 transition-colors duration-150 hover:bg-[var(--sidebar-accent)]"
                  title={t("homeData.quickActionUnavailable")}
                >
                  <Icon className="size-4" aria-hidden />
                  {item.label}
                </button>
              );
            }
            return (
              <NavLink
                key={item.id}
                to={item.to}
                end
                className={({ isActive }) =>
                  cn(
                    "relative inline-flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors duration-150",
                    isActive
                      ? "text-primary"
                      : "text-[var(--sidebar-foreground)] hover:bg-[var(--sidebar-accent)]",
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && !reduced ? (
                      <motion.span
                        layoutId="sidebar-nav-active"
                        className="absolute inset-0 rounded-lg bg-[var(--sidebar-accent)]"
                        transition={springSnappy}
                      />
                    ) : isActive ? (
                      <span className="absolute inset-0 rounded-lg bg-[var(--sidebar-accent)]" />
                    ) : null}
                    <Icon className="relative size-4" aria-hidden />
                    <span className="relative">{item.label}</span>
                  </>
                )}
              </NavLink>
            );
          })}
        </LayoutGroup>
      </nav>
      <div className="space-y-2 border-t border-[var(--sidebar-border)] px-4 py-4">
        <p className="text-[10px] font-semibold uppercase tracking-[0.1em] text-muted-foreground">
          {t("nav.developer")}
        </p>
        <NavLink
          to="/design-system"
          className={({ isActive }) =>
            cn(
              "inline-flex w-full items-center rounded-lg px-2.5 py-2 text-xs font-medium transition-colors duration-150",
              isActive
                ? "bg-[var(--sidebar-accent)] text-primary"
                : "text-muted-foreground hover:bg-[var(--sidebar-accent)] hover:text-foreground",
            )
          }
        >
          {t("nav.designSystem")}
        </NavLink>
      </div>
    </aside>
  );
}

export { Sidebar };
