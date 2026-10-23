import {
  useEffect,
  useId,
  useRef,
  useState,
  type CSSProperties,
  type MouseEvent as ReactMouseEvent,
} from "react";
import { createPortal } from "react-dom";
import { CalendarClock } from "lucide-react";
import {
  isoDateToDisplay,
  softGlassComboDateHeading,
  softGlassDateTimeInputPlaceholder,
  todayIsoDate,
  UiStandardsQdate,
} from "@/features/ui-standards/UiStandardsDateGallery";
import { UiStandardsQinput } from "@/features/ui-standards/UiStandardsInputGallery";
import {
  displayTime,
  nowTime,
  UiStandardsQtime,
} from "@/features/ui-standards/UiStandardsTimeGallery";
import {
  SOFT_GLASS_DATETIME_COLOR,
  SOFT_GLASS_DATETIME_DEFAULT_HOUR24,
  SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL,
  SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL,
} from "@/features/ui-standards/soft-glass-datetime-global-default";
import { useScalePresence } from "@/features/ui-standards/soft-glass-scale-presence";
import { Switch } from "@/components/ui/switch";
import "@/features/ui-standards/exits-qfield.css";
import "@/features/ui-standards/exits-datetime-roll.css";

const SOFT_GLASS_INK = "#ffffff";
/** Ignore the opening gesture + short ghost clicks before outside-dismiss is armed. */
const OPEN_SKIP_DISMISS_MS = 320;

function parseLocalDateTime(value: string): { date: string; time: string } {
  const trimmed = value.trim();
  if (!trimmed) return { date: "", time: "" };
  const [datePart = "", timePart = ""] = trimmed.split("T");
  const [hh = "", mm = ""] = timePart.split(":");
  const time =
    hh !== "" && mm !== ""
      ? `${String(Number(hh) || 0).padStart(2, "0")}:${String(Number(mm) || 0).padStart(2, "0")}`
      : "";
  return {
    date: /^\d{4}-\d{2}-\d{2}$/.test(datePart) ? datePart : "",
    time,
  };
}

function toLocalValue(date: string, time: string): string {
  if (!date) return "";
  return `${date}T${time || "00:00"}`;
}

function softGlassDateTimeDisplay(dateIso: string, time: string, hour24: boolean): string {
  return [dateIso ? isoDateToDisplay(dateIso) : "", time ? displayTime(time, hour24) : ""].filter(Boolean).join(" ");
}

export type SoftGlassDateTimeFieldProps = {
  label: string;
  value: string;
  onChange: (next: string) => void;
  testId?: string;
  clearable?: boolean;
};

/**
 * Soft Glass Roll · Primary, global default — pick-only date + time for Soft Glass dialogs.
 * One Soft Glass card: header (full month + 12·24) + date & time rolls + footer Cancel / Now / Set.
 * Opens in a centered flex portal + scrim above ExitsModal.
 * Value format stays datetime-local (`YYYY-MM-DDTHH:mm`) for product forms.
 * Lock: soft-glass-datetime-global-default.ts / exits-soft-glass-datetime.mdc
 */
