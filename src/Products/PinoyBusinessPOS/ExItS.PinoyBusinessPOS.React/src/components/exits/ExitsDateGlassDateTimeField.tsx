import { useState } from "react";
import {
  ORG_EHG_DATE_GLASS_DEFAULT_FOCUS,
  ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  ORG_EHG_DATE_GLASS_DEFAULT_KIND,
  ORG_EHG_DATE_GLASS_DEFAULT_SIZE,
  ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-date-glass-global-default";
import {
  OrgEhgDateGlassField,
  type OrgEhgDateGlassIntensity,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassField";
import "@/features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css";

export type ExitsDateGlassDateTimeFieldProps = {
  label: string;
  /** datetime-local value `YYYY-MM-DDTHH:mm` (same as SoftGlassDateTimeField). */
  value: string;
  onChange: (next: string) => void;
  testId?: string;
  className?: string;
  /** Override locked medium when a product recipe names soft / strong / etc. */
  intensity?: OrgEhgDateGlassIntensity;
  /** Override locked popover (product modals often use dialog). */
  surface?: "popover" | "dialog";
};

function pad2(n: number): string {
  return String(n).padStart(2, "0");
}

/** Convert datetime-local → Org EHG Quasar display (`MM/DD/YYYY  h:mm AM`). */
export function localDateTimeToOrgEhgDisplay(local: string): string {
  const trimmed = local.trim();
  if (!trimmed) return "";
  const match = trimmed.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
  if (!match) return "";
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  let hour24 = Number(match[4]);
  const minute = match[5];
  if (![year, month, day, hour24].every((n) => Number.isFinite(n)) || !minute) return "";
  const period = hour24 >= 12 ? "PM" : "AM";
  const hour12 = hour24 % 12 || 12;
  return `${pad2(month)}/${pad2(day)}/${year}  ${hour12}:${minute} ${period}`;
}

/** Convert Org EHG Quasar display → datetime-local. */
export function orgEhgDisplayToLocalDateTime(display: string): string {
  const trimmed = display.trim();
  if (!trimmed) return "";
  const match = trimmed.match(
    /^(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{1,2}):(\d{2})\s*(AM|PM)$/i,
  );
  if (!match) return "";
  let hour = Number(match[4]);
  const period = match[6]!.toUpperCase();
  if (period === "PM" && hour < 12) hour += 12;
  if (period === "AM" && hour === 12) hour = 0;
  return `${match[3]}-${pad2(Number(match[1]))}-${pad2(Number(match[2]))}T${pad2(hour)}:${pad2(Number(match[5]))}`;
}

/**
 * Product Org EHG Date glass datetime field (locked recipe).
 * Default command: ext org-ehg date-glass quasar medium datetime md popover focus-thick-below
 * Checkout pickup uses soft + popover: ext org-ehg date-glass quasar soft datetime md popover focus-thick-below
 * Value format stays datetime-local for product forms.
 */
export function ExitsDateGlassDateTimeField({
  label,
  value,
  onChange,
  testId,
  className,
  intensity = ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  surface = ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
}: ExitsDateGlassDateTimeFieldProps) {
  const [open, setOpen] = useState(false);
  const display = localDateTimeToOrgEhgDisplay(value);

  return (
    <div className={className}>
      <OrgEhgDateGlassField
        label={label}
        value={display}
        open={open}
        onOpenChange={setOpen}
        onChange={(next) => onChange(orgEhgDisplayToLocalDateTime(next))}
        intensity={intensity}
        variety={ORG_EHG_DATE_GLASS_DEFAULT_VARIETY}
        kind={ORG_EHG_DATE_GLASS_DEFAULT_KIND}
        size={ORG_EHG_DATE_GLASS_DEFAULT_SIZE}
        focusAnimation={ORG_EHG_DATE_GLASS_DEFAULT_FOCUS}
        surface={surface}
        showPopover="datetime"
        placeholder="Select date & time"
        testId={testId}
      />
      {/* Hidden bridge for tests / automation — datetime-local. */}
      <input
        type="datetime-local"
        className="sr-only"
        tabIndex={-1}
        aria-hidden
        data-testid={testId ? `${testId}-value` : undefined}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  );
}
