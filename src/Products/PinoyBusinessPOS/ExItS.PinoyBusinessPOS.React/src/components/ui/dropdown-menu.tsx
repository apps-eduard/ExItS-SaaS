import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type ButtonHTMLAttributes,
  type CSSProperties,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { cn } from "@/lib/cn";

type MenuAlign = "start" | "end";

const DEFAULT_COLLISION_PADDING = 10;
/** Below dialogs (z-80); above sticky table chrome (z-2) and elevated surfaces. */
const DROPDOWN_Z_INDEX = 70;

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
  /** Applied to the floating menu surface (portaled when portal=true). */
  menuClassName?: string;
  menuLabel?: string;
  /**
   * Render menu through a document.body portal with fixed positioning.
   * Avoids clipping by ancestor overflow (e.g. ExitsTable scroll viewport).
   * Default true.
   */
  portal?: boolean;
  /** Viewport collision padding in px (default 10). */
  collisionPadding?: number;
};

type MenuCoords = {
  top: number;
  left: number;
  maxHeight: number;
  placement: "below" | "above";
};

function resolveTriggerElement(root: HTMLElement | null, triggerId: string): HTMLElement | null {
  if (!root) return null;
  return (
    root.querySelector<HTMLElement>(`#${CSS.escape(triggerId)}`) ??
    root.querySelector<HTMLElement>("[aria-haspopup='menu']")
  );
}

function computeMenuPosition(options: {
  trigger: HTMLElement;
  menu: HTMLElement;
  align: MenuAlign;
  collisionPadding: number;
}): MenuCoords {
  const { trigger, menu, align, collisionPadding: pad } = options;
  const rect = trigger.getBoundingClientRect();
  const menuWidth = menu.offsetWidth;
  const menuHeight = menu.offsetHeight;
  const viewportW = window.innerWidth;
  const viewportH = window.innerHeight;
  const gap = 6;

  const spaceBelow = viewportH - rect.bottom - pad;
  const spaceAbove = rect.top - pad;
  const preferAbove = spaceBelow < menuHeight + gap && spaceAbove > spaceBelow;
  const placement: "below" | "above" = preferAbove ? "above" : "below";

  const maxHeight = Math.max(
    8 * 16,
    Math.floor((preferAbove ? spaceAbove : spaceBelow) - gap),
  );

  let top =
    placement === "above" ? rect.top - Math.min(menuHeight, maxHeight) - gap : rect.bottom + gap;
  top = Math.max(pad, Math.min(top, viewportH - Math.min(menuHeight, maxHeight) - pad));

  const isRtl = getComputedStyle(trigger).direction === "rtl";
  let left: number;
  if (align === "end") {
    left = isRtl ? rect.left : rect.right - menuWidth;
  } else {
    left = isRtl ? rect.right - menuWidth : rect.left;
  }
  left = Math.max(pad, Math.min(left, viewportW - menuWidth - pad));

  return { top, left, maxHeight, placement };
}

function isTriggerInViewport(trigger: HTMLElement, pad: number): boolean {
  const rect = trigger.getBoundingClientRect();
  // Unlaid-out / jsdom: zero box — do not treat as scrolled away.
  if (rect.width === 0 && rect.height === 0) {
    return true;
  }
  return (
    rect.bottom > pad &&
    rect.top < window.innerHeight - pad &&
    rect.right > pad &&
    rect.left < window.innerWidth - pad
  );
}

