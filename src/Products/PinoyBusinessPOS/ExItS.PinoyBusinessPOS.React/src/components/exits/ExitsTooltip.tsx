import {
  cloneElement,
  isValidElement,
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type CSSProperties,
  type FocusEvent,
  type MouseEvent,
  type ReactElement,
  type ReactNode,
  type Ref,
} from "react";
import { createPortal } from "react-dom";
import { cn } from "@/lib/cn";

const HOVER_DELAY_MS = 300;
const FOCUS_DELAY_MS = 40;
const GAP_PX = 8;
const VIEWPORT_PAD = 8;
/** Above sticky topbar (30) and reveal rail (35); below dialogs (~80+). */
const TOOLTIP_Z_INDEX = 45;

type ExitsTooltipProps = {
  content: string;
  children: ReactElement;
  /** When true, tooltip never opens (Standard / Reveal expanded labels). */
  disabled?: boolean;
  className?: string;
  contentClassName?: string;
  /** Test id on the floating surface. */
  testId?: string;
};

type Coords = {
  top: number;
  left: number;
};

function mergeRefs<T>(...refs: Array<Ref<T> | undefined>) {
  return (value: T) => {
    for (const ref of refs) {
      if (typeof ref === "function") {
        ref(value);
      } else if (ref && typeof ref === "object") {
        (ref as { current: T }).current = value;
      }
    }
  };
}

function computePosition(trigger: HTMLElement, tip: HTMLElement): Coords {
  const rect = trigger.getBoundingClientRect();
  const tipW = tip.offsetWidth;
  const tipH = tip.offsetHeight;
  const vw = window.innerWidth;
  const vh = window.innerHeight;
  const rtl = document.documentElement.getAttribute("dir") === "rtl";

  let left = rtl ? rect.left - tipW - GAP_PX : rect.right + GAP_PX;
  let top = rect.top + rect.height / 2 - tipH / 2;

  if (!rtl && left + tipW > vw - VIEWPORT_PAD) {
    left = Math.max(VIEWPORT_PAD, rect.left - tipW - GAP_PX);
  }
  if (rtl && left < VIEWPORT_PAD) {
    left = Math.min(vw - tipW - VIEWPORT_PAD, rect.right + GAP_PX);
  }
  top = Math.min(Math.max(VIEWPORT_PAD, top), vh - tipH - VIEWPORT_PAD);
  left = Math.min(Math.max(VIEWPORT_PAD, left), vw - tipW - VIEWPORT_PAD);

  return { top, left };
}

/**
 * Compact professional tooltip — portaled to escape sidebar overflow.
 * Supplementary only; triggers must keep their own accessible names.
 */
export function ExitsTooltip({
  content,
  children,
  disabled = false,
  className,
  contentClassName,
  testId = "exits-tooltip",
}: ExitsTooltipProps) {
  const tipId = useId();
  const triggerRef = useRef<HTMLElement | null>(null);
  const tipRef = useRef<HTMLDivElement | null>(null);
  const openTimer = useRef<number | null>(null);
  const [open, setOpen] = useState(false);
  const [coords, setCoords] = useState<Coords | null>(null);

  const clearOpenTimer = useCallback(() => {
    if (openTimer.current != null) {
      window.clearTimeout(openTimer.current);
      openTimer.current = null;
    }
  }, []);

  const close = useCallback(() => {
    clearOpenTimer();
    setOpen(false);
    setCoords(null);
  }, [clearOpenTimer]);

  const scheduleOpen = useCallback(
    (delayMs: number) => {
      if (disabled || !content.trim()) {
        return;
      }
      clearOpenTimer();
      openTimer.current = window.setTimeout(() => setOpen(true), delayMs);
    },
    [clearOpenTimer, content, disabled],
  );

  useEffect(() => {
    if (disabled) {
      close();
    }
  }, [close, disabled]);

  useEffect(() => () => clearOpenTimer(), [clearOpenTimer]);

  useEffect(() => {
    if (!open || !triggerRef.current || !tipRef.current) {
      return;
    }
    const update = () => {
      if (!triggerRef.current || !tipRef.current) return;
      setCoords(computePosition(triggerRef.current, tipRef.current));
    };
    update();
    window.addEventListener("scroll", update, true);
    window.addEventListener("resize", update);
    return () => {
      window.removeEventListener("scroll", update, true);
      window.removeEventListener("resize", update);
    };
  }, [open, content]);

  if (!isValidElement(children)) {
    return children as ReactNode;
  }

  const child = children as ReactElement<{
    ref?: Ref<HTMLElement>;
    className?: string;
    onMouseEnter?: (event: MouseEvent) => void;
    onMouseLeave?: (event: MouseEvent) => void;
    onFocus?: (event: FocusEvent) => void;
    onBlur?: (event: FocusEvent) => void;
    "aria-describedby"?: string;
  }>;

  const describedBy =
    open && !disabled
      ? [child.props["aria-describedby"], tipId].filter(Boolean).join(" ")
      : child.props["aria-describedby"];

  const trigger = cloneElement(child, {
    ref: mergeRefs(triggerRef, child.props.ref),
    className: cn(child.props.className, className),
    "aria-describedby": describedBy,
    onMouseEnter: (event: MouseEvent) => {
      child.props.onMouseEnter?.(event);
      scheduleOpen(HOVER_DELAY_MS);
    },
    onMouseLeave: (event: MouseEvent) => {
      child.props.onMouseLeave?.(event);
      close();
    },
    onFocus: (event: FocusEvent) => {
      child.props.onFocus?.(event);
      scheduleOpen(FOCUS_DELAY_MS);
    },
    onBlur: (event: FocusEvent) => {
      child.props.onBlur?.(event);
      close();
    },
  });

  const style: CSSProperties | undefined = coords
    ? { top: coords.top, left: coords.left, zIndex: TOOLTIP_Z_INDEX }
    : { top: 0, left: 0, zIndex: TOOLTIP_Z_INDEX, visibility: "hidden" };

  return (
    <>
      {trigger}
      {open && !disabled && typeof document !== "undefined"
        ? createPortal(
            <div
              ref={tipRef}
              id={tipId}
              role="tooltip"
              data-testid={testId}
              className={cn("exits-tooltip", contentClassName)}
              style={style}
            >
              {content}
            </div>,
            document.body,
          )
        : null}
    </>
  );
}

export const EXITS_TOOLTIP_HOVER_DELAY_MS = HOVER_DELAY_MS;
