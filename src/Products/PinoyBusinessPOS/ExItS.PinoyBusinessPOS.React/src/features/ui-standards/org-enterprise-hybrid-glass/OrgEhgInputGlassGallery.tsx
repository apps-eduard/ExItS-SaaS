import { Mail, Search } from "lucide-react";
import type { ReactNode } from "react";
import { OrgEhgInputGlassField } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInputGlassField";
import { OrgEhgInputGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInputGlassPlayground";

function Sample({
  caption,
  children,
}: {
  caption: string;
  children: ReactNode;
}) {
  return (
    <div className="org-ehg-input-glass-sample">
      {children}
      <span className="org-ehg-input-glass-sample__caption">{caption}</span>
    </div>
  );
}

function fieldTheme(theme: "light" | "dark") {
  return { menuTheme: theme } as const;
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="org-ehg-input-glass-panel__section">
      <h3 className="org-ehg-input-glass-panel__section-title">{title}</h3>
      <div className="org-ehg-input-glass-panel__row">{children}</div>
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
      className="org-ehg org-ehg-input-glass-panel"
      data-org-ehg-theme={theme}
      data-testid={testId}
    >
      <header className="org-ehg-input-glass-panel__header">
        <h2 className="org-ehg-input-glass-panel__title">{title} – Input Glass Styles</h2>
      </header>

      <Section title="1. Glass Variations (Primary)">
        <Sample caption="0% transparency">
          <OrgEhgInputGlassField intensity="solid" label="Solid" placeholder="Enter text…" />
        </Sample>
        <Sample caption="10–20% transparency">
          <OrgEhgInputGlassField
            intensity="soft"
            label="Soft Glass"
            placeholder="Enter text…"
            testId={`${testId}-soft`}
          />
        </Sample>
        <Sample caption="30–40% transparency">
          <OrgEhgInputGlassField intensity="medium" label="Medium Glass" placeholder="Enter text…" />
        </Sample>
        <Sample caption="50–70% transparency">
          <OrgEhgInputGlassField intensity="strong" label="Strong Glass" placeholder="Enter text…" />
        </Sample>
        <Sample caption="80–90% transparency">
          <OrgEhgInputGlassField intensity="ultra" label="Ultra Glass" placeholder="Enter text…" />
        </Sample>
      </Section>

      <Section title="2. Label Positions">
        <Sample caption="Above">
          <OrgEhgInputGlassField label="Above" labelPlace="above" placeholder="Enter text…" />
        </Sample>
        <Sample caption="None">
          <OrgEhgInputGlassField label="Hidden" labelPlace="none" placeholder="No label" />
        </Sample>
        <Sample caption="Icon top">
          <OrgEhgInputGlassField
            label="Icon top"
            iconPlace="top"
            icon={Mail}
            placeholder="Email…"
          />
        </Sample>
      </Section>

      <Section title="3. Input Types">
        <Sample caption="Text">
          <OrgEhgInputGlassField kind="text" label="Text" placeholder="Full name" />
        </Sample>
        <Sample caption="Email">
          <OrgEhgInputGlassField kind="email" label="Email" iconPlace="left" icon={Mail} />
        </Sample>
        <Sample caption="Password">
          <OrgEhgInputGlassField kind="password" label="Password" value="secret" />
        </Sample>
        <Sample caption="Search">
          <OrgEhgInputGlassField
            kind="search"
            label="Search"
            iconPlace="left"
            icon={Search}
            clearable
            value="query"
          />
        </Sample>
      </Section>

      <Section title="4. Input States">
        <Sample caption="Default">
          <OrgEhgInputGlassField state="default" label="Default" />
        </Sample>
        <Sample caption="Hover">
          <OrgEhgInputGlassField state="hover" label="Hover" />
        </Sample>
        <Sample caption="Focused">
          <OrgEhgInputGlassField state="focused" label="Focused" />
        </Sample>
        <Sample caption="Error">
          <OrgEhgInputGlassField state="error" label="Error" hint="Required" />
        </Sample>
        <Sample caption="Success">
          <OrgEhgInputGlassField state="success" label="Success" hint="Saved" />
        </Sample>
        <Sample caption="Disabled">
          <OrgEhgInputGlassField state="disabled" label="Disabled" value="Locked" />
        </Sample>
      </Section>

      <Section title="5. Icons & Affixes">
        <Sample caption="Leading">
          <OrgEhgInputGlassField label="Leading" iconPlace="left" icon={Mail} />
        </Sample>
        <Sample caption="Trailing">
          <OrgEhgInputGlassField label="Trailing" iconPlace="right" icon={Search} />
        </Sample>
        <Sample caption="Prefix">
          <OrgEhgInputGlassField label="Amount" prefix="₱" placeholder="0.00" />
        </Sample>
        <Sample caption="Suffix">
          <OrgEhgInputGlassField label="Weight" suffix="kg" placeholder="0" />
        </Sample>
      </Section>

      <Section title="6. Selection Controls">
        <Sample caption="Select / Dropdown">
          <OrgEhgInputGlassField
            kind="select"
            label="Select"
            value="credit-card"
            testId={`${testId}-select`}
            {...fieldTheme(theme)}
          />
        </Sample>
        <Sample caption="Combobox">
          <OrgEhgInputGlassField
            kind="select"
            label="Combobox"
            iconPlace="left"
            icon={Search}
            dropdown
            value="cash"
            testId={`${testId}-combobox`}
            {...fieldTheme(theme)}
          />
        </Sample>
        <Sample caption="Clearable">
          <OrgEhgInputGlassField label="Clearable" clearable value="Clear me" />
        </Sample>
      </Section>

      <Section title="7. Animation">
        <Sample caption="Bg shimmer">
          <OrgEhgInputGlassField
            label="Shimmer"
            bgAnimation="shimmer"
            testId={`${testId}-anim-bg-shimmer`}
          />
        </Sample>
        <Sample caption="Bg pulse">
          <OrgEhgInputGlassField label="Pulse" bgAnimation="pulse" />
        </Sample>
        <Sample caption="Bg drift">
          <OrgEhgInputGlassField label="Drift" bgAnimation="drift" />
        </Sample>
        <Sample caption="Focus ring pulse">
          <OrgEhgInputGlassField
            label="Ring pulse"
            state="focused"
            focusAnimation="ring-pulse"
            testId={`${testId}-anim-focus-ring`}
          />
        </Sample>
        <Sample caption="Focus glow">
          <OrgEhgInputGlassField label="Glow" state="focused" focusAnimation="glow" />
        </Sample>
        <Sample caption="Focus breathe">
          <OrgEhgInputGlassField label="Breathe" state="focused" focusAnimation="breathe" />
        </Sample>
        <Sample caption="Focus thick below">
          <OrgEhgInputGlassField
            label="Thick below"
            state="focused"
            focusAnimation="thick-below"
            testId={`${testId}-anim-focus-thick-below`}
          />
        </Sample>
      </Section>

      <section className="org-ehg-input-glass-panel__section">
        <h3 className="org-ehg-input-glass-panel__section-title">8. Glass Intensity Reference</h3>
        <div className="org-ehg-input-glass-panel__scene" data-testid={`${testId}-scene`}>
          <Sample caption="0%">
            <OrgEhgInputGlassField intensity="solid" labelPlace="none" placeholder="Solid" />
          </Sample>
          <Sample caption="10–20%">
            <OrgEhgInputGlassField intensity="soft" labelPlace="none" placeholder="Soft" />
          </Sample>
          <Sample caption="30–40%">
            <OrgEhgInputGlassField intensity="medium" labelPlace="none" placeholder="Medium" />
          </Sample>
          <Sample caption="50–70%">
            <OrgEhgInputGlassField intensity="strong" labelPlace="none" placeholder="Strong" />
          </Sample>
          <Sample caption="80–90%">
            <OrgEhgInputGlassField intensity="ultra" labelPlace="none" placeholder="Ultra" />
          </Sample>
        </div>
      </section>
    </div>
  );
}

/**
 * Organization — Enterprise Hybrid Glass Input Glass Styles (UI Standards / DEV only).
 */
export function OrgEhgInputGlassGallery() {
  return (
    <div className="org-ehg-input-glass-gallery" data-testid="ui-standard-org-ehg-input-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Input Glass Styles</strong>
        {" — "}
        Organization UI Standards visual reference. Rounded fields (8px), Hybrid Glass washes.
        Personal Soft Glass inputs are unchanged.
      </p>
      <OrgEhgInputGlassPlayground />
      <div className="org-ehg-input-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-input-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-input-glass-dark" />
      </div>
    </div>
  );
}
