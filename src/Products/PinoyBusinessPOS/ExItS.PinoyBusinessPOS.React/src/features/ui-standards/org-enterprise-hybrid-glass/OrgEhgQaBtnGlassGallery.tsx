import type { ReactNode } from "react";
import {
  Check,
  ChevronDown,
  Plus,
  ShoppingCart,
  Sparkles,
  Trash2,
  type LucideIcon,
} from "lucide-react";
import { OrgEhgQaBtnGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQaBtnGlassPlayground";
import type {
  OrgEhgQaGlassIconShape,
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";

function QaBtn({
  label,
  tone = "primary",
  appearance = "flat",
  icon: Icon,
  iconShape = "soft",
  size = "md",
  ghost = false,
  split = false,
  caption,
  testId,
}: {
  label: string;
  tone?: OrgEhgQaGlassTone;
  appearance?:
    | "flat"
    | "standard"
    | "outline"
    | "ghost"
    | "glossy"
    | "glossy-raise"
    | "soft"
    | "raised";
  icon?: LucideIcon;
  iconShape?: OrgEhgQaGlassIconShape;
  size?: OrgEhgQaGlassSize;
  ghost?: boolean;
  split?: boolean;
  caption?: string;
  testId?: string;
}) {
  const className = [
    "org-ehg-qa-glass",
    "org-ehg-qa-btn-glass",
    "org-ehg-qa-glass--no-shadow",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    `org-ehg-qa-btn-glass--appearance-${appearance}`,
    ghost || appearance === "ghost" ? "org-ehg-qa-glass--ghost" : "",
    split ? "org-ehg-qa-glass--split" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const iconChip = Icon ? (
    <span
      className={
        "org-ehg-qa-glass__icon org-ehg-qa-glass__icon--no-shadow" +
        (iconShape === "rounded" ? " org-ehg-qa-glass__icon--rounded" : "")
      }
      aria-hidden="true"
    >
      <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
    </span>
  ) : null;

  const control = split ? (
    <div className={className} data-testid={testId} role="group" aria-label={label}>
      <button type="button" className="org-ehg-qa-glass__split-main">
        {iconChip}
        <span className="org-ehg-qa-glass__label">{label}</span>
      </button>
      <span className="org-ehg-qa-glass__split-rule" aria-hidden />
      <button type="button" className="org-ehg-qa-glass__split-caret" aria-label={`${label} menu`}>
        <ChevronDown className="org-ehg-qa-glass__trail" aria-hidden strokeWidth={2.25} />
      </button>
    </div>
  ) : (
    <button type="button" className={className} data-testid={testId}>
      {iconChip}
      <span className="org-ehg-qa-glass__label">{label}</span>
    </button>
  );

  return (
    <div className="org-ehg-qa-glass-sample">
      {control}
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
        <h2 className="org-ehg-qa-glass-panel__title">{title} – Button Glass</h2>
      </header>

      <Section title="1. Tones">
        <QaBtn label="Primary" tone="primary" icon={Plus} caption="Primary" />
        <QaBtn label="Success" tone="success" icon={Check} caption="Success" />
        <QaBtn label="Danger" tone="danger" icon={Trash2} caption="Danger" />
        <QaBtn label="Info" tone="info" icon={Sparkles} caption="Info" />
        <QaBtn label="Warning" tone="warning" icon={ShoppingCart} caption="Warning" />
        <QaBtn label="Neutral" tone="neutral" icon={Plus} caption="Neutral" />
      </Section>

      <Section title="2. Sizes">
        <QaBtn
          label="Small"
          tone="primary"
          icon={Plus}
          size="sm"
          caption="SM"
          testId={`${testId}-size-sm`}
        />
        <QaBtn
          label="Medium"
          tone="primary"
          icon={Plus}
          size="md"
          caption="MD"
          testId={`${testId}-size-md`}
        />
        <QaBtn
          label="Large"
          tone="primary"
          icon={Plus}
          size="lg"
          caption="LG"
          testId={`${testId}-size-lg`}
        />
      </Section>

      <Section title="3. Appearances">
        <QaBtn label="Flat" tone="primary" icon={Plus} appearance="flat" caption="Flat" />
        <QaBtn label="Standard" tone="primary" icon={Plus} appearance="standard" caption="Standard" />
        <QaBtn label="Outline" tone="primary" icon={Plus} appearance="outline" caption="Outline" />
        <QaBtn label="Soft" tone="primary" icon={Plus} appearance="soft" caption="Soft" />
        <QaBtn label="Glossy" tone="primary" icon={Plus} appearance="glossy" caption="Glossy" />
        <QaBtn
          label="Glossy raise"
          tone="primary"
          icon={Plus}
          appearance="glossy-raise"
          caption="Glossy raise"
        />
        <QaBtn label="Ghost" tone="primary" icon={Plus} appearance="ghost" caption="Ghost" />
        <QaBtn label="Split" tone="primary" icon={Plus} split caption="Split" />
      </Section>
    </div>
  );
}

/**
 * Button glass gallery — same Enterprise Hybrid Glass recipe as Quick Action glass.
 * Personal Glass components sidenav. Intensity Button glass stays under Organization.
 */
export function OrgEhgQaBtnGlassGallery() {
  return (
    <div className="org-ehg-qa-glass-gallery" data-testid="ui-standard-org-ehg-qa-btn-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Button Glass</strong>
        {" — "}
        Same glass recipe as Quick Action glass (tones, soft icon chip, card shape, light/dark).
        Organization intensity Button glass is under product=organization.
      </p>
      <OrgEhgQaBtnGlassPlayground />
      <div className="org-ehg-qa-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-qa-btn-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-qa-btn-glass-dark" />
      </div>
    </div>
  );
}
