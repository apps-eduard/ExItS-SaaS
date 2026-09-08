import { useEffect, useId, useRef, useState, type ReactNode, type TransitionEvent } from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";
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
}) {
  const autoId = useId();
  const resolvedPanelId = panelId ?? `${testId}-panel`;
  const titleId = `${autoId}-title`;
  const descriptionId = description ? `${autoId}-description` : undefined;
  const [mounted, setMounted] = useState(open);
  const [entered, setEntered] = useState(false);
  const exitingRef = useRef(false);
  const onExitedRef = useRef(onExited);
  onExitedRef.current = onExited;

  useBodyScrollLock(mounted);

  useEffect(() => {
    if (open) {
      exitingRef.current = false;
      setMounted(true);
      const frame = window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => setEntered(true));
      });
      return () => window.cancelAnimationFrame(frame);
    }

    setEntered(false);
    exitingRef.current = true;
  }, [open]);

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
    const canQueryMotion =
      typeof window !== "undefined" && typeof window.matchMedia === "function";
    const reduced =
      canQueryMotion && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    // jsdom has no CSS transitions; skip the wait when motion APIs are unavailable.
    const timeoutMs = !canQueryMotion || reduced ? 0 : 450;
    const timeout = window.setTimeout(() => {
      finishExit();
    }, timeoutMs);

    return () => window.clearTimeout(timeout);
  }, [mounted, open]);

  function finishExit() {
    if (!exitingRef.current) {
      return;
    }
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
    <div className="exits-side-drawer" data-testid={testId} data-side={side} data-open={entered}>
      <div
        className="exits-side-drawer__backdrop"
        role="presentation"
        data-open={entered}
        data-testid={`${testId}-backdrop`}
        onClick={onClose}
      />
      <div
        id={resolvedPanelId}
        className={cn("exits-side-drawer__panel", panelClassName)}
        data-open={entered}
        data-side={side}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
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
