import { useRef, useState, type MouseEvent as ReactMouseEvent } from "react";
import { CalendarDays } from "lucide-react";
import { SoftGlassDatePopover } from "@/components/exits/SoftGlassDatePopover";
import {
  isoDateToDisplay,
  SOFT_GLASS_DATE_INPUT_PLACEHOLDER,
} from "@/features/ui-standards/UiStandardsDateGallery";
import { UiStandardsQinput } from "@/features/ui-standards/UiStandardsInputGallery";
import { SOFT_GLASS_DATETIME_COLOR } from "@/features/ui-standards/soft-glass-datetime-global-default";
import "@/features/ui-standards/exits-qfield.css";
import "@/features/ui-standards/exits-datetime-roll.css";

const SOFT_GLASS_INK = "#ffffff";

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
 * Soft Glass date-only picker (no hour/minute).
 * Opens as a popover below the field; picking a date commits and closes.
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
  const rootRef = useRef<HTMLDivElement | null>(null);
  const iso = /^\d{4}-\d{2}-\d{2}$/.test(value.trim()) ? value.trim() : "";
  const [open, setOpen] = useState(false);
  const display = iso ? isoDateToDisplay(iso) : "";

  function onTriggerClick(event: ReactMouseEvent<HTMLDivElement>) {
    const target = event.target as HTMLElement;
    if (target.closest(".ui-standards-qinput__clear")) return;
    if (!target.closest(".ui-standards-qinput__box")) return;
    setOpen((prev) => !prev);
  }

  return (
    <div
      ref={rootRef}
      className="exits-sg-datetime"
      data-testid={testId}
      data-open={open ? "true" : undefined}
      data-surface="popover"
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
            onChange("");
          }}
          suffix={<CalendarDays className="ui-standards-qinput__picker-icon" aria-hidden />}
          testId={`${testId}-input`}
        />
      </div>
      <SoftGlassDatePopover
        open={open}
        onOpenChange={setOpen}
        anchorRef={rootRef}
        label={label}
        value={value}
        onChange={onChange}
        minDate={minDate}
        maxDate={maxDate}
        isDateDisabled={isDateDisabled}
        testId={testId}
        clearable={clearable}
      />
    </div>
  );
}
