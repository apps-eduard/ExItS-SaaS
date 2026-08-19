import { PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { Button } from "@/components/ui/button";
import { AppNav } from "@/components/exits/AppNav";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function AppSidebar() {
  const { t, sidebarCollapsed, setSidebarCollapsed } = usePreferences();

  return (
    <aside
      className={cn(
        "flex h-dvh shrink-0 flex-col border-r border-border bg-surface",
        "transition-[width] duration-[var(--exits-motion-base)] ease-[var(--exits-ease)]",
        sidebarCollapsed ? "w-[4.25rem]" : "w-[15.25rem]",
      )}
    >
      <div className="flex min-h-12 items-center justify-between gap-2 border-b border-border px-2.5">
        {sidebarCollapsed ? (
          <span className="grid size-8 place-items-center rounded-md bg-primary text-[length:var(--exits-text-xs)] font-bold text-primary-foreground">
            Ex
          </span>
        ) : (
          <div className="flex min-w-0 items-center gap-2">
            <span className="grid size-8 shrink-0 place-items-center rounded-md bg-primary text-[length:var(--exits-text-xs)] font-bold text-primary-foreground">
              Ex
            </span>
            <div className="min-w-0">
              <p className="truncate text-sm font-bold">ExItS</p>
              <p className="truncate text-[length:var(--exits-text-xs)] text-muted">
                {t("auth.product")}
              </p>
            </div>
          </div>
        )}
        <Button
          type="button"
          variant="ghost"
          size="sm"
          aria-pressed={sidebarCollapsed}
          aria-label={sidebarCollapsed ? t("shell.expandSidebar") : t("shell.collapseSidebar")}
          onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
        >
          {sidebarCollapsed ? (
            <PanelLeftOpen aria-hidden="true" size={18} />
          ) : (
            <PanelLeftClose aria-hidden="true" size={18} />
          )}
        </Button>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        <AppNav collapsed={sidebarCollapsed} />
      </div>
    </aside>
  );
}
