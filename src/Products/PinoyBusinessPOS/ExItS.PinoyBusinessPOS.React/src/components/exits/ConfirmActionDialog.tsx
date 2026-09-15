import { useEffect, useId, useRef, type ReactNode } from "react";
import { createPortal } from "react-dom";
import {
  CircleAlert,
  CircleHelp,
  Info,
  TriangleAlert,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { EXITS_CANCEL_BUTTON_CLASS } from "@/components/exits/exits-cancel-button";
import { cn } from "@/lib/cn";

export type ConfirmActionVariant = "default" | "info" | "warning" | "danger";

const VARIANT_ICON = {
  default: CircleHelp,
  info: Info,
  warning: TriangleAlert,
  danger: CircleAlert,
} as const;

const VARIANT_ICON_CLASS = {
  default: "text-muted",
  info: "text-[var(--exits-info)]",
  warning: "text-[var(--exits-warning)]",
  danger: "text-[var(--exits-danger)]",
} as const;

const VARIANT_CONFIRM_INTENT = {
  default: "default",
  info: "info",
  warning: "warning",
  danger: "dangerStrong",
} as const;

function useBodyScrollLock(locked: boolean) {
  useEffect(() => {
    if (!locked || typeof document === "undefined") {
      return;
    }
    const { body, documentElement: root } = document;
    const prevBodyOverflow = body.style.overflow;
    const prevRootOverflow = root.style.overflow;
    body.style.overflow = "hidden";
    root.style.overflow = "hidden";
    return () => {
      body.style.overflow = prevBodyOverflow;
      root.style.overflow = prevRootOverflow;
    };
  }, [locked]);
}

function getFocusable(container: HTMLElement): HTMLElement[] {
  const nodes = container.querySelectorAll<HTMLElement>(
    'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
  );
  return Array.from(nodes).filter(
    (el) => !el.hasAttribute("disabled") && el.getAttribute("aria-hidden") !== "true",
  );
}

export type ConfirmActionDialogProps = {
  open: boolean;
  variant?: ConfirmActionVariant;
  title: string;
  description: string;
  confirmLabel: string;
  cancelLabel?: string;
  pending?: boolean;
  confirmDisabled?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  /** Optional extra content (reason field, detail list). */
  children?: ReactNode;
  testId?: string;
};

/**
 * Canonical confirmation dialog — semantic variants own icon + confirm intent.
 * Feature supplies copy + handlers only.
 */
export function ConfirmActionDialog({
  open,
  variant = "default",
  title,
  description,
  confirmLabel,
  cancelLabel = "Cancel",
  pending = false,
  confirmDisabled = false,
  onConfirm,
  onCancel,
  children,
  testId = "confirm-action-dialog",
}: ConfirmActionDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const panelRef = useRef<HTMLDivElement | null>(null);
  const restoreFocusRef = useRef<HTMLElement | null>(null);
  const Icon = VARIANT_ICON[variant];
  const confirmBlocked = pending || confirmDisabled;

  useBodyScrollLock(open);

  useEffect(() => {
    if (!open) {
      return;
    }
    restoreFocusRef.current =
      typeof document !== "undefined" ? (document.activeElement as HTMLElement | null) : null;
    const frame = window.requestAnimationFrame(() => {
      const cancelBtn = panelRef.current?.querySelector<HTMLElement>(
        `[data-testid="${testId}-cancel"]`,
      );
      (cancelBtn ?? panelRef.current)?.focus();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [open, testId]);

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
        if (pending) {
          return;
        }
        event.preventDefault();
        onCancel();
        return;
      }
      if (event.key !== "Tab" || !panelRef.current) {
        return;
      }
      const focusable = getFocusable(panelRef.current);
      if (focusable.length === 0) {
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
  }, [open, pending, onCancel]);

  if (!open || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <div
      className="fixed inset-0 z-[80] flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      data-testid={`${testId}-backdrop`}
      onClick={() => {
        if (!pending) {
          onCancel();
        }
      }}
    >
      <div
        ref={panelRef}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        aria-busy={pending || undefined}
        tabIndex={-1}
        data-testid={testId}
        data-variant={variant}
        className="w-full max-w-md rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-[var(--exits-shadow-lg)]"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="flex gap-3">
          <span
            className={cn(
              "mt-0.5 inline-flex size-9 shrink-0 items-center justify-center rounded-full bg-[var(--exits-surface-muted)]",
              VARIANT_ICON_CLASS[variant],
            )}
            aria-hidden
          >
            <Icon className="size-5" />
          </span>
          <div className="min-w-0 flex-1">
            <h2 id={titleId} className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {title}
            </h2>
            <p
              id={descriptionId}
              className="m-0 mt-2 whitespace-pre-line text-[length:var(--exits-text-sm)] text-muted"
            >
              {description}
            </p>
            {children ? <div className="mt-3">{children}</div> : null}
          </div>
        </div>
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            className={EXITS_CANCEL_BUTTON_CLASS}
            disabled={pending}
            onClick={(event) => {
              event.stopPropagation();
              if (!pending) {
                onCancel();
              }
            }}
            data-testid={`${testId}-cancel`}
          >
            {cancelLabel}
          </Button>
          <Button
            type="button"
            variant={VARIANT_CONFIRM_INTENT[variant]}
            disabled={confirmBlocked}
            onClick={(event) => {
              event.stopPropagation();
              if (!confirmBlocked) {
                onConfirm();
              }
            }}
            data-testid={`${testId}-confirm`}
          >
            {pending ? `${confirmLabel}…` : confirmLabel}
          </Button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
