import {
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import type { LucideIcon } from "lucide-react";
import { getActionIcon } from "@/components/exits/action-semantics";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { useBodyScrollLock } from "@/lib/use-body-scroll-lock";

export type ExitsModalSize = "sm" | "md" | "lg";

const SIZE_CLASS: Record<ExitsModalSize, string> = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
};

function getFocusable(container: HTMLElement): HTMLElement[] {
  const nodes = container.querySelectorAll<HTMLElement>(
    'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
  );
  return Array.from(nodes).filter(
    (el) => !el.hasAttribute("disabled") && el.getAttribute("aria-hidden") !== "true",
  );
}

export type ExitsModalProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: ReactNode;
  children: ReactNode;
  /** Sticky footer actions (Cancel | Continue pattern). */
  footer?: ReactNode;
  size?: ExitsModalSize;
  /**
   * Below `lg`, fill the viewport height (edge-to-edge sheet).
   * Desktop (`lg+`) keeps the normal centered dialog height.
   */
  fullHeightOnCompact?: boolean;
  /** When true, Escape / backdrop / close control do not dismiss. */
  busy?: boolean;
  /** When false, backdrop click does not dismiss (default true). */
  closeOnOutsideClick?: boolean;
  /** When false, Escape does not dismiss (default true). Useful for forms with nested date pickers. */
  closeOnEscape?: boolean;
  testId?: string;
  closeLabel?: string;
  /** Optional id on the dialog panel (e.g. aria-controls from an opener). */
  id?: string;
  /** Optional form submit handler — wraps body+footer in <form>. */
  onSubmit?: (event: FormEvent<HTMLFormElement>) => void;
  className?: string;
  /** Render the title as a primary card ribbon overhanging the top-left corner. */
  ribbon?: boolean;
  /** Icon shown in the ribbon pennant (ribbon titles only). */
  ribbonIcon?: LucideIcon;
};

/**
 * Canonical short contextual modal shell (NOT FormDrawer).
 * Owns overlay, focus trap, Esc, restore focus, mobile sizing, close control.
 */
