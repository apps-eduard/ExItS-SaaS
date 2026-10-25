import {
  useEffect,
  useId,
  useRef,
  useState,
  type CSSProperties,
  type MouseEvent as ReactMouseEvent,
} from "react";
import { createPortal } from "react-dom";
import { CalendarDays } from "lucide-react";
import {
  isoDateToDisplay,
  SOFT_GLASS_DATE_INPUT_PLACEHOLDER,
  softGlassComboDateHeading,
  todayIsoDate,
  UiStandardsQdate,
} from "@/features/ui-standards/UiStandardsDateGallery";
import { UiStandardsQinput } from "@/features/ui-standards/UiStandardsInputGallery";
import { SOFT_GLASS_DATETIME_COLOR } from "@/features/ui-standards/soft-glass-datetime-global-default";
import { useScalePresence } from "@/features/ui-standards/soft-glass-scale-presence";
import "@/features/ui-standards/exits-qfield.css";
import "@/features/ui-standards/exits-datetime-roll.css";

const SOFT_GLASS_INK = "#ffffff";
const OPEN_SKIP_DISMISS_MS = 320;

export type SoftGlassDateFieldProps = {
  label: string;
  value: string;
  onChange: (next: string) => void;
  /** ISO yyyy-MM-dd — dates before this are rejected on pick. */
  minDate?: string;
  /** ISO yyyy-MM-dd — dates after this are rejected on pick. */
  maxDate?: string;
  /** Return true to reject a candidate ISO date (closed days, etc.). */
  isDateDisabled?: (isoDate: string) => boolean;
  testId?: string;
  clearable?: boolean;
};

/**
 * Soft Glass date-only picker (no hour/minute). Prefer Soft Glass Roll calendar shell.
 */
export function SoftGlassDateField({
  label,
  value,
  onChange,
  minDate,
  maxDate,
  isDateDisabled,
  testId = "soft-glass-date",
  clearable = true,
}: SoftGlassDateFieldProps) {
  const reactId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const panelRef = useRef<HTMLDivElement | null>(null);
  const skipDismissRef = useRef(false);
  const iso = /^\d{4}-\d{2}-\d{2}$/.test(value.trim()) ? value.trim() : "";
  const [open, setOpen] = useState(false);
  const mounted = useScalePresence(open);
  const [draftDate, setDraftDate] = useState(iso);
  const display = iso ? isoDateToDisplay(iso) : "";
  const liveDate = draftDate || todayIsoDate();
  const headerYear = liveDate.slice(0, 4);
  const headerDate = softGlassComboDateHeading(liveDate);
  const shellStyle = {
    "--qdate-color": SOFT_GLASS_DATETIME_COLOR,
    "--qdate-ink": SOFT_GLASS_INK,
  } as CSSProperties;

  useEffect(() => {
    if (!open) return;
    setDraftDate(iso);
  }, [open, iso]);

  useEffect(() => {
    if (!open) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
        setOpen(false);
      }
    }
    document.addEventListener("keydown", onKeyDown, true);
    return () => document.removeEventListener("keydown", onKeyDown, true);
  }, [open]);

  function acceptDate(next: string): boolean {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(next)) return false;
    if (minDate && next < minDate) return false;
    if (maxDate && next > maxDate) return false;
    if (isDateDisabled?.(next)) return false;
    return true;
  }

  function commit(next: string) {
    if (!acceptDate(next)) return;
    setDraftDate(next);
    onChange(next);
  }

  function openPop() {
    skipDismissRef.current = true;
    setOpen(true);
    window.setTimeout(() => {
      skipDismissRef.current = false;
    }, OPEN_SKIP_DISMISS_MS);
  }

  function closePop() {
    if (skipDismissRef.current) return;
    setOpen(false);
  }

  function onTriggerClick(event: ReactMouseEvent<HTMLDivElement>) {
    const target = event.target as HTMLElement;
    if (target.closest(".ui-standards-qinput__clear")) return;
    if (!target.closest(".ui-standards-qinput__box")) return;
    if (open) {
      setOpen(false);
      return;
    }
    openPop();
  }

  const panel = mounted ? (
    <div
      ref={panelRef}
      id={`${reactId}-panel`}
      className="exits-sg-datetime__combo ui-standards-qdate ui-standards-qdate--soft-glass exits-sg-datetime__panel--in-portal"
      style={shellStyle}
      data-exits-sg-datetime-panel=""
      data-transition="scale"
      data-state={open ? "open" : "closed"}
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
              commit(next);
              setOpen(false);
            }
          }}
          onClear={() => {
            setDraftDate("");
            onChange("");
            setOpen(false);
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
  ) : null;

  const portalLayer =
    panel && typeof document !== "undefined"
      ? createPortal(
          <div
            className="exits-sg-datetime__portal"
            data-state={open ? "open" : "closed"}
            data-testid={`${testId}-portal`}
          >
            <button
              type="button"
              className="exits-sg-datetime__scrim"
              aria-label="Close"
              data-testid={`${testId}-scrim`}
              onClick={closePop}
            />
            <div className="exits-sg-datetime__portal-stage" data-panels="combo">
              {panel}
            </div>
          </div>,
          document.body,
        )
      : null;

  return (
    <div
      ref={rootRef}
      className="exits-sg-datetime"
      data-testid={testId}
      data-open={open ? "true" : undefined}
    >
      <div className="exits-sg-datetime__trigger" onClick={onTriggerClick}>
        <UiStandardsQinput
          label={label}
          value={display}
          readOnly
          placeholder={SOFT_GLASS_DATE_INPUT_PLACEHOLDER}
          accent={SOFT_GLASS_DATETIME_COLOR}
          fill={SOFT_GLASS_DATETIME_COLOR}
          ink={SOFT_GLASS_INK}
          appearance="outlined"
          clearable={clearable && Boolean(iso)}
          clearMark="reset"
          onClear={() => {
            setDraftDate("");
            onChange("");
          }}
          suffix={<CalendarDays className="ui-standards-qinput__picker-icon" aria-hidden />}
          testId={`${testId}-input`}
        />
      </div>
      {portalLayer}
    </div>
  );
}
