import * as DropdownMenuPrimitive from "@radix-ui/react-dropdown-menu";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export function DropdownMenu({
  trigger,
  label,
  children,
}: {
  trigger: ReactNode;
  label: string;
  children: ReactNode;
}) {
  return (
    <DropdownMenuPrimitive.Root>
      <DropdownMenuPrimitive.Trigger asChild>{trigger}</DropdownMenuPrimitive.Trigger>
      <DropdownMenuPrimitive.Portal>
        <DropdownMenuPrimitive.Content
          align="start"
          className={cn(
            "z-[var(--exits-z-dropdown)] min-w-40 rounded-md border border-border bg-surface-elevated p-1 shadow-md",
          )}
        >
          <DropdownMenuPrimitive.Label className="px-2 py-1.5 text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {label}
          </DropdownMenuPrimitive.Label>
          {children}
        </DropdownMenuPrimitive.Content>
      </DropdownMenuPrimitive.Portal>
    </DropdownMenuPrimitive.Root>
  );
}

export function DropdownMenuItem({
  children,
  onSelect,
}: {
  children: ReactNode;
  onSelect?: () => void;
}) {
  return (
    <DropdownMenuPrimitive.Item
      className="flex min-h-11 cursor-pointer items-center rounded-sm px-2 text-[length:var(--exits-text-sm)] outline-none focus:bg-surface-muted"
      onSelect={onSelect}
    >
      {children}
    </DropdownMenuPrimitive.Item>
  );
}
