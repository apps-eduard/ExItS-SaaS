import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type CSSProperties,
  type RefObject,
} from "react";
import { createPortal } from "react-dom";
import {
  softGlassComboDateHeading,
  todayIsoDate,
  UiStandardsQdate,
} from "@/features/ui-standards/UiStandardsDateGallery";
import { SOFT_GLASS_DATETIME_COLOR } from "@/features/ui-standards/soft-glass-datetime-global-default";
import { useScalePresence } from "@/features/ui-standards/soft-glass-scale-presence";
import "@/features/ui-standards/exits-datetime-roll.css";

const SOFT_GLASS_INK = "#ffffff";
const OPEN_SKIP_DISMISS_MS = 320;
const POPOVER_GAP_PX = 8;
const POPOVER_WIDTH_PX = 320;

export type SoftGlassDatePopoverProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Element the popover anchors below (card, field, etc.). */
  anchorRef: RefObject<HTMLElement | null>;
  label: string;
  value: string;
  onChange: (next: string) => void;
  minDate?: string;
  maxDate?: string;
  isDateDisabled?: (isoDate: string) => boolean;
  testId?: string;
  clearable?: boolean;
};

type PopoverCoords = { top: number; left: number; width: number };

/**
 * Soft Glass calendar popover anchored below `anchorRef`.
 * Picking a date commits and closes.
 */
export function SoftGlassDatePopover({
  open,
  onOpenChange,
  anchorRef,
  label,
  value,
  onChange,
  minDate,
  maxDate,
  isDateDisabled,
  testId = "soft-glass-date",
  clearable = true,
}: SoftGlassDatePopoverProps) {
  const reactId = useId();
  const panelRef = useRef<HTMLDivElement | null>(null);
  const skipDismissRef = useRef(false);
  const iso = /^\d{4}-\d{2}-\d{2}$/.test(value.trim()) ? value.trim() : "";
  const mounted = useScalePresence(open);
  const [draftDate, setDraftDate] = useState(iso);
  const [coords, setCoords] = useState<PopoverCoords | null>(null);
  const liveDate = draftDate || todayIsoDate();
  const headerYear = liveDate.slice(0, 4);
  const headerDate = softGlassComboDateHeading(liveDate);
  const shellStyle = {
    "--qdate-color": SOFT_GLASS_DATETIME_COLOR,
    "--qdate-ink": SOFT_GLASS_INK,
  } as CSSProperties;

  function measurePopover(): PopoverCoords | null {
    const trigger = anchorRef.current;
    if (!trigger) return null;
    const rect = trigger.getBoundingClientRect();
    const width = Math.min(POPOVER_WIDTH_PX, Math.max(rect.width, 280), window.innerWidth - 16);
    let left = rect.left;
    if (left + width > window.innerWidth - 8) {
      left = Math.max(8, window.innerWidth - 8 - width);
    }
    if (left < 8) left = 8;
    let top = rect.bottom + POPOVER_GAP_PX;
    const estimatedHeight = panelRef.current?.offsetHeight ?? 360;
    if (top + estimatedHeight > window.innerHeight - 8) {
      const above = rect.top - POPOVER_GAP_PX - estimatedHeight;
      if (above >= 8) top = above;
    }
    return { top, left, width };
  }

  useEffect(() => {
    if (!open) return;
    skipDismissRef.current = true;
    setDraftDate(iso);
    const id = window.setTimeout(() => {
      skipDismissRef.current = false;
    }, OPEN_SKIP_DISMISS_MS);
    return () => window.clearTimeout(id);
  }, [open, iso]);

  useLayoutEffect(() => {
    if (!open) {
      setCoords(null);
      return;
    }
    setCoords(measurePopover());
    const raf = window.requestAnimationFrame(() => setCoords(measurePopover()));
    function onReposition() {
      setCoords(measurePopover());
    }
    window.addEventListener("resize", onReposition);
    window.addEventListener("scroll", onReposition, true);
    return () => {
      window.cancelAnimationFrame(raf);
      window.removeEventListener("resize", onReposition);
      window.removeEventListener("scroll", onReposition, true);
    };
  }, [open, mounted, anchorRef]);

  useEffect(() => {
    if (!open) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
        onOpenChange(false);
      }
    }
    document.addEventListener("keydown", onKeyDown, true);
    return () => document.removeEventListener("keydown", onKeyDown, true);
  }, [open, onOpenChange]);

  function acceptDate(next: string): boolean {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(next)) return false;
    if (minDate && next < minDate) return false;
    if (maxDate && next > maxDate) return false;
    if (isDateDisabled?.(next)) return false;
    return true;
  }

  function closePop() {
    if (skipDismissRef.current) return;
    onOpenChange(false);
  }

  if (!mounted || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <div
      className="exits-sg-datetime__portal exits-sg-datetime__portal--popover"
      data-state={open ? "open" : "closed"}
      data-surface="popover"
      data-testid={`${testId}-portal`}
    >
      <button
        type="button"
        className="exits-sg-datetime__scrim exits-sg-datetime__scrim--popover"
        aria-label="Close"
        data-testid={`${testId}-scrim`}
        onClick={closePop}
      />
      <div
        ref={panelRef}
        id={`${reactId}-panel`}
        className="exits-sg-datetime__combo ui-standards-qdate ui-standards-qdate--soft-glass exits-sg-datetime__panel--in-portal exits-sg-datetime__popover-panel"
        style={{
          ...shellStyle,
          ...(coords
            ? {
                top: coords.top,
                left: coords.left,
                width: coords.width,
              }
            : undefined),
        }}
        data-exits-sg-datetime-panel=""
        data-transition="scale"
        data-state={open ? "open" : "closed"}
        data-surface="popover"
        role="dialog"
        aria-label={label}
        aria-modal="true"
        data-testid={`${testId}-panel`}
        onPointerDown={(event) => event.stopPropagation()}
      >
        <div className="ui-standards-qdate__header exits-sg-datetime__combo-header">
          <button type="button" className="ui-standards-qdate__year" tabIndex={-1}>
            {headerYear}
          </button>
          <span className="ui-standards-qdate__selected" data-testid={`${testId}-header`}>
            {headerDate}
          </span>
        </div>
        <div className="exits-sg-datetime__combo-body">
          <UiStandardsQdate
            color={SOFT_GLASS_DATETIME_COLOR}
            ink={SOFT_GLASS_INK}
            value={liveDate}
            today={minDate && minDate > todayIsoDate() ? minDate : todayIsoDate()}
            showToday
            showClear={clearable}
            todayPlacement="footer"
            appearance="soft-glass"
            variety="calendar"
            embedded
            showValue={false}
            onChange={(next) => {
              if (typeof next === "string" && acceptDate(next)) {
                setDraftDate(next);
                onChange(next);
                onOpenChange(false);
              }
            }}
            onClear={() => {
              setDraftDate("");
              onChange("");
              onOpenChange(false);
            }}
            testId={`${testId}-calendar`}
          />
        </div>
        <div className="exits-sg-datetime__combo-footer">
          <button
            type="button"
            className="exits-sg-datetime__combo-cancel"
            data-testid={`${testId}-cancel`}
            onClick={closePop}
          >
            Cancel
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
