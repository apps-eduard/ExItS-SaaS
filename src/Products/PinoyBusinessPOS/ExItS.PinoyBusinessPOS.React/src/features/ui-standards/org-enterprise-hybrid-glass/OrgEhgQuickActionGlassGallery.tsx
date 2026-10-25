import { useState, type ReactNode } from "react";
import {
  AlertTriangle,
  Bell,
  CalendarDays,
  ChevronDown,
  ChevronRight,
  Copy,
  Download,
  Eye,
  FileText,
  Filter,
  Heart,
  Info,
  MoreHorizontal,
  Moon,
  Package,
  Pencil,
  Plus,
  Receipt,
  Search,
  Settings,
  ShoppingCart,
  Trash2,
  Upload,
  UserRound,
  type LucideIcon,
} from "lucide-react";
import { OrgEhgQuickActionGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassPlayground";

export type OrgEhgQaGlassTone =
  | "amber"
  | "charcoal"
  | "cyan"
  | "danger"
  | "emerald"
  | "fuchsia"
  | "indigo"
  | "info"
  | "inventory"
  | "lime"
  | "neutral"
  | "orange"
  | "people"
  | "primary"
  | "rose"
  | "sales"
  | "secondary"
  | "sky"
  | "success"
  | "teal"
  | "violet"
  | "warning";

/** Soft = square with radius; Rounded = circle. */
export type OrgEhgQaGlassIconShape = "soft" | "rounded";

export type OrgEhgQaGlassSize = "sm" | "md" | "lg";

export type OrgEhgQaGlassTrailing =
  | "chevron"
  | "caret"
  | "toggle"
  | "action-group"
  | "action-icon"
  | "none";

function qaIconShapeClass(shape: OrgEhgQaGlassIconShape): string {
  return shape === "rounded"
    ? " org-ehg-qa-glass-icon--rounded"
    : " org-ehg-qa-glass-icon--soft";
}

function qaActionIconClass(
  tone: OrgEhgQaGlassTone,
  shape: OrgEhgQaGlassIconShape,
  size: OrgEhgQaGlassSize,
): string {
  return `org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action org-ehg-qa-glass-icon--tone-${tone} org-ehg-qa-glass-icon--size-${size}${qaIconShapeClass(shape)}`;
}

function QaTrailingActionIcon({
  label,
  tone = "primary",
  icon: Icon,
  iconShape = "soft",
  size = "md",
  testId,
}: {
  label: string;
  tone?: OrgEhgQaGlassTone;
  icon: LucideIcon;
  iconShape?: OrgEhgQaGlassIconShape;
  size?: OrgEhgQaGlassSize;
  testId?: string;
}) {
  return (
    <button
      type="button"
      className={qaActionIconClass(tone, iconShape, size)}
      aria-label={label}
      data-testid={testId}
      data-icon-shape={iconShape}
      data-size={size}
    >
      <Icon className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
    </button>
  );
}

function QaTrailingActionGroup({
  iconShape = "soft",
  size = "md",
  testId,
}: {
  iconShape?: OrgEhgQaGlassIconShape;
  size?: OrgEhgQaGlassSize;
  testId?: string;
}) {
  const shapeClass = qaIconShapeClass(iconShape);
  const sizeClass = ` org-ehg-qa-glass-icon--size-${size}`;
  return (
    <div
      className={`org-ehg-qa-glass__action-group org-ehg-qa-glass__action-group--size-${size}`}
      role="group"
      aria-label="Actions"
      data-testid={testId}
      data-icon-shape={iconShape}
      data-size={size}
    >
      <button
        type="button"
        className={`org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action org-ehg-qa-glass-icon--tone-primary${sizeClass}${shapeClass}`}
        aria-label="Edit"
      >
        <Pencil className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
      </button>
      <button
        type="button"
        className={`org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action org-ehg-qa-glass-icon--tone-secondary${sizeClass}${shapeClass}`}
        aria-label="Copy"
      >
        <Copy className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
      </button>
      <button
        type="button"
        className={`org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action org-ehg-qa-glass-icon--tone-danger${sizeClass}${shapeClass}`}
        aria-label="Delete"
      >
        <Trash2 className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
      </button>
    </div>
  );
}

function QaGlass({
  label,
  tone = "primary",
  icon: Icon,
  iconShape = "soft",
  size = "md",
  trailing = "chevron",
  badge = false,
  ghost = false,
  split = false,
  caption,
  testId,
  toggled,
  onToggle,
}: {
  label: string;
  tone?: OrgEhgQaGlassTone;
  icon: LucideIcon;
  iconShape?: OrgEhgQaGlassIconShape;
  size?: OrgEhgQaGlassSize;
  trailing?: OrgEhgQaGlassTrailing;
  badge?: boolean;
  ghost?: boolean;
  split?: boolean;
  caption?: string;
  testId?: string;
  toggled?: boolean;
  onToggle?: () => void;
}) {
  const className = [
    "org-ehg-qa-glass",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    ghost ? "org-ehg-qa-glass--ghost" : "",
    split ? "org-ehg-qa-glass--split" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const iconChip = (
    <span
      className={
        "org-ehg-qa-glass__icon" +
        (iconShape === "rounded" ? " org-ehg-qa-glass__icon--rounded" : "")
      }
      aria-hidden="true"
    >
      <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
      {badge ? <span className="org-ehg-qa-glass__dot" /> : null}
    </span>
  );

  const leadAction =
    trailing === "action-group" ? (
      <QaTrailingActionGroup
        iconShape={iconShape}
        size={size}
        testId={testId ? `${testId}-group` : undefined}
      />
    ) : trailing === "action-icon" ? (
      <QaTrailingActionIcon
        label={label}
        tone={tone}
        icon={Icon}
        iconShape={iconShape}
        size={size}
        testId={testId ? `${testId}-action-icon` : undefined}
      />
    ) : null;

  const trail =
    trailing === "toggle" ? (
      <button
        type="button"
        className={
          "org-ehg-qa-glass__switch" + (toggled ? " org-ehg-qa-glass__switch--on" : "")
        }
        aria-label={toggled ? "On" : "Off"}
        aria-pressed={toggled}
        data-testid={testId ? `${testId}-switch` : undefined}
        onClick={(event) => {
          event.stopPropagation();
          onToggle?.();
        }}
      />
    ) : trailing === "caret" ? (
      <ChevronDown className="org-ehg-qa-glass__trail" aria-hidden strokeWidth={2.25} />
    ) : trailing === "chevron" ? (
      <ChevronRight className="org-ehg-qa-glass__trail" aria-hidden strokeWidth={2.25} />
    ) : null;

  /* Action chips / toggle need a group shell so trail controls stay clickable. */
  const needsGroupShell =
    split || trailing === "toggle" || trailing === "action-group" || trailing === "action-icon";
  const control = needsGroupShell ? (
    <div className={className} data-testid={testId} role="group" aria-label={label}>
      {split ? (
        <>
          <button type="button" className="org-ehg-qa-glass__split-main">
            {iconChip}
            <span className="org-ehg-qa-glass__label">{label}</span>
          </button>
          <span className="org-ehg-qa-glass__split-rule" aria-hidden />
          <button
            type="button"
            className="org-ehg-qa-glass__split-caret"
            aria-label={`${label} menu`}
          >
            <ChevronDown className="org-ehg-qa-glass__trail" aria-hidden strokeWidth={2.25} />
          </button>
        </>
      ) : leadAction ? (
        <>
          {leadAction}
          <span className="org-ehg-qa-glass__label">{label}</span>
        </>
      ) : (
        <>
          {iconChip}
          <span className="org-ehg-qa-glass__label">{label}</span>
          {trail}
        </>
      )}
    </div>
  ) : (
    <button type="button" className={className} data-testid={testId}>
      {iconChip}
      <span className="org-ehg-qa-glass__label">{label}</span>
      {trail}
    </button>
  );

  return (
    <div className="org-ehg-qa-glass-sample">
      {control}
      {caption ? <span className="org-ehg-qa-glass-sample__caption">{caption}</span> : null}
    </div>
  );
}

function QaIcon({
  label,
  tone = "neutral",
  icon: Icon,
  filled = false,
  testId,
}: {
  label: string;
  tone?: OrgEhgQaGlassTone;
  icon: LucideIcon;
  filled?: boolean;
  testId?: string;
}) {
  return (
    <button
      type="button"
      className={[
        "org-ehg-qa-glass-icon",
        `org-ehg-qa-glass-icon--tone-${tone}`,
        filled ? "org-ehg-qa-glass-icon--filled" : "",
      ]
        .filter(Boolean)
        .join(" ")}
      aria-label={label}
      data-testid={testId}
    >
      <Icon className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
    </button>
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
  const [darkModeOn, setDarkModeOn] = useState(theme === "dark");

  return (
    <div
      className="org-ehg org-ehg-qa-glass-panel"
      data-org-ehg-theme={theme}
      data-testid={testId}
    >
      <header className="org-ehg-qa-glass-panel__header">
        <h2 className="org-ehg-qa-glass-panel__title">{title} – Quick Action Glass</h2>
      </header>

      <Section title="1. Intent actions">
        <QaGlass label="Highlight" tone="amber" icon={Bell} caption="Amber" />
        <QaGlass label="Archive" tone="charcoal" icon={FileText} caption="Charcoal" />
        <QaGlass label="Start" tone="cyan" icon={Plus} caption="Cyan" />
        <QaGlass label="Delete" tone="danger" icon={Trash2} caption="Danger" />
        <QaGlass label="Confirm" tone="emerald" icon={ShoppingCart} caption="Emerald" />
        <QaGlass label="Campaign" tone="fuchsia" icon={Bell} caption="Fuchsia" />
        <QaGlass label="Workspace" tone="indigo" icon={FileText} caption="Indigo" />
        <QaGlass label="Details" tone="info" icon={Info} caption="Info" />
        <QaGlass label="New Product" tone="inventory" icon={Package} caption="Inventory" />
        <QaGlass label="Fresh" tone="lime" icon={Plus} caption="Lime" />
        <QaGlass label="Browse" tone="neutral" icon={Search} caption="Neutral" />
        <QaGlass label="Promo" tone="orange" icon={Bell} caption="Orange" />
        <QaGlass label="Add Customer" tone="people" icon={UserRound} caption="People" />
        <QaGlass
          label="Add New"
          tone="primary"
          icon={Plus}
          caption="Primary"
          testId={`${testId}-primary`}
        />
        <QaGlass label="Favorite" tone="rose" icon={Heart} caption="Rose" />
        <QaGlass label="New Sale" tone="sales" icon={Receipt} caption="Sales / POS" />
        <QaGlass label="Create" tone="secondary" icon={FileText} caption="Secondary" />
        <QaGlass label="Notice" tone="sky" icon={Info} caption="Sky" />
        <QaGlass label="New Order" tone="success" icon={ShoppingCart} caption="Success" />
        <QaGlass label="Finance" tone="teal" icon={Receipt} caption="Teal" />
        <QaGlass label="Invite" tone="violet" icon={UserRound} caption="Violet" />
        <QaGlass label="Review" tone="warning" icon={AlertTriangle} caption="Warning" />
      </Section>

      <Section title="1b. Icon shape">
        <QaGlass
          label="Soft square"
          tone="primary"
          icon={Plus}
          iconShape="soft"
          caption="Soft (radius square)"
          testId={`${testId}-icon-soft`}
        />
        <QaGlass
          label="Rounded"
          tone="primary"
          icon={Plus}
          iconShape="rounded"
          caption="Rounded (circle)"
          testId={`${testId}-icon-rounded`}
        />
      </Section>

      <Section title="1c. Size">
        <QaGlass label="Small" tone="primary" icon={Plus} size="sm" caption="SM" testId={`${testId}-size-sm`} />
        <QaGlass label="Medium" tone="primary" icon={Plus} size="md" caption="MD" testId={`${testId}-size-md`} />
        <QaGlass label="Large" tone="primary" icon={Plus} size="lg" caption="LG" testId={`${testId}-size-lg`} />
        <QaGlass
          label="Edit"
          tone="primary"
          icon={Pencil}
          size="sm"
          iconShape="rounded"
          trailing="action-icon"
          caption="Action icon SM"
        />
        <QaGlass
          label="Edit"
          tone="primary"
          icon={Pencil}
          size="lg"
          iconShape="rounded"
          trailing="action-icon"
          caption="Action icon LG"
        />
      </Section>

      <Section title="2. Dropdowns">
        <QaGlass label="Import" tone="primary" icon={Download} trailing="caret" caption="Import" />
        <QaGlass label="Export" tone="neutral" icon={Upload} trailing="caret" caption="Export" />
        <QaGlass
          label="More"
          tone="neutral"
          icon={MoreHorizontal}
          trailing="caret"
          caption="More options"
        />
      </Section>

      <Section title="3. Icon only">
        <div className="org-ehg-qa-glass-panel__icons" data-testid={`${testId}-icons`}>
          <QaIcon label="Add" tone="primary" icon={Plus} filled testId={`${testId}-icon-add`} />
          <QaIcon label="Search" tone="neutral" icon={Search} />
          <QaIcon label="Filter" tone="neutral" icon={Filter} />
          <QaIcon label="Settings" tone="neutral" icon={Settings} />
        </div>
      </Section>

      <Section title="4. Functional variants">
        <QaGlass label="Set Date" tone="primary" icon={CalendarDays} caption="With caret" />
        <QaGlass
          label="Remind Me"
          tone="people"
          icon={Bell}
          badge
          caption="With badge"
          testId={`${testId}-badge`}
        />
        <QaGlass
          label="Dark Mode"
          tone="neutral"
          icon={Moon}
          trailing="toggle"
          toggled={darkModeOn}
          onToggle={() => setDarkModeOn((v) => !v)}
          caption="With toggle"
          testId={`${testId}-toggle`}
        />
        <QaGlass
          label="Actions"
          tone="primary"
          icon={FileText}
          split
          caption="Split button"
          testId={`${testId}-split`}
        />
        <QaGlass
          label="Actions"
          tone="primary"
          icon={MoreHorizontal}
          iconShape="soft"
          trailing="action-group"
          caption="Action group · soft square"
          testId={`${testId}-group`}
        />
        <QaGlass
          label="Actions"
          tone="primary"
          icon={MoreHorizontal}
          iconShape="rounded"
          trailing="action-group"
          caption="Action group · rounded"
          testId={`${testId}-group-rounded`}
        />
        <QaGlass
          label="Edit"
          tone="primary"
          icon={Pencil}
          iconShape="rounded"
          trailing="action-icon"
          caption="Action icon · primary"
          testId={`${testId}-action-icon`}
        />
        <QaGlass
          label="Delete"
          tone="danger"
          icon={Trash2}
          iconShape="rounded"
          trailing="action-icon"
          caption="Action icon · danger"
          testId={`${testId}-action-icon-danger`}
        />
        <QaGlass
          label="View Details"
          tone="primary"
          icon={Eye}
          trailing="none"
          ghost
          caption="Ghost"
          testId={`${testId}-ghost`}
        />
        <QaGlass label="Delete" tone="danger" icon={Trash2} caption="Delete" />
        <QaGlass label="Settings" tone="primary" icon={Settings} caption="Settings" />
      </Section>
    </div>
  );
}

/**
 * Organization — Enterprise Hybrid Glass Quick Action samples (UI Standards / DEV only).
 * Light | dark columns matching glass Quick Action reference. Not Personal Soft Glass QA.
 */
export function OrgEhgQuickActionGlassGallery() {
  return (
    <div className="org-ehg-qa-glass-gallery" data-testid="ui-standard-org-ehg-quick-action-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Quick Action Glass</strong>
        {" — "}
        Glass quick-action rows (intent chips, dropdowns, icon-only, badge, toggle, split, ghost).
        Personal Soft Glass Quick Actions (`.personal-qa-glass`) are unchanged.
      </p>
      <OrgEhgQuickActionGlassPlayground />
      <div className="org-ehg-qa-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-qa-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-qa-glass-dark" />
      </div>
    </div>
  );
}
