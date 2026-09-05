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

export function BottomSheet({
  open,
  onClose,
  title,
  children,
  panelId,
  testId = "bottom-sheet",
  closeLabel = "Close",
  panelClassName,
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
}) {
  useBodyScrollLock(open);

  // Unmount when closed so transform animations on ancestors cannot trap `position: fixed`.
  if (!open || typeof document === "undefined") {
    return null;
  }

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
        className={cn(
          "fixed inset-x-0 bottom-0 z-[70] flex max-h-[75dvh] min-h-0 flex-col gap-3 overflow-hidden rounded-t-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[0_-8px_32px_rgba(0,0,0,0.12)]",
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
        <div className="bottom-sheet__body flex min-h-0 min-w-0 flex-1 flex-col">{children}</div>
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
  testId?: string;
}) {
  useBodyScrollLock(open);

  // Portal to body so ancestor overflow/transform cannot trap `position: fixed`
  // or let sticky action bars steal clicks from the confirm button.
  if (!open || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <div
      className="fixed inset-0 z-[80] flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onCancel}
      data-testid={`${testId}-backdrop`}
    >
      <div
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={`${testId}-title`}
        aria-describedby={`${testId}-detail`}
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
            onClick={(event) => {
              event.stopPropagation();
              onCancel();
            }}
            data-testid={`${testId}-cancel`}
          >
            {cancelIcon}
            {cancelLabel}
          </Button>
          <Button
            type="button"
            variant={confirmTone === "danger" ? "destructive" : "default"}
            onClick={(event) => {
              event.stopPropagation();
              onConfirm();
            }}
            data-testid={`${testId}-confirm`}
          >
            {confirmIcon}
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
