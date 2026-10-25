import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  ORG_EHG_DATE_GLASS_COMMAND,
  ORG_EHG_DATE_GLASS_DEFAULT_FOCUS,
  ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  ORG_EHG_DATE_GLASS_DEFAULT_KIND,
  ORG_EHG_DATE_GLASS_DEFAULT_SIZE,
  ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
  ORG_EHG_DATE_GLASS_QUASAR_FOOTER,
  ORG_EHG_DATE_GLASS_QUASAR_TIME_CARETS,
  ORG_EHG_DATE_GLASS_QUASAR_TIME_EQUAL_ROWS,
  ORG_EHG_DATE_GLASS_QUASAR_TIME_ROLL,
  ORG_EHG_DATE_GLASS_QUASAR_TIME_TYPEABLE_HH_MM,
  ORG_EHG_DATE_GLASS_TITLE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-date-glass-global-default";

const fieldSrc = readFileSync(resolve(__dirname, "OrgEhgDateGlassField.tsx"), "utf8");
const playgroundSrc = readFileSync(resolve(__dirname, "OrgEhgDateGlassPlayground.tsx"), "utf8");
const css = readFileSync(resolve(__dirname, "org-enterprise-hybrid-glass.css"), "utf8");

describe("org-ehg-date-glass-global-default", () => {
  it("locks the Quasar medium datetime popover command", () => {
    expect(ORG_EHG_DATE_GLASS_COMMAND).toBe(
      "ext org-ehg date-glass quasar medium datetime md popover focus-thick-below",
    );
    expect(ORG_EHG_DATE_GLASS_TITLE).toBe("Date glass · Quasar medium datetime");
    expect(ORG_EHG_DATE_GLASS_DEFAULT_VARIETY).toBe("quasar");
    expect(ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY).toBe("medium");
    expect(ORG_EHG_DATE_GLASS_DEFAULT_KIND).toBe("datetime");
    expect(ORG_EHG_DATE_GLASS_DEFAULT_SIZE).toBe("md");
    expect(ORG_EHG_DATE_GLASS_DEFAULT_SURFACE).toBe("popover");
    expect(ORG_EHG_DATE_GLASS_DEFAULT_FOCUS).toBe("thick-below");
    expect(ORG_EHG_DATE_GLASS_QUASAR_TIME_ROLL).toBe(true);
    expect(ORG_EHG_DATE_GLASS_QUASAR_TIME_CARETS).toBe(false);
    expect(ORG_EHG_DATE_GLASS_QUASAR_TIME_TYPEABLE_HH_MM).toBe(true);
    expect(ORG_EHG_DATE_GLASS_QUASAR_TIME_EQUAL_ROWS).toBe(true);
    expect(ORG_EHG_DATE_GLASS_QUASAR_FOOTER).toBe(true);
  });

  it("wires field and playground defaults to the lock", () => {
    expect(fieldSrc).toContain("org-ehg-date-glass-global-default");
    expect(fieldSrc).toContain("ORG_EHG_DATE_GLASS_DEFAULT_SIZE");
    expect(fieldSrc).toContain("ORG_EHG_DATE_GLASS_DEFAULT_FOCUS");
    expect(playgroundSrc).toContain("ORG_EHG_DATE_GLASS_COMMAND");
    expect(playgroundSrc).toContain("ORG_EHG_DATE_GLASS_DEFAULT_FOCUS");
    expect(playgroundSrc).toContain("ORG_EHG_DATE_GLASS_DEFAULT_SIZE");
  });

  it("keeps Quasar time as equal-row glass roll without carets", () => {
    expect(fieldSrc).toContain("QuasarTimeGlassRoll");
    expect(fieldSrc).toContain("org-ehg-date-glass-popover__quasar-time-neighbor");
    expect(fieldSrc).not.toContain("quasar-time-caret");
    expect(css).toContain("--org-ehg-time-row-h");
    expect(css).toContain("org-ehg-date-glass-popover__quasar-time-band");
  });
});

