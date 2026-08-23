import { useEffect, useRef, useState } from "react";
import { AppNav } from "@/components/exits/AppNav";
import {
  NavAccordionProvider,
  NavBulkAccordionToggle,
} from "@/components/exits/nav-accordion-context";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

const PEEK_CLOSE_DELAY_MS = 280;

export function AppSidebar() {
  const { t, sidebarCollapsed } = usePreferences();
  const [peekOpen, setPeekOpen] = useState(false);
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const iconRail = sidebarCollapsed && !peekOpen;
  const expanded = !sidebarCollapsed || peekOpen;

  function clearCloseTimer() {
    if (closeTimer.current) {
      clearTimeout(closeTimer.current);
      closeTimer.current = null;
    }
  }

  function openPeek() {
    if (!sidebarCollapsed) {
      return;
    }
    clearCloseTimer();
    setPeekOpen(true);
  }

  function schedulePeekClose() {
    if (!sidebarCollapsed) {
      return;
    }
    clearCloseTimer();
    closeTimer.current = setTimeout(() => setPeekOpen(false), PEEK_CLOSE_DELAY_MS);
  }

  useEffect(() => () => clearCloseTimer(), []);

  useEffect(() => {
    if (!sidebarCollapsed) {
      setPeekOpen(false);
    }
  }, [sidebarCollapsed]);

  return (
    <div
      className={cn(
        "relative shrink-0 transition-[width] duration-[var(--exits-motion-slow)] ease-[var(--exits-ease-emphasized)]",
        sidebarCollapsed ? "w-[4.25rem]" : "w-[15.5rem]",
      )}
      data-testid="app-sidebar-slot"
    >
      <aside
        data-sidebar-peek={peekOpen ? "true" : "false"}
        data-testid="app-sidebar"
        className={cn(
          "top-0 flex h-dvh flex-col overflow-hidden border-r border-border bg-surface",
          "transition-[width,box-shadow] duration-[var(--exits-motion-slow)] ease-[var(--exits-ease-emphasized)]",
          sidebarCollapsed ? "absolute left-0" : "relative",
          sidebarCollapsed
            ? cn(
                expanded
                  ? "z-[var(--exits-z-drawer)] w-[15.5rem] shadow-[var(--exits-shadow-lg)]"
                  : "z-auto w-[4.25rem]",
              )
            : "w-[15.5rem]",
        )}
        onPointerEnter={openPeek}
        onPointerLeave={schedulePeekClose}
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
                expanded ? "max-w-[12rem] opacity-100" : "max-w-0 opacity-0",
              )}
              aria-hidden={!expanded}
            >
              <div className="min-w-0 flex-1">
                <p className="truncate text-[length:var(--exits-text-sm)] font-semibold leading-tight">
                  ExItS
                </p>
                <p className="truncate text-[length:var(--exits-text-xs)] text-muted">
                  {t("auth.product")}
                </p>
              </div>
              {expanded ? <NavBulkAccordionToggle /> : null}
            </div>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto overflow-x-hidden">
            <AppNav collapsed={iconRail} railTooltipsEnabled={!sidebarCollapsed} />
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
