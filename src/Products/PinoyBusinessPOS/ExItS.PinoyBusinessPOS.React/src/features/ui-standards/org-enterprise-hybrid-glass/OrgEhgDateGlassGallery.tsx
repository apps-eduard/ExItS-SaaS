import type { ReactNode } from "react";
import {
  OrgEhgDateGlassField,
  OrgEhgDateGlassPopover,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassField";
import { OrgEhgDateGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassPlayground";
import { ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY } from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";

function Sample({ caption, children }: { caption: string; children: ReactNode }) {
  return (
    <div className="org-ehg-date-glass-sample">
      {children}
      <span className="org-ehg-date-glass-sample__caption">{caption}</span>
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="org-ehg-date-glass-panel__section">
      <h3 className="org-ehg-date-glass-panel__section-title">{title}</h3>
      <div className="org-ehg-date-glass-panel__row">{children}</div>
    </section>
  );
}

function ModePanel({
  theme,
  title,
  testId,
}: {
  theme: "light" | "dark";
  title: string;
  testId: string;
}) {
  return (
    <div
      className="org-ehg org-ehg-pg-bg org-ehg-date-glass-panel"
      data-org-ehg
      data-org-ehg-theme={theme}
      data-bg-option="soft-ambient"
      data-testid={testId}
      style={{
        ["--org-ehg-bg-intensity" as string]: String(ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY),
      }}
    >
      <div className="org-ehg-bg-lab__base" aria-hidden />
      <div className="org-ehg-bg-lab__decor" aria-hidden />
      <div className="org-ehg-pg-bg__content org-ehg-date-glass-panel__body">
        <header className="org-ehg-date-glass-panel__header">
          <h2 className="org-ehg-date-glass-panel__title">{title} – Date &amp; Date-Time</h2>
          <p className="org-ehg-date-glass-panel__blurb">
            Locked: Quasar medium datetime · md · popover · focus-thick-below. Time is a
            3-value glass roll (tap above/below; HH/MM typeable; no carets).
          </p>
        </header>

        <Section title="1. Default · Quasar date &amp; time">
          <Sample caption="Quasar · Medium datetime (locked)">
            <OrgEhgDateGlassField
              variety="quasar"
              kind="datetime"
              intensity="medium"
              size="md"
              focusAnimation="thick-below"
              showPopover="none"
              testId={`${testId}-quasar-default`}
            />
          </Sample>
          <Sample caption="Quasar · Soft">
            <OrgEhgDateGlassField variety="quasar" kind="datetime" intensity="soft" showPopover="none" />
          </Sample>
          <Sample caption="Quasar · Strong">
            <OrgEhgDateGlassField variety="quasar" kind="datetime" intensity="strong" showPopover="none" />
          </Sample>
          <Sample caption="Quasar · Focused">
            <OrgEhgDateGlassField
              variety="quasar"
              kind="datetime"
              state="focused"
              focusAnimation="thick-below"
              showPopover="none"
            />
          </Sample>
        </Section>

        <Section title="2. Date Fields">
          <Sample caption="Solid">
            <OrgEhgDateGlassField intensity="solid" kind="date" showPopover="none" />
          </Sample>
          <Sample caption="Soft Glass">
            <OrgEhgDateGlassField
              intensity="soft"
              kind="date"
              showPopover="none"
              testId={`${testId}-soft`}
            />
          </Sample>
          <Sample caption="Medium Glass">
            <OrgEhgDateGlassField intensity="medium" kind="date" showPopover="none" />
          </Sample>
          <Sample caption="Strong Glass">
            <OrgEhgDateGlassField intensity="strong" kind="date" showPopover="none" />
          </Sample>
        </Section>

        <Section title="3. Date-Time Glass">
          <Sample caption="Medium Glass (default)">
            <OrgEhgDateGlassField intensity="medium" kind="datetime" showPopover="none" />
          </Sample>
          <Sample caption="Soft Glass">
            <OrgEhgDateGlassField intensity="soft" kind="datetime" showPopover="none" />
          </Sample>
          <Sample caption="Strong Glass">
            <OrgEhgDateGlassField intensity="strong" kind="datetime" showPopover="none" />
          </Sample>
          <Sample caption="Ultra Glass">
            <OrgEhgDateGlassField intensity="ultra" kind="datetime" showPopover="none" />
          </Sample>
        </Section>

        <Section title="4. Interaction States">
          <Sample caption="Default">
            <OrgEhgDateGlassField state="default" showPopover="none" />
          </Sample>
          <Sample caption="Hover">
            <OrgEhgDateGlassField state="hover" showPopover="none" />
          </Sample>
          <Sample caption="Focused">
            <OrgEhgDateGlassField
              state="focused"
              focusAnimation="thick-below"
              showPopover="none"
              testId={`${testId}-focused`}
            />
          </Sample>
          <Sample caption="Disabled">
            <OrgEhgDateGlassField state="disabled" showPopover="none" />
          </Sample>
          <Sample caption="Error">
            <OrgEhgDateGlassField state="error" hint="Invalid date" showPopover="none" />
          </Sample>
          <Sample caption="Success">
            <OrgEhgDateGlassField state="success" hint="Confirmed" showPopover="none" />
          </Sample>
        </Section>

        <Section title="5. Size Variations">
          <Sample caption="Compact">
            <OrgEhgDateGlassField size="sm" showPopover="none" />
          </Sample>
          <Sample caption="Standard">
            <OrgEhgDateGlassField size="md" showPopover="none" />
          </Sample>
          <Sample caption="Large">
            <OrgEhgDateGlassField size="lg" showPopover="none" />
          </Sample>
        </Section>

        <Section title="6. Picker Types">
          <Sample caption="Single Date">
            <OrgEhgDateGlassField kind="date" showPopover="none" />
          </Sample>
          <Sample caption="Date Range">
            <OrgEhgDateGlassField kind="range" showPopover="none" />
          </Sample>
          <Sample caption="Date & Time Glass">
            <OrgEhgDateGlassField kind="datetime" intensity="medium" showPopover="none" />
          </Sample>
          <Sample caption="Month Picker">
            <OrgEhgDateGlassField kind="month" showPopover="none" />
          </Sample>
        </Section>

        <section className="org-ehg-date-glass-panel__section">
          <h3 className="org-ehg-date-glass-panel__section-title">7. Android popovers (default)</h3>
          <div className="org-ehg-date-glass-panel__popovers">
            <Sample caption="Android · Date">
              <OrgEhgDateGlassPopover
                variety="android"
                variant="date"
                testId={`${testId}-popover-date`}
              />
            </Sample>
            <Sample caption="Android · Date & time">
              <OrgEhgDateGlassPopover
                variety="android"
                variant="datetime"
                testId={`${testId}-popover-datetime`}
              />
            </Sample>
          </div>
        </section>

        <section className="org-ehg-date-glass-panel__section">
          <h3 className="org-ehg-date-glass-panel__section-title">8. Sidebar popovers (alternate)</h3>
          <div className="org-ehg-date-glass-panel__popovers">
            <Sample caption="Sidebar · Quick Select">
              <OrgEhgDateGlassPopover variety="sidebar" variant="date" />
            </Sample>
            <Sample caption="Sidebar · Time list">
              <OrgEhgDateGlassPopover variety="sidebar" variant="datetime" />
            </Sample>
          </div>
        </section>

        <section className="org-ehg-date-glass-panel__section">
          <h3 className="org-ehg-date-glass-panel__section-title">
            9. Quasar popovers (glass time roll)
          </h3>
          <div className="org-ehg-date-glass-panel__popovers">
            <Sample caption="Quasar · Calendar (month / year)">
              <OrgEhgDateGlassPopover
                variety="quasar"
                intensity="medium"
                variant="date"
                testId={`${testId}-popover-quasar-date`}
              />
            </Sample>
            <Sample caption="Quasar · Date &amp; glass HH/MM roll">
              <OrgEhgDateGlassPopover
                variety="quasar"
                intensity="medium"
                variant="datetime"
                testId={`${testId}-popover-quasar-datetime`}
              />
            </Sample>
            <Sample caption="Quasar · Time roll only">
              <OrgEhgDateGlassPopover
                variety="quasar"
                intensity="medium"
                variant="time"
                testId={`${testId}-popover-quasar-time`}
              />
            </Sample>
            <Sample caption="Quasar · Date range">
              <OrgEhgDateGlassPopover
                variety="quasar"
                intensity="medium"
                variant="range"
                value="Feb 20, 2025 → Feb 27, 2025"
                testId={`${testId}-popover-quasar-range`}
              />
            </Sample>
          </div>
        </section>
      </div>
    </div>
  );
}

/**
 * Organization — Enterprise Hybrid Glass Date Styles (UI Standards / DEV only).
 */
export function OrgEhgDateGlassGallery() {
  return (
    <div className="org-ehg-date-glass-gallery" data-testid="ui-standard-org-ehg-date-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Date glass</strong>
        {" — "}
        Locked: ext org-ehg date-glass quasar medium datetime md popover focus-thick-below.
        Quasar time = 3-value glass roll (tap neighbors; typeable HH/MM; Clear closes).
      </p>
      <OrgEhgDateGlassPlayground />
      <div className="org-ehg-date-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-date-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-date-glass-dark" />
      </div>
    </div>
  );
}
