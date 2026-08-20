import { useEffect, useId, useRef, useState, type KeyboardEvent, type ReactNode } from "react";
import { cn } from "@/lib/cn";

type MenuAlign = "start" | "end";

export function useDismissibleOpen(initial = false) {
  const [open, setOpen] = useState(initial);
  return { open, setOpen, close: () => setOpen(false), toggle: () => setOpen((value) => !value) };
}

type DropdownMenuProps = {
  align?: MenuAlign;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  trigger: (props: {
    id: string;
    expanded: boolean;
    controls: string;
    onClick: () => void;
    onKeyDown: (event: KeyboardEvent<HTMLButtonElement>) => void;
  }) => ReactNode;
  children: ReactNode;
  className?: string;
  menuLabel?: string;
};

/** Accessible dropdown menu — Escape, outside click, and focus return to trigger. */
export function DropdownMenu({
  align = "end",
  open,
  onOpenChange,
  trigger,
  children,
  className,
  menuLabel,
}: DropdownMenuProps) {
  const triggerId = useId();
  const menuId = useId();
  const rootRef = useRef<HTMLDivElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    function onPointerDown(event: MouseEvent | PointerEvent) {
      const target = event.target as Node | null;
      if (!target || !rootRef.current?.contains(target)) {
        onOpenChange(false);
      }
    }

    function onKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        onOpenChange(false);
        triggerRef.current?.focus();
      }
    }

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [onOpenChange, open]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const firstItem = menuRef.current?.querySelector<HTMLElement>(
      '[role="menuitem"]:not([disabled]), [role="option"]:not([aria-disabled="true"])',
    );
    firstItem?.focus();
  }, [open]);

  function onTriggerKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (event.key === "ArrowDown" || event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      onOpenChange(true);
    }
  }

  return (
    <div ref={rootRef} className={cn("relative inline-flex", className)}>
      {trigger({
        id: triggerId,
        expanded: open,
        controls: menuId,
        onClick: () => onOpenChange(!open),
        onKeyDown: onTriggerKeyDown,
      })}
      {open ? (
        <div
          ref={(node) => {
            menuRef.current = node;
            const previous = rootRef.current?.querySelector<HTMLElement>(
              `#${CSS.escape(triggerId)}`,
            );
            if (previous) {
              triggerRef.current = previous;
            }
          }}
          id={menuId}
          role="menu"
          aria-labelledby={triggerId}
          aria-label={menuLabel}
          className={cn(
            "absolute top-[calc(100%+0.35rem)] z-40 min-w-[14rem] max-w-[min(20rem,calc(100vw-1.5rem))] overflow-hidden rounded-[var(--exits-radius-md)] border border-border bg-surface py-1 shadow-[0_8px_24px_rgba(20,32,26,0.12)]",
            align === "end" ? "right-0" : "left-0",
          )}
        >
          {children}
        </div>
      ) : null}
    </div>
  );
}

export function MenuItem({
  children,
  onSelect,
  destructive = false,
  disabled = false,
}: {
  children: ReactNode;
  onSelect: () => void;
  destructive?: boolean;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      role="menuitem"
      disabled={disabled}
      className={cn(
        "flex w-full items-center gap-2 px-3 py-2.5 text-left text-[length:var(--exits-text-sm)] font-medium transition-colors duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:bg-[var(--exits-surface-muted)]",
        destructive
          ? "text-destructive hover:bg-[var(--exits-surface-muted)]"
          : "text-foreground hover:bg-[var(--exits-surface-muted)]",
        disabled && "opacity-50",
      )}
      onClick={() => {
        if (!disabled) {
          onSelect();
        }
      }}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          if (!disabled) {
            onSelect();
          }
        }
      }}
    >
      {children}
    </button>
  );
}

export function MenuSeparator() {
  return <div role="separator" className="my-1 h-px bg-border" />;
}

export function MenuHeader({ children }: { children: ReactNode }) {
  return <div className="border-b border-border px-3 py-2.5">{children}</div>;
}
