import * as DialogPrimitive from "@radix-ui/react-dialog";
import { AppNav } from "@/components/exits/AppNav";
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
          <DialogPrimitive.Title className="border-b border-border px-4 py-3 text-[length:var(--exits-text-lg)] font-bold">
            {t("app.title")}
          </DialogPrimitive.Title>
          <DialogPrimitive.Description className="sr-only">
            {t("shell.openNavigation")}
          </DialogPrimitive.Description>
          <div className="min-h-0 flex-1 overflow-y-auto">
            <AppNav collapsed={false} onNavigate={() => onOpenChange(false)} />
          </div>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