export function ExitsModal({
  open,
  onOpenChange,
  title,
  description,
  children,
  footer,
  size = "md",
  fullHeightOnCompact = false,
  busy = false,
  closeOnOutsideClick = true,
  closeOnEscape = true,
  testId = "exits-modal",
  closeLabel = "Close",
  id,
  onSubmit,
  className,
  ribbon = false,
  ribbonIcon: RibbonIcon,
}: ExitsModalProps) {
  const titleId = useId();
  const descriptionId = useId();
  const panelRef = useRef<HTMLDivElement | null>(null);
  const backdropRef = useRef<HTMLDivElement | null>(null);
  const restoreFocusRef = useRef<HTMLElement | null>(null);
  const CloseIcon = getActionIcon("close");
  const [mounted, setMounted] = useState(open);
  if (open && !mounted) {
    setMounted(true);
  }
  const closing = mounted && !open;

  useBodyScrollLock(open);

  // Stay mounted until the exit animation ends; unmount at once when nothing animates
  // (reduced motion, or environments without the Web Animations API).
  useEffect(() => {
    if (!closing) {
      return;
    }
    const animations = [backdropRef.current, panelRef.current].flatMap((node) =>
      node && typeof node.getAnimations === "function" ? node.getAnimations() : [],
    );
    if (animations.length === 0) {
      setMounted(false);
      return;
    }
    let cancelled = false;
    void Promise.allSettled(animations.map((animation) => animation.finished)).then(() => {
      if (!cancelled) {
        setMounted(false);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [closing]);

  useEffect(() => {
    if (!open) {
      return;
    }
    restoreFocusRef.current =
      typeof document !== "undefined" ? (document.activeElement as HTMLElement | null) : null;
    const frame = window.requestAnimationFrame(() => {
      const panel = panelRef.current;
      if (!panel) {
        return;
      }
      // Prefer an explicit autofocus target; otherwise focus the panel itself.
      // Avoid focusing the header Close control (Space/Enter dismisses) and avoid
      // auto-focusing the first field (races with callers that type into other inputs).
      const preferred = panel.querySelector<HTMLElement>(
        "[data-exits-modal-autofocus='true']",
      );
      (preferred ?? panel).focus();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [open]);

  useEffect(() => {
    if (open || !restoreFocusRef.current) {
      return;
    }
    restoreFocusRef.current.focus?.();
    restoreFocusRef.current = null;
  }, [open]);

  useEffect(() => {
    if (!open) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        if (busy || !closeOnEscape) {
          return;
        }
        event.preventDefault();
        onOpenChange(false);
        return;
      }
      if (event.key !== "Tab" || !panelRef.current) {
        return;
      }
      const focusable = getFocusable(panelRef.current);
      if (focusable.length === 0) {
        event.preventDefault();
        panelRef.current.focus();
        return;
      }
      const first = focusable[0]!;
      const last = focusable[focusable.length - 1]!;
      const active = document.activeElement as HTMLElement | null;
      if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, busy, closeOnEscape, onOpenChange]);

  if (!mounted || typeof document === "undefined") {
    return null;
  }
  const motionState = closing ? "closed" : "open";

  const body = (
    <>
      <div className="flex shrink-0 items-start justify-between gap-3 border-b border-border pb-3">
        <div className={cn("min-w-0", ribbon && "exits-modal__ribbon-head")}>
          {ribbon ? (
            <h2 id={titleId} className="exits-modal__ribbon" data-testid={`${testId}-ribbon`}>
              {RibbonIcon ? (
                <span className="exits-modal__pennant" aria-hidden>
                  <RibbonIcon className="size-[0.95rem] shrink-0" />
                </span>
              ) : null}
              {title}
            </h2>
          ) : (
            <h2
              id={titleId}
              className="m-0 text-[length:var(--exits-text-md)] font-semibold text-foreground"
            >
              {title}
            </h2>
          )}
          {description ? (
            <p
              id={descriptionId}
              className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
            >
              {description}
            </p>
          ) : null}
        </div>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={closeLabel}
          disabled={busy}
          onClick={() => {
            if (!busy) {
              onOpenChange(false);
            }
          }}
          data-testid={`${testId}-close`}
        >
          {CloseIcon ? <CloseIcon className="size-4" aria-hidden /> : null}
        </Button>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto py-1">{children}</div>
      {footer ? (
        <div className="flex shrink-0 flex-wrap justify-end gap-2 border-t border-border pt-3">
          {footer}
        </div>
      ) : null}
    </>
  );

  return createPortal(
    <div
      className={cn(
        "exits-modal-backdrop fixed inset-0 z-[70] flex justify-center bg-black/40",
        fullHeightOnCompact
          ? "items-stretch p-0 lg:items-center lg:p-4"
          : "items-end p-4 sm:items-center",
      )}
      ref={backdropRef}
      role="presentation"
      data-state={motionState}
      data-testid={`${testId}-backdrop`}
      onClick={() => {
        if (!closing && !busy && closeOnOutsideClick) {
          onOpenChange(false);
        }
      }}
    >
      <div
        ref={panelRef}
        data-state={motionState}
        id={id}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        aria-busy={busy || undefined}
        tabIndex={-1}
        data-testid={testId}
        className={cn(
          "flex w-full flex-col gap-3 border border-border bg-surface p-4 shadow-[var(--exits-shadow-lg)]",
          ribbon ? "exits-modal--ribbon relative overflow-visible" : "overflow-hidden",
          "exits-modal-panel",
          fullHeightOnCompact
            ? cn(
                "h-[100dvh] max-h-[100dvh] max-w-none rounded-none",
                "lg:h-auto lg:max-h-[min(90dvh,40rem)] lg:rounded-[var(--exits-radius-md)]",
                size === "sm" && "lg:max-w-sm",
                size === "md" && "lg:max-w-md",
                size === "lg" && "lg:max-w-lg",
              )
            : cn("max-h-[min(90dvh,40rem)] rounded-[var(--exits-radius-md)]", SIZE_CLASS[size]),
          className,
        )}
        onClick={(event) => event.stopPropagation()}
      >
        {onSubmit ? (
          <form
            className="flex min-h-0 flex-1 flex-col gap-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (!busy) {
                onSubmit(event);
              }
            }}
          >
            {body}
          </form>
        ) : (
          body
        )}
      </div>
    </div>,
    document.body,
  );
}
