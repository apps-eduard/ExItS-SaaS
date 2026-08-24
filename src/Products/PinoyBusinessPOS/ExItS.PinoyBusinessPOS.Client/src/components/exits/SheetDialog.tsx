import type { ReactNode } from "react";
import { createPortal } from "react-dom";
import { Button } from "@/components/ui/button";

export function BottomSheet({
  open,
  onClose,
  title,
  children,
  panelId,
  testId = "bottom-sheet",
  closeLabel = "Close",
}: {
  open: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  panelId: string;
  testId?: string;
  closeLabel?: string;
}) {
  // Unmount when closed so transform animations on ancestors cannot trap `position: fixed`.
  if (!open || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <>
      <div
        className="fixed inset-0 z-30 bg-black/40"
        role="presentation"
        onClick={onClose}
        data-testid={`${testId}-backdrop`}
      />
      <div
        id={panelId}
        data-testid={testId}
        className="fixed inset-x-0 bottom-0 z-40 flex max-h-[75dvh] flex-col gap-3 rounded-t-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[0_-8px_32px_rgba(0,0,0,0.12)]"
        role="dialog"
        aria-modal="true"
        aria-label={title}
      >
        {title ? (
          <div className="flex items-center justify-between gap-3">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{title}</h2>
            <Button type="button" variant="ghost" onClick={onClose}>
              {closeLabel}
            </Button>
          </div>
        ) : null}
        {children}
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
  testId = "confirmation-dialog",
}: {
  open: boolean;
  title: string;
  detail: string;
  confirmLabel: string;
  cancelLabel: string;
  onConfirm: () => void;
  onCancel: () => void;
  testId?: string;
}) {
  if (!open) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
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
          className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
        >
          {detail}
        </p>
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onCancel}>
            {cancelLabel}
          </Button>
          <Button type="button" onClick={onConfirm}>
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
