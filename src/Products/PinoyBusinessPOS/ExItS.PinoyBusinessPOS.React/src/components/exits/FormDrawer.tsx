import { useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import {
  MODAL_GLOBAL_CANCEL_APPEARANCE,
  MODAL_GLOBAL_CANCEL_EMPHASIS,
  MODAL_GLOBAL_CANCEL_INTENT,
  MODAL_GLOBAL_SAVE_APPEARANCE,
  MODAL_GLOBAL_SAVE_INTENT,
} from "@/features/ui-standards/modal-action-global-default";

export type FormDrawerSize = "sm" | "md" | "lg";

const SIZE_PANEL_CLASS: Record<FormDrawerSize, string> = {
  sm: "exits-form-drawer__panel--sm",
  md: "exits-form-drawer__panel--md",
  lg: "exits-form-drawer__panel--lg",
};

function SavePlusIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4V12" />
      <path d="M16 13H8a1 1 0 0 0-1 1v7" />
      <path d="M19 22v-6" />
      <path d="M22 19h-6" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

function SaveCheckIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v4.35" />
      <path d="m16 19 2 2 4-4" />
      <path d="M17 15.13V14a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v7" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

function getFocusable(container: HTMLElement): HTMLElement[] {
  const nodes = container.querySelectorAll<HTMLElement>(
    'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
  );
  return Array.from(nodes).filter(
    (el) => !el.hasAttribute("disabled") && el.getAttribute("aria-hidden") !== "true",
  );
}

/**
 * Canonical entity edit shell: floating right drawer (rounded inset panel).
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
  ribbon = false,
  ribbonIcon,
  saveIcon = "plus",
  centerOnLarge = true,
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
  ribbon?: boolean;
  ribbonIcon?: LucideIcon;
  /** Lucide save-plus (add) or save-check (record). */
  saveIcon?: "plus" | "check" | "none";
  /** Large screens: centered dialog. Small screens: side drawer. Default true. */
  centerOnLarge?: boolean;
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
      const labelledBy = panel.getAttribute("aria-labelledby");
      const title =
        (labelledBy ? panel.ownerDocument.getElementById(labelledBy) : null) ??
        panel.querySelector<HTMLElement>(".exits-side-drawer__title, .exits-side-drawer__ribbon");
      (title ?? panel).focus();
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

  const SaveIconMark =
    saveIcon === "check" ? SaveCheckIcon : saveIcon === "plus" ? SavePlusIcon : null;

  const defaultFooter = (
    <div className="exits-form-drawer__footer-actions">
      <Button
        type="button"
        intent={MODAL_GLOBAL_CANCEL_INTENT}
        appearance={MODAL_GLOBAL_CANCEL_APPEARANCE}
        emphasis={MODAL_GLOBAL_CANCEL_EMPHASIS}
        disabled={saving}
        data-testid={cancelTestId ?? `${testId}-cancel`}
        onClick={requestClose}
      >
        {cancelLabel}
      </Button>
      {onSave ? (
        <Button
          type="submit"
          intent={MODAL_GLOBAL_SAVE_INTENT}
          appearance={MODAL_GLOBAL_SAVE_APPEARANCE}
          disabled={saving || saveDisabled}
          data-testid={saveTestId ?? `${testId}-save`}
        >
          {SaveIconMark ? <SaveIconMark className="size-4 shrink-0" /> : null}
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
        ribbon={ribbon}
        ribbonIcon={ribbonIcon}
        centerOnLarge={centerOnLarge}
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
