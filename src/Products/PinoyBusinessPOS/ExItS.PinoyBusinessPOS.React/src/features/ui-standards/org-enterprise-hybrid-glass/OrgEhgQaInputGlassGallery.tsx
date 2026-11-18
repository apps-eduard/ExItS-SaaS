import { useState, type ReactNode } from "react";
import { Mail, Search, UserRound, type LucideIcon } from "lucide-react";
import { OrgEhgQaInputGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQaInputGlassPlayground";
import type {
  OrgEhgQaGlassIconShape,
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";

function QaInput({
  label,
  tone = "primary",
  icon: Icon,
  iconShape = "soft",
  size = "md",
  ghost = false,
  placeholder = "Type here…",
  defaultValue = "",
  caption,
  testId,
}: {
  label: string;
  tone?: OrgEhgQaGlassTone;
  icon?: LucideIcon;
  iconShape?: OrgEhgQaGlassIconShape;
  size?: OrgEhgQaGlassSize;
  ghost?: boolean;
  placeholder?: string;
  defaultValue?: string;
  caption?: string;
  testId?: string;
}) {
  const [value, setValue] = useState(defaultValue);
  const fieldShellClass = [
    "org-ehg-qa-glass",
    "org-ehg-qa-input-glass__field",
    "org-ehg-qa-input-glass--density-balance",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    ghost ? "org-ehg-qa-glass--ghost" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="org-ehg-qa-glass-sample">
      <label
        className={`org-ehg-qa-input-glass org-ehg-qa-glass--tone-${tone}`}
        data-testid={testId}
        data-raised={value.trim().length > 0 ? "true" : "false"}
        data-has-icon={Icon ? "true" : "false"}
        data-icon-look={Icon ? "chip" : "none"}
      >
        <span className={fieldShellClass}>
          {Icon ? (
            <span
              className={
                "org-ehg-qa-glass__icon" +
                (iconShape === "rounded" ? " org-ehg-qa-glass__icon--rounded" : "")
              }
              aria-hidden="true"
            >
              <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
            </span>
          ) : null}
          <span className="org-ehg-qa-input-glass__label">{label}</span>
          <input
            className="org-ehg-qa-input-glass__control"
            value={value}
            placeholder={placeholder}
            aria-label={label}
            onChange={(event) => setValue(event.target.value)}
          />
          {value ? (
            <button
              type="button"
              className="org-ehg-qa-input-glass__clear"
              aria-label="Clear"
              onClick={() => setValue("")}
            >
              ×
            </button>
          ) : null}
        </span>
      </label>
      {caption ? <span className="org-ehg-qa-glass-sample__caption">{caption}</span> : null}
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="org-ehg-qa-glass-panel__section">
      <h3 className="org-ehg-qa-glass-panel__section-title">{title}</h3>
      <div className="org-ehg-qa-glass-panel__row">{children}</div>
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
      className="org-ehg org-ehg-qa-glass-panel"
      data-org-ehg-theme={theme}
      data-testid={testId}
    >
      <header className="org-ehg-qa-glass-panel__header">
        <h2 className="org-ehg-qa-glass-panel__title">{title} – Input Glass</h2>
      </header>

      <Section title="1. Tones">
        <QaInput label="Primary" tone="primary" icon={Search} caption="Primary" />
        <QaInput label="Success" tone="success" icon={Mail} caption="Success" />
        <QaInput label="Danger" tone="danger" icon={UserRound} caption="Danger" />
        <QaInput label="Info" tone="info" icon={Search} caption="Info" />
        <QaInput label="People" tone="people" icon={UserRound} caption="People" />
        <QaInput label="Neutral" tone="neutral" icon={Search} caption="Neutral" />
      </Section>

      <Section title="2. Sizes">
        <QaInput
          label="Small"
          tone="primary"
          icon={Search}
          size="sm"
          caption="SM"
          testId={`${testId}-size-sm`}
        />
        <QaInput
          label="Medium"
          tone="primary"
          icon={Search}
          size="md"
          caption="MD"
          testId={`${testId}-size-md`}
        />
        <QaInput
          label="Large"
          tone="primary"
          icon={Search}
          size="lg"
          caption="LG"
          testId={`${testId}-size-lg`}
        />
      </Section>

      <Section title="3. Variants">
        <QaInput label="Default" tone="primary" icon={Search} caption="Default" />
        <QaInput label="Ghost" tone="primary" icon={Search} ghost caption="Ghost" />
        <QaInput label="No icon" tone="secondary" caption="Text only" />
        <QaInput
          label="Rounded chip"
          tone="cyan"
          icon={Mail}
          iconShape="rounded"
          caption="Rounded icon"
        />
        <QaInput
          label="Filled"
          tone="primary"
          icon={Search}
          defaultValue="Sample value"
          caption="With value"
        />
      </Section>
    </div>
  );
}

/**
 * Input glass gallery — same Enterprise Hybrid Glass recipe as Quick Action glass.
 * Personal Glass components sidenav. Intensity Input glass stays under Organization.
 */
export function OrgEhgQaInputGlassGallery() {
  return (
    <div className="org-ehg-qa-glass-gallery" data-testid="ui-standard-org-ehg-qa-input-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Input Glass</strong>
        {" — "}
        Same glass recipe as Quick Action glass (tones, soft icon chip, card shape, light/dark).
        Organization intensity Input glass is under product=organization.
      </p>
      <OrgEhgQaInputGlassPlayground />
      <div className="org-ehg-qa-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-qa-input-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-qa-input-glass-dark" />
      </div>
    </div>
  );
}