/** Accessible dropdown menu — Escape, outside click, portal overlay, and focus return. */
export function DropdownMenu({
  align = "end",
  open,
  onOpenChange,
  trigger,
  children,
  className,
  menuClassName,
  menuLabel,
  portal = true,
  collisionPadding = DEFAULT_COLLISION_PADDING,
}: DropdownMenuProps) {
  const triggerId = useId();
  const menuId = useId();
  const rootRef = useRef<HTMLDivElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLElement | null>(null);
  const [coords, setCoords] = useState<MenuCoords | null>(null);

  function syncTriggerRef() {
    const el = resolveTriggerElement(rootRef.current, triggerId);
    if (el) {
      triggerRef.current = el;
    }
    return el;
  }

  function reposition() {
    const triggerEl = syncTriggerRef();
    const menuEl = menuRef.current;
    if (!triggerEl || !menuEl) return;

    if (!isTriggerInViewport(triggerEl, collisionPadding)) {
      onOpenChange(false);
      return;
    }

    setCoords(computeMenuPosition({ trigger: triggerEl, menu: menuEl, align, collisionPadding }));
  }

  useLayoutEffect(() => {
    if (!open) {
      setCoords(null);
      return;
    }
    syncTriggerRef();
    reposition();
    // Position after open/align change; content height changes are handled on scroll/resize.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- avoid re-running on new children identity each render
  }, [open, align, collisionPadding]);

  useEffect(() => {
    if (!open) {
      return;
    }

    function onPointerDown(event: MouseEvent | PointerEvent) {
      const target = event.target as Node | null;
      if (!target) {
        onOpenChange(false);
        return;
      }
      const inTrigger = Boolean(rootRef.current?.contains(target));
      const inMenu = Boolean(menuRef.current?.contains(target));
      if (!inTrigger && !inMenu) {
        onOpenChange(false);
      }
    }

    function onKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        onOpenChange(false);
        triggerRef.current?.focus();
        return;
      }

      if (event.key !== "ArrowDown" && event.key !== "ArrowUp") {
        return;
      }

      const menuEl = menuRef.current;
      if (!menuEl) return;
      const items = Array.from(
        menuEl.querySelectorAll<HTMLElement>('[role="menuitem"]:not([disabled])'),
      );
      if (items.length === 0) return;

      const activeIndex = items.indexOf(document.activeElement as HTMLElement);
      event.preventDefault();
      if (event.key === "ArrowDown") {
        const next = activeIndex < 0 ? 0 : (activeIndex + 1) % items.length;
        items[next]?.focus();
      } else {
        const next = activeIndex <= 0 ? items.length - 1 : activeIndex - 1;
        items[next]?.focus();
      }
    }

    function onRepositionEvent(event: Event) {
      if (menuRef.current && event.target instanceof Node && menuRef.current.contains(event.target)) {
        return;
      }
      reposition();
    }

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    window.addEventListener("resize", onRepositionEvent);
    // Capture scroll from table scrollports and window.
    window.addEventListener("scroll", onRepositionEvent, true);

    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
      window.removeEventListener("resize", onRepositionEvent);
      window.removeEventListener("scroll", onRepositionEvent, true);
    };
  }, [onOpenChange, open, align, collisionPadding]);

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

  const menuStyle: CSSProperties | undefined = portal
    ? {
        position: "fixed",
        top: coords?.top ?? 0,
        left: coords?.left ?? 0,
        zIndex: DROPDOWN_Z_INDEX,
        maxHeight: coords?.maxHeight,
        visibility: coords ? "visible" : "hidden",
      }
    : undefined;

  const menuSurface = open ? (
    <div
      ref={menuRef}
      id={menuId}
      role="menu"
      aria-labelledby={triggerId}
      aria-label={menuLabel}
      data-exits-dropdown-portal={portal ? "true" : undefined}
      data-placement={coords?.placement}
      style={menuStyle}
      className={cn(
        "overflow-y-auto overflow-x-hidden rounded-[var(--exits-radius-md)] border border-[var(--exits-border-strong)] bg-[var(--exits-surface-elevated)] py-1 shadow-[var(--exits-shadow-md)]",
        portal
          ? "min-w-[12.5rem] max-w-[min(20rem,calc(100vw-1.25rem))]"
          : cn(
              "absolute top-[calc(100%+0.35rem)] z-[70] min-w-[14rem] max-w-[min(20rem,calc(100vw-1.5rem))] overflow-hidden",
              align === "end" ? "inset-inline-end-0" : "inset-inline-start-0",
            ),
        menuClassName,
      )}
    >
      {children}
    </div>
  ) : null;

  return (
    <div ref={rootRef} className={cn("relative inline-flex", className)}>
      {trigger({
        id: triggerId,
        expanded: open,
        controls: menuId,
        onClick: () => onOpenChange(!open),
        onKeyDown: onTriggerKeyDown,
      })}
      {portal
        ? open && typeof document !== "undefined"
          ? createPortal(menuSurface, document.body)
          : null
        : menuSurface}
    </div>
  );
}

export function MenuItem({
  children,
  onSelect,
  destructive = false,
  disabled = false,
  ...rest
}: {
  children: ReactNode;
  onSelect: () => void;
  destructive?: boolean;
  disabled?: boolean;
} & ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      type="button"
      role="menuitem"
      disabled={disabled}
      {...rest}
      className={cn(
        "flex w-full items-center gap-2 px-3 py-2.5 text-start text-[length:var(--exits-text-sm)] font-medium transition-colors duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:bg-[var(--exits-surface-muted)] hover:bg-[var(--exits-surface-muted)]",
        destructive
          ? "text-destructive hover:bg-[var(--exits-surface-muted)]"
          : "text-foreground",
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
  return (
    <div className="border-b border-border px-3 py-2 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
      {children}
    </div>
  );
}
