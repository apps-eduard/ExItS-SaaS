import * as DialogPrimitive from "@radix-ui/react-dialog";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export function ConfirmActionDialog({
  open,
  title,
  description,
  confirmLabel,
  cancelLabel,
  pendingLabel,
  destructive = false,
  pending = false,
  confirmDisabled = false,
  error,
  children,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  cancelLabel: string;
  pendingLabel: string;
  destructive?: boolean;
  pending?: boolean;
  confirmDisabled?: boolean;
  error?: ReactNode;
  children?: ReactNode;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  return (
    <DialogPrimitive.Root
      open={open}
      onOpenChange={(next) => {
        if (!next && !pending) {
          onCancel();
        }
      }}
    >
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-[var(--exits-z-overlay)] bg-[var(--exits-overlay)]" />
        <DialogPrimitive.Content
          className={cn(
            "fixed left-1/2 top-1/2 z-[var(--exits-z-drawer)] w-[calc(100%-1.5rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-[var(--exits-density-card-padding)] shadow-lg",
          )}
          onEscapeKeyDown={(event) => {
            if (pending) {
              event.preventDefault();
            }
          }}
        >
          <DialogPrimitive.Title className="text-[length:var(--exits-text-lg)] font-bold">
            {title}
          </DialogPrimitive.Title>
          <DialogPrimitive.Description className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {description}
          </DialogPrimitive.Description>
          {children ? <div className="mt-3 grid gap-2">{children}</div> : null}
          {error}
          <div className="mt-4 flex flex-wrap justify-end gap-2">
            <Button type="button" variant="outline" disabled={pending} onClick={onCancel}>
              {cancelLabel}
            </Button>
            <Button
              type="button"
              variant={destructive ? "destructive" : "default"}
              disabled={pending || confirmDisabled}
              aria-busy={pending}
              onClick={onConfirm}
            >
              {pending ? pendingLabel : confirmLabel}
            </Button>
          </div>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
