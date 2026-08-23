import { AppNav } from "@/components/exits/AppNav";
import {
  NavAccordionProvider,
  NavBulkAccordionToggle,
} from "@/components/exits/nav-accordion-context";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function AppSidebar() {
  const { t, sidebarCollapsed } = usePreferences();

  return (
    <aside
      className={cn(
        "flex h-dvh shrink-0 flex-col border-r border-border bg-surface",
        "transition-[width] duration-[var(--exits-motion-slow)] ease-[var(--exits-ease-emphasized)]",
        sidebarCollapsed ? "w-[4.25rem]" : "w-[15.5rem]",
      )}
    >
      <NavAccordionProvider>
        <div
          className={cn(
            "flex h-12 shrink-0 items-center gap-2 border-b border-border px-3",
            sidebarCollapsed && "justify-center px-2",
          )}
        >
          <span className="grid size-8 shrink-0 place-items-center rounded-lg bg-primary text-[11px] font-bold text-primary-foreground shadow-sm transition-transform duration-[var(--exits-motion-base)] ease-[var(--exits-ease)]">
            Ex
          </span>
          {sidebarCollapsed ? null : (
            <div className="flex min-w-0 flex-1 items-center gap-2 overflow-hidden">
              <div className="min-w-0 flex-1">
                <p className="truncate text-[length:var(--exits-text-sm)] font-semibold leading-tight">
                  ExItS
                </p>
                <p className="truncate text-[length:var(--exits-text-xs)] text-muted">
                  {t("auth.product")}
                </p>
              </div>
              <NavBulkAccordionToggle />
            </div>
          )}
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto overflow-x-hidden">
          <AppNav collapsed={sidebarCollapsed} />
        </div>
      </NavAccordionProvider>
    </aside>
  );
}
