import { useEffect, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

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

export type BottomSheetPresentation = "sheet" | "sheet-mobile-dialog-desktop";

export function BottomSheet({
  open,
  onClose,
  title,
  children,
  panelId,
  testId = "bottom-sheet",
  closeLabel = "Close",
  panelClassName,
  /** Mobile bottom sheet; from md+ optionally center as a compact dialog. */
  presentation = "sheet",
}: {
  open: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  panelId: string;
  testId?: string;
  closeLabel?: string;
  /** Extra classes for the dialog panel (e.g. desktop max-width). */
  panelClassName?: string;
  presentation?: BottomSheetPresentation;
}) {
  useBodyScrollLock(open);

  useEffect(() => {
    if (!open) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
      }
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  // Unmount when closed so transform animations on ancestors cannot trap `position: fixed`.
  if (!open || typeof document === "undefined") {
    return null;
  }

  const responsiveDialog = presentation === "sheet-mobile-dialog-desktop";

  return createPortal(
    <>
      <div
        className="fixed inset-0 z-[60] bg-black/40"
        role="presentation"
        onClick={onClose}
        data-testid={`${testId}-backdrop`}
      />
      <div
        id={panelId}
        data-testid={testId}
        data-presentation={presentation}
        className={cn(
          "fixed z-[70] flex min-h-0 flex-col gap-3 overflow-hidden border border-border bg-surface p-4",
          responsiveDialog
            ? [
                // Mobile: bottom sheet
                "inset-x-0 bottom-0 max-h-[75dvh] rounded-t-[var(--exits-radius-lg)] shadow-[0_-8px_32px_rgba(0,0,0,0.12)]",
                // md+: centered compact dialog
                "md:inset-auto md:left-1/2 md:top-1/2 md:w-[min(100%-2rem,40rem)] md:max-h-[75vh] md:-translate-x-1/2 md:-translate-y-1/2 md:rounded-[var(--exits-radius-lg)] md:shadow-[var(--exits-shadow-lg)]",
              ]
            : "inset-x-0 bottom-0 max-h-[75dvh] rounded-t-[var(--exits-radius-lg)] shadow-[0_-8px_32px_rgba(0,0,0,0.12)]",
          panelClassName,
        )}
        role="dialog"
        aria-modal="true"
        aria-label={title}
      >
        {title ? (
          <div className="bottom-sheet__header flex shrink-0 items-center justify-between gap-3">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{title}</h2>
            <Button type="button" variant="ghost" className="shrink-0" onClick={onClose}>
              {closeLabel}
            </Button>
          </div>
        ) : null}
        <div className="bottom-sheet__body flex min-h-0 min-w-0 flex-1 flex-col overflow-y-auto">
          {children}
        </div>
      </div>
    </>,
    document.body,
  );
}

export function ConfirmationDialog({
  open,
  title,
  detail,
  confirmLabel,
  cancelLabel,
  onConfirm,
  onCancel,
  confirmTone = "default",
  cancelTone = "ghost",
  confirmIcon,
  cancelIcon,
  busy = false,
  confirmDisabled = false,
  confirmPendingLabel,
  testId = "confirmation-dialog",
}: {
  open: boolean;
  title: string;
  detail: string;
  confirmLabel: string;
  cancelLabel: string;
  onConfirm: () => void;
  onCancel: () => void;
  /** Use danger for destructive confirms (void, cancel transfer, etc.). */
  confirmTone?: "default" | "danger";
  /** Match page danger-outline actions (e.g. Cancel transfer). */
  cancelTone?: "ghost" | "danger-outline";
  confirmIcon?: ReactNode;
  cancelIcon?: ReactNode;
  /** When true, actions are disabled and backdrop dismiss is ignored. */
  busy?: boolean;
  confirmDisabled?: boolean;
  /** Shown on the confirm button while `busy` (falls back to confirmLabel). */
  confirmPendingLabel?: string;
  testId?: string;
}) {
  useBodyScrollLock(open);

  // Portal to body so ancestor overflow/transform cannot trap `position: fixed`
  // or let sticky action bars steal clicks from the confirm button.
  if (!open || typeof document === "undefined") {
    return null;
  }

  const confirmBlocked = busy || confirmDisabled;

  return createPortal(
    <div
      className="fixed inset-0 z-[80] flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={() => {
        if (!busy) {
          onCancel();
        }
      }}
      data-testid={`${testId}-backdrop`}
    >
      <div
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={`${testId}-title`}
        aria-describedby={`${testId}-detail`}
        aria-busy={busy || undefined}
        data-testid={testId}
        className="w-full max-w-md rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 shadow-lg"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id={`${testId}-title`} className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {title}
        </h2>
        <p
          id={`${testId}-detail`}
          className="m-0 mt-2 whitespace-pre-line text-[length:var(--exits-text-sm)] text-muted"
        >
          {detail}
        </p>
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button
            type="button"
            variant={cancelTone === "danger-outline" ? "outline" : "ghost"}
            className={
              cancelTone === "danger-outline"
                ? "border-destructive/40 text-destructive hover:border-destructive/55 hover:bg-[var(--exits-danger-soft)]"
                : undefined
            }
            disabled={busy}
            onClick={(event) => {
              event.stopPropagation();
              if (!busy) {
                onCancel();
              }
            }}
            data-testid={`${testId}-cancel`}
          >
            {cancelIcon}
            {cancelLabel}
          </Button>
          <Button
            type="button"
            variant={confirmTone === "danger" ? "destructive" : "default"}
            disabled={confirmBlocked}
            onClick={(event) => {
              event.stopPropagation();
              if (!confirmBlocked) {
                onConfirm();
              }
            }}
            data-testid={`${testId}-confirm`}
          >
            {busy ? null : confirmIcon}
            {busy ? (confirmPendingLabel ?? confirmLabel) : confirmLabel}
          </Button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
