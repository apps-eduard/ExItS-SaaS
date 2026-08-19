import * as DropdownMenuPrimitive from "@radix-ui/react-dropdown-menu";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export function DropdownMenu({
  trigger,
  label,
  children,
  align = "start",
}: {
  trigger: ReactNode;
  label: string;
  children: ReactNode;
  align?: "start" | "end";
}) {
  return (
    <DropdownMenuPrimitive.Root>
      <DropdownMenuPrimitive.Trigger asChild>{trigger}</DropdownMenuPrimitive.Trigger>
      <DropdownMenuPrimitive.Portal>
        <DropdownMenuPrimitive.Content
          align={align}
          className={cn(
            "z-[var(--exits-z-dropdown)] min-w-44 rounded-md border border-border bg-surface-elevated p-1 shadow-sm",
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
  disabled,
}: {
  children: ReactNode;
  onSelect?: () => void;
  disabled?: boolean;
}) {
  return (
    <DropdownMenuPrimitive.Item
      disabled={disabled}
      className="flex min-h-9 cursor-pointer items-center gap-2 rounded-sm px-2 text-[length:var(--exits-text-sm)] outline-none focus:bg-surface-muted data-disabled:cursor-default data-disabled:opacity-100"
      onSelect={onSelect}
    >
      {children}
    </DropdownMenuPrimitive.Item>
  );
}

export function DropdownMenuSeparator() {
  return <DropdownMenuPrimitive.Separator className="my-1 h-px bg-border" />;
}
