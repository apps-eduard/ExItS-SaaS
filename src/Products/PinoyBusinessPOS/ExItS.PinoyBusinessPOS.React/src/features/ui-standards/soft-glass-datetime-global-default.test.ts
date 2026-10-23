import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  softGlassDateTimeInputPlaceholder,
  SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER,
  SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER_24,
} from "@/features/ui-standards/UiStandardsDateGallery";
import {
  SOFT_GLASS_DATE_COMMAND,
  SOFT_GLASS_DATE_ICON,
  SOFT_GLASS_DATE_PLACEHOLDER,
  SOFT_GLASS_DATETIME_APPEARANCE,
  SOFT_GLASS_DATETIME_COLOR,
  SOFT_GLASS_DATETIME_COMMAND,
  SOFT_GLASS_DATETIME_DEFAULT_EMPTY,
  SOFT_GLASS_DATETIME_DEFAULT_HOUR24,
  SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL,
  SOFT_GLASS_DATETIME_FORMAT_PLACEMENT,
  SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL,
  SOFT_GLASS_DATETIME_ICON,
  SOFT_GLASS_DATETIME_COMBINED_CARD,
  SOFT_GLASS_DATETIME_INDEPENDENT_PANELS,
  SOFT_GLASS_DATETIME_SEQUENTIAL,
  SOFT_GLASS_DATETIME_NOW_COMMITS_AND_CLOSES,
  SOFT_GLASS_DATETIME_NOW_PLACEMENT,
  SOFT_GLASS_DATETIME_OPEN_SHOWS_CURRENT,
  SOFT_GLASS_DATETIME_PAIR_COMMAND,
  SOFT_GLASS_DATETIME_PLACEHOLDER,
  SOFT_GLASS_DATETIME_PLACEHOLDER_24,
  SOFT_GLASS_DATETIME_RESET_MARK,
  SOFT_GLASS_DATETIME_ROLL_QUICK_SELECT,
  SOFT_GLASS_DATETIME_SHOW_SET,
  SOFT_GLASS_DATETIME_TITLE,
  SOFT_GLASS_DATETIME_TODAY_PLACEMENT,
  SOFT_GLASS_DATETIME_TYPEABLE,
  SOFT_GLASS_DATETIME_VARIETY,
} from "@/features/ui-standards/soft-glass-datetime-global-default";
import {
  SOFT_GLASS_PRIMARY_FIELDS_LABEL_DURATION,
  SOFT_GLASS_PRIMARY_FIELDS_LABEL_LIFT,
} from "@/features/ui-standards/soft-glass-primary-fields-global-default";

const css = readFileSync(resolve(__dirname, "ui-standards.css"), "utf8");
const rollCss = readFileSync(resolve(__dirname, "exits-datetime-roll.css"), "utf8");
const gallery = readFileSync(resolve(__dirname, "UiStandardsDateGallery.tsx"), "utf8");
const productField = readFileSync(
  resolve(__dirname, "../../components/exits/SoftGlassDateTimeField.tsx"),
  "utf8",
);

