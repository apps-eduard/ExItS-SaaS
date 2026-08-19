import { AppNav } from "@/components/exits/AppNav";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function AppSidebar() {
  const { t, sidebarCollapsed } = usePreferences();

  return (
    <aside
      className={cn(
        "flex h-dvh shrink-0 flex-col border-r border-border bg-surface",
        "transition-[width] duration-[var(--exits-motion-base)] ease-[var(--exits-ease)]",
        sidebarCollapsed ? "w-16" : "w-[15.5rem]",
      )}
    >
      <div className="flex h-12 items-center gap-2 border-b border-border px-3">
        <span className="grid size-7 shrink-0 place-items-center rounded-md bg-primary text-[11px] font-bold text-primary-foreground">
          Ex
        </span>
        {sidebarCollapsed ? null : (
          <div className="min-w-0 flex-1">
            <p className="truncate text-[length:var(--exits-text-sm)] font-semibold leading-tight">
              ExItS
            </p>
            <p className="truncate text-[length:var(--exits-text-xs)] text-muted">
              {t("auth.product")}
            </p>
          </div>
        )}
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        <AppNav collapsed={sidebarCollapsed} />
      </div>
    </aside>
  );
}
