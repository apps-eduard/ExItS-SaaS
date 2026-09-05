import type { ReactNode } from "react";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export function AppShell({
  children,
  header,
  withOrgBottomNav = false,
  sellFloor = false,
}: {
  children: ReactNode;
  header?: ReactNode;
  /** Reserve space for fixed org bottom nav. */
  withOrgBottomNav?: boolean;
  /**
   * Sell floor uses a near-fullscreen shell. Prefer an explicit class over CSS `:has()`
   * so desktop style recalculation does not thrash on every DOM mutation.
   */
  sellFloor?: boolean;
}) {
  const { t } = useI18n();

  return (
    <div
      className={cn(
        "app-shell mx-auto flex min-h-[100dvh] w-full max-w-5xl min-w-0 flex-col overflow-x-hidden px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[env(safe-area-inset-top)]",
        sellFloor && "app-shell--sell-floor",
        withOrgBottomNav
          ? "pb-[max(5.5rem,calc(4.25rem+env(safe-area-inset-bottom)))]"
          : "pb-[max(2rem,env(safe-area-inset-bottom))]",
      )}
    >
      <a
        href="#main-content"
        className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
      >
        {t("app.skipToContent")}
      </a>
      {header}
      <main
        id="main-content"
        className={cn(
          "flex min-w-0 flex-1 flex-col",
          sellFloor ? "min-h-0 gap-0 pt-2" : "gap-4 pt-6",
        )}
        tabIndex={-1}
      >
        {children}
      </main>
    </div>
  );
}
