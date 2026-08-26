import * as DialogPrimitive from "@radix-ui/react-dialog";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export function Sheet({
  trigger,
  title,
  children,
}: {
  trigger: ReactNode;
  title: string;
  children: ReactNode;
}) {
  return (
    <DialogPrimitive.Root>
      <DialogPrimitive.Trigger asChild>{trigger}</DialogPrimitive.Trigger>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-[var(--exits-z-overlay)] bg-[var(--exits-overlay)]" />
        <DialogPrimitive.Content
          className={cn(
            "fixed inset-y-0 right-0 z-[var(--exits-z-drawer)] w-full max-w-md border-l border-border bg-surface p-[var(--exits-density-card-padding)] shadow-lg",
          )}
        >
          <DialogPrimitive.Title className="text-[length:var(--exits-text-lg)] font-bold">
            {title}
          </DialogPrimitive.Title>
          <DialogPrimitive.Description className="mt-4 text-[length:var(--exits-text-sm)] text-muted">
            {children}
          </DialogPrimitive.Description>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