describe("Soft Glass date / date-time global default", () => {
  it("locks Soft Glass Roll · Primary recipe", () => {
    expect(SOFT_GLASS_DATETIME_COMMAND).toBe("ext soft-glass date datetime roll global-default");
    expect(SOFT_GLASS_DATE_COMMAND).toBe("ext soft-glass date roll global-default");
    expect(SOFT_GLASS_DATETIME_PAIR_COMMAND).toBe("ext soft-glass datetime roll global-default");
    expect(SOFT_GLASS_DATETIME_TITLE).toBe("Soft Glass Roll · Primary, global default");
    expect(SOFT_GLASS_DATETIME_COLOR).toBe("var(--exits-primary)");
    expect(SOFT_GLASS_DATETIME_APPEARANCE).toBe("soft-glass");
    expect(SOFT_GLASS_DATETIME_VARIETY).toBe("roll");
    expect(SOFT_GLASS_DATE_ICON).toBe("calendar-days");
    expect(SOFT_GLASS_DATETIME_ICON).toBe("calendar-clock");
    expect(SOFT_GLASS_DATETIME_DEFAULT_EMPTY).toBe(true);
    expect(SOFT_GLASS_DATETIME_TYPEABLE).toBe(false);
    expect(SOFT_GLASS_DATE_PLACEHOLDER).toBe("mm/dd/yyyy");
    expect(SOFT_GLASS_DATETIME_DEFAULT_HOUR24).toBe(false);
    expect(SOFT_GLASS_DATETIME_PLACEHOLDER).toBe("mm/dd/yyyy hh:mm am");
    expect(SOFT_GLASS_DATETIME_PLACEHOLDER_24).toBe("mm/dd/yyyy hh:mm");
    expect(SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER).toBe(SOFT_GLASS_DATETIME_PLACEHOLDER);
    expect(SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER_24).toBe(SOFT_GLASS_DATETIME_PLACEHOLDER_24);
    expect(softGlassDateTimeInputPlaceholder(false)).toBe("mm/dd/yyyy hh:mm am");
    expect(softGlassDateTimeInputPlaceholder(true)).toBe("mm/dd/yyyy hh:mm");
    expect(SOFT_GLASS_DATETIME_RESET_MARK).toBe("reset");
    expect(SOFT_GLASS_DATETIME_TODAY_PLACEMENT).toBe("header");
    expect(SOFT_GLASS_DATETIME_NOW_PLACEMENT).toBe("footer");
    expect(SOFT_GLASS_DATETIME_NOW_COMMITS_AND_CLOSES).toBe(true);
    expect(SOFT_GLASS_DATETIME_FORMAT_PLACEMENT).toBe("header");
    expect(SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL).toBe("24H");
    expect(SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL).toBe("12H");
    expect(SOFT_GLASS_DATETIME_COMBINED_CARD).toBe(true);
    expect(SOFT_GLASS_DATETIME_INDEPENDENT_PANELS).toBe(false);
    expect(SOFT_GLASS_DATETIME_SEQUENTIAL).toBe(false);
    expect(SOFT_GLASS_DATETIME_SHOW_SET).toBe(true);
    expect(SOFT_GLASS_DATETIME_ROLL_QUICK_SELECT).toBe(true);
    expect(SOFT_GLASS_DATETIME_OPEN_SHOWS_CURRENT).toBe(true);
  });

  it("keeps the Soft Glass Roll showcase on the lock", () => {
    expect(gallery).toContain(SOFT_GLASS_DATETIME_TITLE);
    expect(gallery).toContain(`clearMark="${SOFT_GLASS_DATETIME_RESET_MARK}"`);
    expect(gallery).toContain(`placeholder={SOFT_GLASS_DATE_INPUT_PLACEHOLDER}`);
    expect(gallery).toContain("softGlassDateTimeInputPlaceholder(dateTimeHour24)");
    expect(gallery).toContain("SOFT_GLASS_DATETIME_DEFAULT_HOUR24");
    expect(gallery).toContain("readOnly");
    expect(gallery).not.toContain("onDateTextChange");
    expect(gallery).not.toContain("maskSoftGlassDateTimeInput");
    expect(gallery).toContain(`todayPlacement="${SOFT_GLASS_DATETIME_TODAY_PLACEMENT}"`);
    expect(gallery).toContain("exits-sg-datetime__combo");
    expect(gallery).toContain("exits-sg-datetime__combo-cancel");
    expect(gallery).toContain("exits-sg-datetime__combo-now");
    expect(gallery).toContain("setDateTimeOpen(false)");
    expect(gallery).toContain("softGlassComboDateHeading");
    expect(gallery).toContain("ui-standards-qdate__year");
    expect(gallery).toContain("embedded");
    expect(gallery).toContain("CalendarDays");
    expect(gallery).toContain("CalendarClock");
    expect(css).toContain(".ui-standards-soft-glass-roll-card");
    expect(css).toContain('@import "./exits-datetime-roll.css"');
    expect(css).toContain("cursor: pointer");
    expect(css).toContain("caret-color: transparent");
    expect(css).toContain(".ui-standards-soft-glass-roll-card__field[data-open]");
    expect(css).toContain("top: 0 !important");
    expect(css).toContain(`transition-duration: ${SOFT_GLASS_PRIMARY_FIELDS_LABEL_DURATION} !important`);
    expect(css).toContain(`translateY(${SOFT_GLASS_PRIMARY_FIELDS_LABEL_LIFT})`);
    expect(rollCss).toContain(".ui-standards-qdate__set");
    expect(rollCss).toContain(".ui-standards-qdate__today--header");
    expect(rollCss).toContain(".ui-standards-qdate__roll-col");
    expect(rollCss).toContain("flex-direction: column");
    expect(rollCss).toContain(".ui-standards-qdate--soft-glass .ui-standards-qdate__roll-item--current");
    expect(rollCss).toContain("border-radius: 0.65rem");
    expect(rollCss).toContain(".exits-sg-datetime__combo");
    expect(productField).toContain('import "@/features/ui-standards/exits-datetime-roll.css"');
    expect(productField).toContain('appearance="soft-glass"');
    expect(productField).toContain('variety="roll"');
    expect(productField).toContain("exits-sg-datetime__combo");
    expect(productField).toContain("exits-sg-datetime__combo-cancel");
    expect(productField).toContain("softGlassComboDateHeading");
    expect(productField).toContain("ui-standards-qdate__year");
    expect(productField).toContain("embedded");
    expect(productField).toContain("onSet");
    expect(productField).toContain('closest(".ui-standards-qinput__box")');
    expect(productField).toContain("createPortal");
    expect(productField).toContain("exits-sg-datetime__portal");
    expect(productField).toContain("exits-sg-datetime__portal-stage");
    expect(productField).toContain("exits-sg-datetime__panel--in-portal");
  });
});
