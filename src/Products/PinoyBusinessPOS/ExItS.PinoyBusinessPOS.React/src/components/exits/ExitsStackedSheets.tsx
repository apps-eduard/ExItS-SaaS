import {
  useEffect,
  useId,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
  type TransitionEvent,
} from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";
import { cn } from "@/lib/cn";
import { useBodyScrollLock } from "@/lib/use-body-scroll-lock";
import { usePrefersReducedMotion } from "@/lib/motion";

/** Coordinated lower-sheet swap duration (ms). */
export const EXITS_STACKED_SHEETS_DURATION_MS = 250;

export type ExitsStackedLowerPanel = {
  id: string;
  content: ReactNode;
  /** When true, lower stage uses a solid white surface instead of the tinted stage. */
  surface?: "tinted" | "solid";
};

type LowerPhase = "idle" | "exit" | "enter";

/**
 * Premium stacked sheets: stationary upper identity surface + one animated lower panel.
 * Lower swaps slide down then up with no visible gap between upper and lower.
 */
export function ExitsStackedSheets({
  open,
  onClose,
  onEscape,
  upper,
  lower,
  testId = "exits-stacked-sheets",
  closeLabel = "Close",
  labelledBy,
  describedBy,
  className,
  /** Force light chrome even when the app theme is dark (experiment). */
  forceLight = true,
}: {
  open: boolean;
  /** X control and backdrop — full dismiss. */
  onClose: () => void;
  /** Escape key. Defaults to onClose. Use to step back from a lower panel first. */
  onEscape?: () => void;
  upper: ReactNode;
  lower: ExitsStackedLowerPanel;
  testId?: string;
  closeLabel?: string;
  labelledBy?: string;
  describedBy?: string;
  className?: string;
  forceLight?: boolean;
}) {
  const autoId = useId();
  const panelId = `${testId}-panel-${autoId}`;
  const reducedMotion = usePrefersReducedMotion();
  const durationMs = reducedMotion ? 0 : EXITS_STACKED_SHEETS_DURATION_MS;

  const [rendered, setRendered] = useState(open);
  const [entered, setEntered] = useState(false);
  const [displayLower, setDisplayLower] = useState(lower);
  const [phase, setPhase] = useState<LowerPhase>("idle");
  const [enterToken, setEnterToken] = useState(0);

  const desiredLowerRef = useRef(lower);
  const displayIdRef = useRef(lower.id);
  const phaseRef = useRef<LowerPhase>("idle");
  const swapTimerRef = useRef<number | null>(null);
  const exitTimerRef = useRef<number | null>(null);
  const closeBusyRef = useRef(false);
  const durationRef = useRef(durationMs);

  desiredLowerRef.current = lower;
  displayIdRef.current = displayLower.id;
  phaseRef.current = phase;
  durationRef.current = durationMs;

  useBodyScrollLock(rendered);

  function clearSwapTimer() {
    if (swapTimerRef.current != null) {
      window.clearTimeout(swapTimerRef.current);
      swapTimerRef.current = null;
    }
  }

  function applyLowerContentIfSameId() {
    const desired = desiredLowerRef.current;
    if (desired.id === displayIdRef.current && phaseRef.current === "idle") {
      setDisplayLower(desired);
    }
  }

  function beginSwapToDesired() {
    const desired = desiredLowerRef.current;
    if (desired.id === displayIdRef.current) {
      applyLowerContentIfSameId();
      return;
    }
    if (phaseRef.current !== "idle") {
      return;
    }

    const duration = durationRef.current;
    setPhase("exit");
    phaseRef.current = "exit";

    const afterExit = () => {
      const next = desiredLowerRef.current;
      setDisplayLower(next);
      displayIdRef.current = next.id;
      setEnterToken((token) => token + 1);
      setPhase("enter");
      phaseRef.current = "enter";

      const afterEnter = () => {
        setPhase("idle");
        phaseRef.current = "idle";
        // If a newer panel was requested during the swap, chain another cycle.
        if (desiredLowerRef.current.id !== displayIdRef.current) {
          beginSwapToDesired();
          return;
        }
        applyLowerContentIfSameId();
      };

      if (duration === 0) {
        afterEnter();
        return;
      }
      clearSwapTimer();
      swapTimerRef.current = window.setTimeout(() => {
        swapTimerRef.current = null;
        afterEnter();
      }, duration);
    };

    if (duration === 0) {
      afterExit();
      return;
    }
    clearSwapTimer();
    swapTimerRef.current = window.setTimeout(() => {
      swapTimerRef.current = null;
      afterExit();
    }, duration);
  }

  useEffect(() => {
    if (open) {
      closeBusyRef.current = false;
      if (exitTimerRef.current != null) {
        window.clearTimeout(exitTimerRef.current);
        exitTimerRef.current = null;
      }
      clearSwapTimer();
      setRendered(true);
      setDisplayLower(desiredLowerRef.current);
      displayIdRef.current = desiredLowerRef.current.id;
      setPhase("idle");
      phaseRef.current = "idle";
      const frame = window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => setEntered(true));
      });
      return () => window.cancelAnimationFrame(frame);
    }
    if (!rendered) {
      return;
    }
    closeBusyRef.current = true;
    setEntered(false);
    exitTimerRef.current = window.setTimeout(() => {
      exitTimerRef.current = null;
      closeBusyRef.current = false;
      clearSwapTimer();
      setRendered(false);
      setDisplayLower(desiredLowerRef.current);
      displayIdRef.current = desiredLowerRef.current.id;
      setPhase("idle");
      phaseRef.current = "idle";
    }, durationMs || 1);
    return () => {
      if (exitTimerRef.current != null) {
        window.clearTimeout(exitTimerRef.current);
        exitTimerRef.current = null;
      }
    };
  }, [open, rendered, durationMs]);

  // Drive lower swaps from the desired panel id without cancelling in-flight timers on content refresh.
  useEffect(() => {
    if (!open || !rendered || !entered) {
      return;
    }
    if (lower.id === displayIdRef.current) {
      applyLowerContentIfSameId();
      return;
    }
    beginSwapToDesired();
    // Intentionally only react to id changes; content updates use the same-id path.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- id-driven swap
  }, [lower.id, open, rendered, entered]);

  useEffect(() => {
    if (!open || !rendered || !entered) {
      return;
    }
    if (lower.id === displayIdRef.current && phaseRef.current === "idle") {
      setDisplayLower(lower);
    }
  }, [lower, open, rendered, entered]);

  useEffect(() => {
    if (!rendered || !entered) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") {
        return;
      }
      event.preventDefault();
      if (closeBusyRef.current) {
        return;
      }
      (onEscape ?? onClose)();
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [rendered, entered, onClose, onEscape]);

  useEffect(() => {
    return () => {
      clearSwapTimer();
      if (exitTimerRef.current != null) {
        window.clearTimeout(exitTimerRef.current);
        exitTimerRef.current = null;
      }
    };
  }, []);

  function requestClose() {
    if (closeBusyRef.current) {
      return;
    }
    onClose();
  }

  function onRootTransitionEnd(event: TransitionEvent<HTMLDivElement>) {
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
    closeBusyRef.current = false;
    setRendered(false);
  }

  if (!rendered || typeof document === "undefined") {
    return null;
  }

  const lowerMotion = phase === "exit" ? "exit" : phase === "enter" ? "enter" : "idle";
  const lowerSurface = displayLower.surface ?? "tinted";
  const durationStyle = {
    "--exits-stacked-sheets-duration": `${durationMs}ms`,
  } as CSSProperties;

  return createPortal(
    <div
      className={cn(
        "exits-stacked-sheets",
        forceLight && "exits-stacked-sheets--force-light",
        className,
      )}
      data-testid={testId}
      data-open={entered ? "true" : "false"}
      data-force-light={forceLight ? "true" : undefined}
      data-theme={forceLight ? "light" : undefined}
      data-animating={phase !== "idle" ? "true" : "false"}
      style={durationStyle}
    >
      <div
        className={cn(
          "exits-stacked-sheets__backdrop",
          entered ? "exits-stacked-sheets__backdrop--open" : "exits-stacked-sheets__backdrop--closed",
        )}
        role="presentation"
        data-testid={`${testId}-backdrop`}
        onClick={requestClose}
      />
      <div
        id={panelId}
        role="dialog"
        aria-modal="true"
        aria-labelledby={labelledBy}
        aria-describedby={describedBy}
        data-testid={`${testId}-dialog`}
        data-state={entered ? "open" : "closed"}
        className={cn(
          "exits-stacked-sheets__dialog",
          entered ? "exits-stacked-sheets__dialog--open" : "exits-stacked-sheets__dialog--closed",
        )}
        onTransitionEnd={onRootTransitionEnd}
      >
        <div className="exits-stacked-sheets__handle" aria-hidden>
          <span className="exits-stacked-sheets__handle-bar" />
        </div>
        <button
          type="button"
          className="exits-stacked-sheets__close"
          aria-label={closeLabel}
          data-testid={`${testId}-close`}
          onClick={requestClose}
        >
          <X className="size-4 shrink-0" strokeWidth={2.25} aria-hidden />
        </button>

        <div className="exits-stacked-sheets__stack" data-testid={`${testId}-stack`}>
          <section className="exits-stacked-sheets__upper" data-testid={`${testId}-upper`}>
            {upper}
          </section>

          <section
            className={cn(
              "exits-stacked-sheets__lower-stage",
              lowerSurface === "solid" && "exits-stacked-sheets__lower-stage--solid",
            )}
            data-testid={`${testId}-lower-stage`}
            data-surface={lowerSurface}
          >
            <div
              key={`${displayLower.id}-${enterToken}`}
              className={cn(
                "exits-stacked-sheets__lower",
                lowerSurface === "solid" && "exits-stacked-sheets__lower--solid",
              )}
              data-testid={`${testId}-lower`}
              data-panel={displayLower.id}
              data-motion={lowerMotion}
              style={durationStyle}
            >
              <div
                className="exits-stacked-sheets__lower-scroll"
                data-testid={`${testId}-lower-scroll`}
              >
                {displayLower.content}
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>,
    document.body,
  );
}
