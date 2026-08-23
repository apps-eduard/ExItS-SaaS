import * as DialogPrimitive from "@radix-ui/react-dialog";
import { AppNav } from "@/components/exits/AppNav";
import {
  NavAccordionProvider,
  NavBulkAccordionToggle,
} from "@/components/exits/nav-accordion-context";
import { usePreferences } from "@/hooks/use-preferences";

export function NavigationDrawer({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { t } = usePreferences();

  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-[var(--exits-z-overlay)] bg-[var(--exits-overlay)]" />
        <DialogPrimitive.Content className="fixed inset-y-0 left-0 z-[var(--exits-z-drawer)] flex w-[min(20rem,100%)] flex-col border-r border-border bg-surface shadow-lg outline-none">
          <NavAccordionProvider>
            <DialogPrimitive.Title className="flex items-center gap-2 border-b border-border px-3 py-3 text-[length:var(--exits-text-sm)] font-semibold">
              <span className="grid size-7 shrink-0 place-items-center rounded-md bg-primary text-[11px] font-bold text-primary-foreground">
                Ex
              </span>
              <span className="min-w-0 flex-1">
                ExItS
                <span className="mt-0.5 block text-[length:var(--exits-text-xs)] font-normal text-muted">
                  {t("auth.product")}
                </span>
              </span>
              <NavBulkAccordionToggle />
            </DialogPrimitive.Title>
            <DialogPrimitive.Description className="sr-only">
              {t("shell.openNavigation")}
            </DialogPrimitive.Description>
            <div className="min-h-0 flex-1 overflow-y-auto">
              <AppNav collapsed={false} onNavigate={() => onOpenChange(false)} />
            </div>
          </NavAccordionProvider>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
