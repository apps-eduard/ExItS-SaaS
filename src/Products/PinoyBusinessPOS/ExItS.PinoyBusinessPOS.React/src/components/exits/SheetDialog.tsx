import {
  useEffect,
  useRef,
  useState,
  type PointerEvent as ReactPointerEvent,
  type ReactNode,
  type TransitionEvent,
} from "react";
import { createPortal } from "react-dom";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { useBodyScrollLock } from "@/lib/use-body-scroll-lock";

export type BottomSheetPresentation = "sheet" | "sheet-mobile-dialog-desktop";
export type BottomSheetSize = "default" | "full";

const SHEET_DISMISS_DRAG_PX = 96;
const SHEET_DESKTOP_MQ = "(min-width: 768px)";
const SHEET_EXIT_MS = 220;

function sheetSwipeEnabled(presentation: BottomSheetPresentation): boolean {
  if (typeof window === "undefined") {
    return presentation === "sheet";
  }
  if (presentation === "sheet") {
    return true;
  }
  // sheet-mobile-dialog-desktop: swipe only while it is a bottom sheet (mobile).
  return !window.matchMedia(SHEET_DESKTOP_MQ).matches;
}

export function BottomSheet({
  open,
  onClose,
  title,
  children,
  panelId,
  testId = "bottom-sheet",
  closeLabel = "Close",
  closeTestId,
  panelClassName,
  backdropClassName,
  /** Mobile bottom sheet; from md+ optionally center as a compact dialog. */
  presentation = "sheet",
  /** `full` uses near-viewport height so long lists (e.g. activity) can scroll all entries. */
  size = "default",
}: {
  open: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  panelId: string;
  testId?: string;
  closeLabel?: string;
  closeTestId?: string;
  /** Extra classes for the dialog panel (e.g. desktop max-width). */
  panelClassName?: string;
  /** Extra classes for the dimmed backdrop (e.g. nested stack z-index). */
  backdropClassName?: string;
  presentation?: BottomSheetPresentation;
  size?: BottomSheetSize;
}) {
  const [rendered, setRendered] = useState(open);
  const [entered, setEntered] = useState(false);
  const [dragY, setDragY] = useState(0);
  const draggingRef = useRef(false);
  const startYRef = useRef(0);
  const dragYRef = useRef(0);
  const closingRef = useRef(false);
  const exitTimerRef = useRef<number | null>(null);

  useBodyScrollLock(rendered);

  useEffect(() => {
    if (open) {
      closingRef.current = false;
      if (exitTimerRef.current != null) {
        window.clearTimeout(exitTimerRef.current);
        exitTimerRef.current = null;
      }
      setRendered(true);
      setDragY(0);
      dragYRef.current = 0;
      const frame = window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => setEntered(true));
      });
      return () => window.cancelAnimationFrame(frame);
    }
    if (!rendered) {
      return;
    }
    closingRef.current = true;
    setEntered(false);
    exitTimerRef.current = window.setTimeout(() => {
      exitTimerRef.current = null;
      closingRef.current = false;
      dragYRef.current = 0;
      setDragY(0);
      setRendered(false);
    }, SHEET_EXIT_MS);
    return () => {
      if (exitTimerRef.current != null) {
        window.clearTimeout(exitTimerRef.current);
        exitTimerRef.current = null;
      }
    };
  }, [open, rendered]);

  useEffect(() => {
    if (!rendered || !entered) {
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
  }, [rendered, entered, onClose]);

  function endDrag() {
    if (!draggingRef.current) {
      return;
    }
    draggingRef.current = false;
    const dy = dragYRef.current;
    dragYRef.current = 0;
    if (dy >= SHEET_DISMISS_DRAG_PX) {
      const dismissTo =
        typeof window !== "undefined" ? Math.max(dy, Math.round(window.innerHeight * 0.55)) : dy;
      setDragY(dismissTo);
      onClose();
      return;
    }
    setDragY(0);
  }

  function onHandlePointerDown(event: ReactPointerEvent<HTMLElement>) {
    if (event.button !== 0 && event.pointerType === "mouse") {
      return;
    }
    if (!sheetSwipeEnabled(presentation) || closingRef.current) {
      return;
    }
    draggingRef.current = true;
    startYRef.current = event.clientY;
    dragYRef.current = 0;
    setDragY(0);
    event.currentTarget.setPointerCapture(event.pointerId);
  }

  function onHandlePointerMove(event: ReactPointerEvent<HTMLElement>) {
    if (!draggingRef.current) {
      return;
    }
    const dy = Math.max(0, event.clientY - startYRef.current);
    dragYRef.current = dy;
    setDragY(dy);
  }

  function onHandlePointerUp(event: ReactPointerEvent<HTMLElement>) {
    if (!draggingRef.current) {
      return;
    }
    try {
      event.currentTarget.releasePointerCapture(event.pointerId);
    } catch {
      // already released
    }
    endDrag();
  }

  function onPanelTransitionEnd(event: TransitionEvent<HTMLDivElement>) {
    if (event.target !== event.currentTarget) {
      return;
    }
    if (open || entered) {
      return;
    }
    if (event.propertyName !== "transform" && event.propertyName !== "opacity") {
      return;
    }
    if (exitTimerRef.current != null) {
      window.clearTimeout(exitTimerRef.current);
      exitTimerRef.current = null;
    }
    closingRef.current = false;
    setRendered(false);
    setDragY(0);
  }

  if (!rendered || typeof document === "undefined") {
    return null;
  }

  const responsiveDialog = presentation === "sheet-mobile-dialog-desktop";
  const dragging = dragY > 0 && entered;
  const backdropOpacity = dragging ? Math.max(0.08, 0.4 * (1 - dragY / 320)) : undefined;

  return createPortal(
    <>
      <div
        className={cn(
          "bottom-sheet__backdrop fixed inset-0 z-[60] bg-black/40",
          entered ? "bottom-sheet__backdrop--open" : "bottom-sheet__backdrop--closed",
          backdropClassName,
        )}
        role="presentation"
        onClick={() => {
          if (!closingRef.current) {
            onClose();
          }
        }}
        data-testid={`${testId}-backdrop`}
        style={backdropOpacity != null ? { opacity: backdropOpacity / 0.4 } : undefined}
      />
      <div
        id={panelId}
        data-testid={testId}
        data-presentation={presentation}
        data-size={size}
        data-state={entered ? "open" : "closed"}
        className={cn(
          "bottom-sheet__panel fixed z-[70] flex min-h-0 flex-col gap-3 overflow-hidden border border-border bg-surface p-4",
          responsiveDialog
            ? [
                // Mobile: bottom sheet
                "inset-x-0 bottom-0 rounded-t-[var(--exits-radius-lg)] shadow-[0_-8px_32px_rgba(0,0,0,0.12)]",
                size === "full" ? "max-h-[100dvh] h-[100dvh]" : "max-h-[75dvh]",
                // md+: centered compact dialog — match FormDrawer md width (taller when full)
                "md:inset-auto md:left-1/2 md:top-1/2 md:h-auto md:rounded-[var(--exits-radius-lg)] md:shadow-[var(--exits-shadow-lg)]",
                size === "full"
                  ? "md:w-[min(100%-2rem,36rem)] md:max-h-[92vh]"
                  : "md:w-[min(100%-2rem,30rem)] md:max-h-[75vh]",
              ]
            : [
                "inset-x-0 bottom-0 rounded-t-[var(--exits-radius-lg)] shadow-[0_-8px_32px_rgba(0,0,0,0.12)]",
                size === "full" ? "max-h-[100dvh] h-[100dvh]" : "max-h-[75dvh]",
              ],
          entered ? "bottom-sheet__panel--open" : "bottom-sheet__panel--closed",
          dragging && "bottom-sheet__panel--dragging",
          panelClassName,
        )}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onTransitionEnd={onPanelTransitionEnd}
        style={
          dragY > 0
            ? {
                transform: `translateY(${dragY}px)`,
                transition: dragging ? "none" : "transform 0.22s cubic-bezier(0.32, 0.72, 0, 1)",
              }
            : undefined
        }
      >
        <div
          className="bottom-sheet__handle"
          data-testid={`${testId}-handle`}
          aria-hidden
          onPointerDown={onHandlePointerDown}
          onPointerMove={onHandlePointerMove}
          onPointerUp={onHandlePointerUp}
          onPointerCancel={onHandlePointerUp}
        >
          <span className="bottom-sheet__handle-bar" />
        </div>
        {title ? (
          <div className="bottom-sheet__header flex shrink-0 items-center justify-between gap-3 border-b border-border pb-3">
            <h2
              className="bottom-sheet__title m-0 min-w-0 flex-1 text-[length:var(--exits-text-md)] font-semibold"
              onPointerDown={onHandlePointerDown}
              onPointerMove={onHandlePointerMove}
              onPointerUp={onHandlePointerUp}
              onPointerCancel={onHandlePointerUp}
            >
              {title}
            </h2>
            <Button
              type="button"
              variant="ghost"
              className="shrink-0"
              data-testid={closeTestId ?? `${testId}-close`}
              onClick={onClose}
            >
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
  className,
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
  /** Match page danger actions (ghost / outline / soft solid). */
  cancelTone?: "ghost" | "danger-outline" | "danger-soft";
  confirmIcon?: ReactNode;
  cancelIcon?: ReactNode;
  /** When true, actions are disabled and backdrop dismiss is ignored. */
  busy?: boolean;
  confirmDisabled?: boolean;
  /** Shown on the confirm button while `busy` (falls back to confirmLabel). */
  confirmPendingLabel?: string;
  testId?: string;
  /** Extra classes on the portal root (e.g. nested stack z-index). */
  className?: string;
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
      className={cn(
        "fixed inset-0 z-[80] flex items-end justify-center bg-black/40 p-4 sm:items-center",
        className,
      )}
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
            intent={cancelTone === "ghost" ? "neutral" : "danger"}
            appearance={
              cancelTone === "danger-soft"
                ? "solid"
                : cancelTone === "danger-outline"
                  ? "outline"
                  : "ghost"
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
            intent={confirmTone === "danger" ? "danger" : "primary"}
            appearance="solid"
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
