import { useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { EXITS_CANCEL_BUTTON_CLASS } from "@/components/exits/exits-cancel-button";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

export type FormDrawerSize = "sm" | "md" | "lg";

const SIZE_PANEL_CLASS: Record<FormDrawerSize, string> = {
  sm: "exits-form-drawer__panel--sm",
  md: "exits-form-drawer__panel--md",
  lg: "exits-form-drawer__panel--lg",
};

function getFocusable(container: HTMLElement): HTMLElement[] {
  const nodes = container.querySelectorAll<HTMLElement>(
    'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
  );
  return Array.from(nodes).filter(
    (el) => !el.hasAttribute("disabled") && el.getAttribute("aria-hidden") !== "true",
  );
}

/**
 * Canonical entity edit shell: right drawer on desktop, full-width 100dvh sheet on mobile.
 * Owns presentation only — no domain fields or API calls.
 */
export function FormDrawer({
  open,
  onOpenChange,
  title,
  description,
  children,
  onSave,
  saveLabel = "Save changes",
  cancelLabel = "Cancel",
  saving = false,
  saveDisabled = false,
  footer,
  size = "md",
  testId = "form-drawer",
  saveTestId,
  cancelTestId,
  closeLabel = "Close",
  dirty = false,
  unsavedTitle = "Discard unsaved changes?",
  unsavedDetail = "You have unsaved changes. Close anyway?",
  unsavedConfirmLabel = "Discard",
  unsavedCancelLabel = "Keep editing",
  confirmUnsavedOnClose = false,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  children: ReactNode;
  onSave?: () => void;
  saveLabel?: string;
  cancelLabel?: string;
  saving?: boolean;
  saveDisabled?: boolean;
  /** Replaces default Cancel/Save footer when provided. */
  footer?: ReactNode;
  size?: FormDrawerSize;
  testId?: string;
  saveTestId?: string;
  cancelTestId?: string;
  closeLabel?: string;
  /** When true with confirmUnsavedOnClose, closing prompts before discard. */
  dirty?: boolean;
  confirmUnsavedOnClose?: boolean;
  unsavedTitle?: string;
  unsavedDetail?: string;
  unsavedConfirmLabel?: string;
  unsavedCancelLabel?: string;
}) {
  const panelRef = useRef<HTMLDivElement | null>(null);
  const restoreFocusRef = useRef<HTMLElement | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);

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
    const target = restoreFocusRef.current;
    restoreFocusRef.current = null;
    window.requestAnimationFrame(() => {
      target.focus?.();
    });
  }, [open]);

  useEffect(() => {
    if (!open) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Tab" || !panelRef.current) {
        return;
      }
      const focusable = getFocusable(panelRef.current);
      if (focusable.length === 0) {
        event.preventDefault();
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
  }, [open]);

  function requestClose() {
    if (saving) {
      return;
    }
    if (confirmUnsavedOnClose && dirty) {
      setConfirmOpen(true);
      return;
    }
    onOpenChange(false);
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (saving || saveDisabled || !onSave) {
      return;
    }
    onSave();
  }

  const defaultFooter = (
    <div className="exits-form-drawer__footer-actions">
      <Button
        type="button"
        variant="outline"
        disabled={saving}
        className={EXITS_CANCEL_BUTTON_CLASS}
        data-testid={cancelTestId ?? `${testId}-cancel`}
        onClick={requestClose}
      >
        {cancelLabel}
      </Button>
      {onSave ? (
        <Button
          type="submit"
          disabled={saving || saveDisabled}
          data-testid={saveTestId ?? `${testId}-save`}
        >
          {saveLabel}
        </Button>
      ) : null}
    </div>
  );

  return (
    <>
      <SideDrawer
        open={open}
        onClose={requestClose}
        title={title}
        description={description}
        testId={testId}
        closeLabel={closeLabel}
        panelClassName={cn("exits-form-drawer__panel", SIZE_PANEL_CLASS[size])}
        panelRef={panelRef}
      >
        <form className="exits-form-drawer" data-testid={`${testId}-form`} onSubmit={handleSubmit}>
          <div className="exits-form-drawer__body">{children}</div>
          <div className="exits-form-drawer__footer">{footer ?? defaultFooter}</div>
        </form>
      </SideDrawer>

      <ConfirmationDialog
        open={confirmOpen}
        title={unsavedTitle}
        detail={unsavedDetail}
        confirmLabel={unsavedConfirmLabel}
        cancelLabel={unsavedCancelLabel}
        confirmTone="danger"
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => {
          setConfirmOpen(false);
          onOpenChange(false);
        }}
        testId={`${testId}-unsaved`}
      />
    </>
  );
}
