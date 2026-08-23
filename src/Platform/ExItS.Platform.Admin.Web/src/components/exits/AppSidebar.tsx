import { AppNav } from "@/components/exits/AppNav";
import {
  NavAccordionProvider,
  NavBulkAccordionToggle,
} from "@/components/exits/nav-accordion-context";
import { SIDEBAR_ICON_RAIL_WIDTH_CLASS } from "@/components/exits/nav-item-styles";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function AppSidebar() {
  const { t, sidebarCollapsed } = usePreferences();
  const iconRail = sidebarCollapsed;

  return (
    <div
      className={cn(
        "relative shrink-0 transition-[width] duration-[var(--exits-motion-slow)] ease-[var(--exits-ease-emphasized)]",
        sidebarCollapsed ? SIDEBAR_ICON_RAIL_WIDTH_CLASS : "w-[15.5rem]",
      )}
      data-testid="app-sidebar-slot"
    >
      <aside
        data-testid="app-sidebar"
        className={cn(
          "relative flex h-dvh flex-col overflow-hidden border-r border-border bg-surface",
          "transition-[width] duration-[var(--exits-motion-slow)] ease-[var(--exits-ease-emphasized)]",
          sidebarCollapsed ? SIDEBAR_ICON_RAIL_WIDTH_CLASS : "w-[15.5rem]",
        )}
      >
        <NavAccordionProvider>
          <div
            className={cn(
              "flex h-12 shrink-0 items-center gap-2 border-b border-border px-3",
              iconRail && "justify-center px-2",
            )}
          >
            <span className="grid size-8 shrink-0 place-items-center rounded-lg bg-primary text-[11px] font-bold text-primary-foreground shadow-sm">
              Ex
            </span>
            <div
              className={cn(
                "flex min-w-0 flex-1 items-center gap-2 overflow-hidden whitespace-nowrap transition-[opacity,max-width] duration-[var(--exits-motion-base)] ease-[var(--exits-ease-emphasized)]",
                iconRail ? "max-w-0 opacity-0" : "max-w-[12rem] opacity-100",
              )}
              aria-hidden={iconRail}
            >
              <div className="min-w-0 flex-1">
                <p className="truncate text-[length:var(--exits-text-sm)] font-semibold leading-tight">
                  ExItS
                </p>
                <p className="truncate text-[length:var(--exits-text-xs)] text-muted">
                  {t("auth.product")}
                </p>
              </div>
              {iconRail ? null : <NavBulkAccordionToggle />}
            </div>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto overflow-x-hidden">
            <AppNav collapsed={iconRail} railTooltipsEnabled={iconRail} />
          </div>
          {iconRail ? (
            <div className="flex shrink-0 justify-center border-t border-border/70 px-2 py-2">
              <NavBulkAccordionToggle />
            </div>
          ) : null}
        </NavAccordionProvider>
      </aside>
    </div>
  );
}