export function SoftGlassDateTimeField({
  label,
  value,
  onChange,
  testId = "soft-glass-datetime",
  clearable = true,
}: SoftGlassDateTimeFieldProps) {
  const reactId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const panelRef = useRef<HTMLDivElement | null>(null);
  const skipDismissRef = useRef(false);
  const parts = parseLocalDateTime(value);
  const [open, setOpen] = useState(false);
  const mounted = useScalePresence(open);
  const [hour24, setHour24] = useState<boolean>(SOFT_GLASS_DATETIME_DEFAULT_HOUR24);
  const [draftDate, setDraftDate] = useState(parts.date);
  const [draftTime, setDraftTime] = useState(parts.time);
  /** Hide time rolls while month/day/year quick grid is open. */
  const [dateQuickOpen, setDateQuickOpen] = useState(false);
  const display = softGlassDateTimeDisplay(parts.date, parts.time, hour24);
  const liveDate = draftDate || todayIsoDate();
  const liveTime = draftTime || nowTime();
  const headerYear = liveDate.slice(0, 4);
  const headerDate = softGlassComboDateHeading(liveDate);
  const shellStyle = {
    "--qdate-color": SOFT_GLASS_DATETIME_COLOR,
    "--qdate-ink": SOFT_GLASS_INK,
  } as CSSProperties;

  useEffect(() => {
    if (!open) return;
    setDraftDate(parts.date);
    setDraftTime(parts.time);
    setDateQuickOpen(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- sync drafts from value at open
  }, [open]);

  useEffect(() => {
    if (!open) setDateQuickOpen(false);
  }, [open]);

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

  function commit(date: string, time: string) {
    onChange(toLocalValue(date, time));
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

  /** Only the bordered Soft Glass box opens the roll — not the float-label padding above it. */
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

  function onNow() {
    const date = todayIsoDate();
    const time = nowTime();
    setDraftDate(date);
    setDraftTime(time);
    commit(date, time);
    setOpen(false);
  }

  function onSet() {
    const date = draftDate || todayIsoDate();
    const time = draftTime || nowTime();
    setDraftDate(date);
    setDraftTime(time);
    commit(date, time);
    setOpen(false);
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
        <button
          type="button"
          className="ui-standards-qdate__year"
          tabIndex={-1}
          data-testid={`${testId}-header-year`}
        >
          {headerYear}
        </button>
        <span className="ui-standards-qdate__selected" data-testid={`${testId}-header`}>
          {headerDate}
        </span>
        <div className="exits-sg-datetime__combo-actions">
          <div className="ui-standards-qtime__format ui-standards-qtime__format--header">
            <Switch
              checked={hour24}
              onCheckedChange={setHour24}
              aria-label="Hour format"
              checkedLabel={SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL}
              uncheckedLabel={SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL}
              data-testid={`${testId}-24h`}
            />
          </div>
        </div>
      </div>
      <div className="exits-sg-datetime__combo-body" data-date-quick={dateQuickOpen ? "true" : undefined}>
        <UiStandardsQdate
          color={SOFT_GLASS_DATETIME_COLOR}
          ink={SOFT_GLASS_INK}
          value={liveDate}
          appearance="soft-glass"
          variety="roll"
          embedded
          showValue={false}
          onQuickOpenChange={setDateQuickOpen}
          onChange={(next) => {
            if (typeof next === "string") {
              setDraftDate(next);
              commit(next, draftTime || nowTime());
            }
          }}
          testId={`${testId}-date`}
        />
        {dateQuickOpen ? null : (
          <UiStandardsQtime
            color={SOFT_GLASS_DATETIME_COLOR}
            ink={SOFT_GLASS_INK}
            value={liveTime}
            hour24={hour24}
            onHour24Change={setHour24}
            appearance="soft-glass"
            variety="roll"
            embedded
            showValue={false}
            onChange={(next) => {
              if (typeof next === "string") {
                const date = draftDate || todayIsoDate();
                setDraftTime(next);
                if (!draftDate) setDraftDate(date);
                commit(date, next);
              }
            }}
            testId={`${testId}-time`}
          />
        )}
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
        <button
          type="button"
          className="exits-sg-datetime__combo-now"
          data-testid={`${testId}-now`}
          onClick={onNow}
        >
          Now
        </button>
        <button
          type="button"
          className="ui-standards-qdate__set exits-sg-datetime__combo-set"
          data-testid={`${testId}-set`}
          onClick={onSet}
        >
          Set
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
      data-open={open ? "true" : undefined}
      data-testid={testId}
    >
      <div className="exits-sg-datetime__trigger" onClick={onTriggerClick}>
        <UiStandardsQinput
          label={label}
          value={display}
          readOnly
          placeholder={softGlassDateTimeInputPlaceholder(hour24)}
          accent={SOFT_GLASS_DATETIME_COLOR}
          fill={SOFT_GLASS_DATETIME_COLOR}
          ink={SOFT_GLASS_INK}
          appearance="outlined"
          clearable={clearable}
          clearMark="reset"
          onClear={() => {
            onChange("");
            setDraftDate("");
            setDraftTime("");
            setOpen(false);
          }}
          suffix={<CalendarClock className="ui-standards-qinput__picker-icon" aria-hidden />}
          testId={`${testId}-field`}
        />
      </div>

      {portalLayer}
    </div>
  );
}
