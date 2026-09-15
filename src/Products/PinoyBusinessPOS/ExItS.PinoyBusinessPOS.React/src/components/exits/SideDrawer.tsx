import { useEffect, useId, useRef, useState, type ReactNode, type Ref, type TransitionEvent } from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { prefersReducedMotion } from "@/lib/motion";
import { useBodyScrollLock } from "@/lib/use-body-scroll-lock";

export function SideDrawer({
  open,
  onClose,
  onExited,
  title,
  description,
  children,
  panelId,
  testId = "side-drawer",
  closeLabel = "Close",
  closeTestId,
  side = "right",
  panelClassName,
  panelRef,
}: {
  open: boolean;
  onClose: () => void;
  /** Called after the exit animation finishes (or immediately when motion is disabled). */
  onExited?: () => void;
  title: string;
  description?: string;
  children: ReactNode;
  panelId?: string;
  testId?: string;
  closeLabel?: string;
  closeTestId?: string;
  side?: "right" | "left";
  panelClassName?: string;
  panelRef?: Ref<HTMLDivElement>;
}) {
  const autoId = useId();
  const resolvedPanelId = panelId ?? `${testId}-panel`;
  const titleId = `${autoId}-title`;
  const descriptionId = description ? `${autoId}-description` : undefined;
  const [mounted, setMounted] = useState(open);
  const [entered, setEntered] = useState(false);
  const exitingRef = useRef(false);
  const exitCompletedRef = useRef(false);
  const onExitedRef = useRef(onExited);
  onExitedRef.current = onExited;

  // Open after a closed start: mount in the same turn so portals appear without waiting an effect.
  if (open && !mounted) {
    setMounted(true);
  }

  // Lock while logically open — never while only exiting (avoids stuck overflow if unmount stalls).
  useBodyScrollLock(open);

  useEffect(() => {
    if (!open) {
      setEntered(false);
      exitingRef.current = true;
      return;
    }

    exitingRef.current = false;
    exitCompletedRef.current = false;
    setMounted(true);
    // Double-rAF for enter transition. Cancel BOTH frames so a fast close cannot
    // leave entered=true after open=false (invisible full-screen click block).
    let cancelled = false;
    let inner = 0;
    const outer = window.requestAnimationFrame(() => {
      inner = window.requestAnimationFrame(() => {
        if (!cancelled) {
          setEntered(true);
        }
      });
    });
    return () => {
      cancelled = true;
      window.cancelAnimationFrame(outer);
      if (inner) {
        window.cancelAnimationFrame(inner);
      }
    };
  }, [open]);

  const interactive = open && entered;

  useEffect(() => {
    if (!mounted || !open) {
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
  }, [mounted, open, onClose]);

  useEffect(() => {
    if (!mounted || open) {
      return;
    }

    // Fallback when transitionend does not fire (jsdom, reduced motion, interrupted).
    const reduced = prefersReducedMotion();
    const timeoutMs = reduced ? 0 : 450;
    const timeout = window.setTimeout(() => {
      finishExit();
    }, timeoutMs);

    return () => window.clearTimeout(timeout);
  }, [mounted, open]);

  function finishExit() {
    if (open || exitCompletedRef.current) {
      return;
    }
    exitCompletedRef.current = true;
    exitingRef.current = false;
    setMounted(false);
    onExitedRef.current?.();
  }

  function onPanelTransitionEnd(event: TransitionEvent<HTMLDivElement>) {
    if (event.target !== event.currentTarget) {
      return;
    }
    if (event.propertyName !== "transform") {
      return;
    }
    if (open || entered) {
      return;
    }
    finishExit();
  }

  if (!mounted || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <div
      className="exits-side-drawer"
      data-testid={testId}
      data-side={side}
      data-open={entered}
      data-interactive={interactive ? "true" : "false"}
      // inert removes the whole exiting portal from hit-testing (stronger than CSS alone).
      {...(!interactive ? { inert: true } : {})}
    >
      <div
        className="exits-side-drawer__backdrop"
        role="presentation"
        data-open={entered}
        data-interactive={interactive ? "true" : "false"}
        data-testid={`${testId}-backdrop`}
        onClick={interactive ? onClose : undefined}
      />
      <div
        ref={panelRef}
        id={resolvedPanelId}
        className={cn("exits-side-drawer__panel", panelClassName)}
        data-open={entered}
        data-interactive={interactive ? "true" : "false"}
        data-side={side}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        tabIndex={-1}
        onTransitionEnd={onPanelTransitionEnd}
      >
        <div className="exits-side-drawer__header">
          <div className="exits-side-drawer__heading min-w-0 flex-1">
            <h2 id={titleId} className="exits-side-drawer__title">
              {title}
            </h2>
            {description ? (
              <p id={descriptionId} className="exits-side-drawer__description">
                {description}
              </p>
            ) : null}
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="shrink-0"
            data-testid={closeTestId ?? `${testId}-close`}
            aria-label={closeLabel}
            onClick={onClose}
          >
            <X className="size-5" aria-hidden="true" />
          </Button>
        </div>
        <div className="exits-side-drawer__body">{children}</div>
      </div>
    </div>,
    document.body,
  );
}
