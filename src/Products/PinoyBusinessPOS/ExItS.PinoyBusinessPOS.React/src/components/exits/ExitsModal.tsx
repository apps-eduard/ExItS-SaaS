import {
  useEffect,
  useId,
  useRef,
  type FormEvent,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
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
  description?: string;
  children: ReactNode;
  /** Sticky footer actions (Cancel | Continue pattern). */
  footer?: ReactNode;
  size?: ExitsModalSize;
  /** When true, Escape / backdrop do not close. */
  busy?: boolean;
  testId?: string;
  closeLabel?: string;
  /** Optional form submit handler — wraps body+footer in <form>. */
  onSubmit?: (event: FormEvent<HTMLFormElement>) => void;
  className?: string;
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
  busy = false,
  testId = "exits-modal",
  closeLabel = "Close",
  onSubmit,
  className,
}: ExitsModalProps) {
  const titleId = useId();
  const descriptionId = useId();
  const panelRef = useRef<HTMLDivElement | null>(null);
  const restoreFocusRef = useRef<HTMLElement | null>(null);
  const CloseIcon = getActionIcon("close");

  useBodyScrollLock(open);

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
      const focusable = getFocusable(panel);
      (focusable[0] ?? panel).focus();
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
        if (busy) {
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
  }, [open, busy, onOpenChange]);

  if (!open || typeof document === "undefined") {
    return null;
  }

  const body = (
    <>
      <div className="flex shrink-0 items-start justify-between gap-3">
        <div className="min-w-0">
          <h2
            id={titleId}
            className="m-0 text-[length:var(--exits-text-md)] font-semibold text-foreground"
          >
            {title}
          </h2>
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
      className="fixed inset-0 z-[70] flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      data-testid={`${testId}-backdrop`}
      onClick={() => {
        if (!busy) {
          onOpenChange(false);
        }
      }}
    >
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        aria-busy={busy || undefined}
        tabIndex={-1}
        data-testid={testId}
        className={cn(
          "flex w-full max-h-[min(90dvh,40rem)] flex-col gap-3 overflow-hidden rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-[var(--exits-shadow-lg)]",
          "exits-motion-dialog motion-reduce:transition-none",
          SIZE_CLASS[size],
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
