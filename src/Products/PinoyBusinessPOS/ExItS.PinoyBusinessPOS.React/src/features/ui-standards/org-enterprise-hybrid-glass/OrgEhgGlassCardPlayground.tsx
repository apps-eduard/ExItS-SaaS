import { useLayoutEffect, useState, type ReactNode } from "react";
import {
  Moon,
  RotateCcw,
  Sun,
  Wallet,
} from "lucide-react";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { Button } from "@/components/ui/button";
import { CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import {
  CompactSelect,
  PlaygroundTextField,
} from "@/features/ui-standards/UiStandardsPlaygrounds";
import { PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import {
  OrgEhgCardGlassChecklist,
  OrgEhgCardGlassComparison,
  OrgEhgCardGlassConfirm,
  OrgEhgCardGlassEditForm,
  OrgEhgCardGlassHybridKpi,
  OrgEhgCardGlassInvite,
  OrgEhgCardGlassInvoice,
  OrgEhgCardGlassLoading,
  OrgEhgCardGlassMedia,
  OrgEhgCardGlassNotification,
  OrgEhgCardGlassPricing,
  OrgEhgCardGlassProfile,
  OrgEhgCardGlassSchedule,
  OrgEhgCardGlassSettings,
  OrgEhgCardGlassSoftKpi,
  OrgEhgCardGlassSolid,
  OrgEhgCardGlassTable,
  OrgEhgCardGlassTimeline,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgCardGlassKinds";
import {
  ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY,
  ORG_EHG_PLAYGROUND_BG_OPTIONS,
  type OrgEhgPlaygroundBgOption,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";

/** Glass card playground seed — Soft Vignette. */
const GLASS_CARD_BG_DEFAULT: OrgEhgPlaygroundBgOption = "soft-vignette";

type GlassCardSample =
  | "empty"
  | "kpi"
  | "hybrid-kpi"
  | "solid"
  | "timeline"
  | "table"
  | "settings"
  | "edit"
  | "confirm"
  | "profile"
  | "checklist"
  | "notification"
  | "pricing"
  | "invoice"
  | "invite"
  | "schedule"
  | "media"
  | "comparison"
  | "loading";
/** Card surface fills — Soft / Hybrid match Soft KPI & Hybrid KPI; Solid / Outline / Ghost are alternate shells. */
type GlassCardFill = "soft" | "hybrid" | "solid" | "outline" | "ghost";
type GlassCardShape = "standard" | "soft" | "pill";
type GlassCardHeaderKind = "title" | "subtitle";
type GlassCardFooterKind = "actions" | "caption";
type GlassCardFooterAlign = "start" | "center" | "end";
type SettingsTab = "card" | "header" | "footer";

const SAMPLE_OPTIONS: ReadonlyArray<{ label: string; value: GlassCardSample }> = [
  { label: "Empty", value: "empty" },
  { label: "Soft KPI", value: "kpi" },
  { label: "Hybrid KPI", value: "hybrid-kpi" },
  { label: "Solid data", value: "solid" },
  { label: "Activity timeline", value: "timeline" },
  { label: "Orders table", value: "table" },
  { label: "Notifications", value: "notification" },
  { label: "Setting form", value: "settings" },
  { label: "Edit form", value: "edit" },
  { label: "Confirm action", value: "confirm" },
  { label: "Profile", value: "profile" },
  { label: "Checklist", value: "checklist" },
  { label: "Pricing plan", value: "pricing" },
  { label: "Invoice", value: "invoice" },
  { label: "Staff invite", value: "invite" },
  { label: "Schedule event", value: "schedule" },
  { label: "Media preview", value: "media" },
  { label: "Branch comparison", value: "comparison" },
  { label: "Loading skeleton", value: "loading" },
];

const FILL_OPTIONS: ReadonlyArray<{ label: string; value: GlassCardFill }> = [
  { label: "Soft Glass", value: "soft" },
  { label: "Hybrid Glass", value: "hybrid" },
  { label: "Solid", value: "solid" },
  { label: "Outline", value: "outline" },
  { label: "Ghost", value: "ghost" },
];

function glassCardFillClass(fill: GlassCardFill): string {
  if (fill === "soft") return "";
  return `org-ehg-card-glass--${fill}`;
}

const SHAPE_OPTIONS: ReadonlyArray<{ label: string; value: GlassCardShape }> = [
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Pill", value: "pill" },
];

const HEADER_OPTIONS: ReadonlyArray<{ label: string; value: GlassCardHeaderKind }> = [
  { label: "Title", value: "title" },
  { label: "Title and subtitle", value: "subtitle" },
];

const FOOTER_OPTIONS: ReadonlyArray<{ label: string; value: GlassCardFooterKind }> = [
  { label: "Actions", value: "actions" },
  { label: "Caption", value: "caption" },
];

const ALIGN_OPTIONS: ReadonlyArray<{ label: string; value: GlassCardFooterAlign }> = [
  { label: "Start", value: "start" },
  { label: "Center", value: "center" },
  { label: "End", value: "end" },
];

const DEFAULTS = {
  sample: "kpi" as GlassCardSample,
  fill: "soft" as GlassCardFill,
  shape: "standard" as GlassCardShape,
  shadow: true,
  showHeader: true,
  header: "subtitle" as GlassCardHeaderKind,
  title: "No activity yet",
  subtitle: "This card is waiting for data",
  showIcon: true,
  showBadge: true,
  badge: "+12.4%",
  metric: "₱186,400",
  hint: "Target ₱220,000 · 68% complete",
  showFooter: true,
  footer: "actions" as GlassCardFooterKind,
  footerAlign: "end" as GlassCardFooterAlign,
  caption: "Updated today",
  primaryAction: "Add first item",
  secondaryAction: "Dismiss",
};

function glassCardCommand(
  sample: GlassCardSample,
  fill: GlassCardFill,
  shape: GlassCardShape,
  shadow: boolean,
  showHeader: boolean,
  header: GlassCardHeaderKind,
  showIcon: boolean,
  showBadge: boolean,
  showFooter: boolean,
  footer: GlassCardFooterKind,
  footerAlign: GlassCardFooterAlign,
): string {
  const parts = ["ext", "org-ehg", "glass-card", `sample-${sample}`, fill];
  if (shape !== "standard") parts.push(`shape-${shape}`);
  if (!shadow) parts.push("no-shadow");
  if (sample !== "empty") return parts.join(" ");
  if (!showHeader) parts.push("no-header");
  else if (header === "subtitle") parts.push("header-subtitle");
  else parts.push("header-title");
  if (showHeader && !showIcon) parts.push("no-icon");
  if (showHeader && !showBadge) parts.push("no-badge");
  if (!showFooter) parts.push("no-footer");
  else if (footer === "caption") parts.push("footer-caption");
  else {
    parts.push("footer-actions");
    if (footerAlign !== "end") parts.push(`footer-${footerAlign}`);
  }
  return parts.join(" ");
}

function PartSwitch({
  label,
  checked,
  onCheckedChange,
  testId,
  disabled,
}: {
  label: string;
  checked: boolean;
  onCheckedChange: (next: boolean) => void;
  testId: string;
  disabled?: boolean;
}) {
  return (
    <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
      <Switch
        checked={checked}
        onCheckedChange={onCheckedChange}
        aria-label={label}
        disabled={disabled}
        data-testid={testId}
      />
      {label}
    </label>
  );
}

type ConfigurablePreviewProps = {
  fill: GlassCardFill;
  shape: GlassCardShape;
  shadow: boolean;
  showHeader: boolean;
  header: GlassCardHeaderKind;
  title: string;
  subtitle: string;
  showIcon: boolean;
  showBadge: boolean;
  badge: string;
  metric: string;
  hint: string;
  showFooter: boolean;
  footer: GlassCardFooterKind;
  footerAlign: GlassCardFooterAlign;
  caption: string;
  primaryAction: string;
  secondaryAction: string;
  testIdSuffix: string;
};

function EmptyPreviewCard(props: ConfigurablePreviewProps) {
  const {
    fill,
    shape,
    shadow,
    showHeader,
    header,
    title,
    subtitle,
    showIcon,
    showFooter,
    footer,
    footerAlign,
    caption,
    primaryAction,
    secondaryAction,
    testIdSuffix,
  } = props;

  const cardClass = [
    "org-ehg-card-glass",
    "org-ehg-card-glass--empty",
    "org-ehg-glass-card-pg__card",
    glassCardFillClass(fill),
    `org-ehg-glass-card-pg__card--shape-${shape}`,
    shadow ? "" : "org-ehg-glass-card-pg__card--no-shadow",
  ]
    .filter(Boolean)
    .join(" ");

  let footerPart: ReactNode = null;
  if (showFooter) {
    footerPart =
      footer === "caption" ? (
        <footer
          className="org-ehg-glass-card-pg__footer-caption"
          data-testid={`ui-standard-org-ehg-glass-card-pg-preview-footer${testIdSuffix}`}
          data-footer="caption"
        >
          {caption.trim() || "Caption"}
        </footer>
      ) : (
        <footer
          className="org-ehg-card-glass__actions"
          data-testid={`ui-standard-org-ehg-glass-card-pg-preview-footer${testIdSuffix}`}
          data-footer="actions"
          data-align={footerAlign}
          style={{ justifyContent: footerAlign }}
        >
          <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
            {secondaryAction.trim() || "Cancel"}
          </button>
          <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
            {primaryAction.trim() || "Add first item"}
          </button>
        </footer>
      );
  }

  return (
    <article
      className={cardClass}
      data-testid={`ui-standard-org-ehg-glass-card-pg-preview-card${testIdSuffix}`}
      data-fill={fill}
      data-shape={shape}
      data-shadow={shadow ? "on" : "off"}
      data-header={showHeader ? header : "none"}
      data-footer={showFooter ? footer : "none"}
      data-show-header={showHeader ? "true" : "false"}
      data-show-footer={showFooter ? "true" : "false"}
      data-sample="empty"
    >
      {showHeader ? (
        <header
          className="org-ehg-card-glass__header"
          data-testid={`ui-standard-org-ehg-glass-card-pg-preview-header${testIdSuffix}`}
        >
          <div className="org-ehg-card-glass__lead">
            {showIcon ? (
              <span className="org-ehg-card-glass__icon-chip" aria-hidden>
                <Wallet className="size-4" strokeWidth={2.25} />
              </span>
            ) : null}
            <div>
              <h3 className="org-ehg-card-glass__title">{title.trim() || "No activity yet"}</h3>
              {header === "subtitle" ? (
                <p className="org-ehg-card-glass__subtitle">
                  {subtitle.trim() || "This card is waiting for data"}
                </p>
              ) : null}
            </div>
          </div>
        </header>
      ) : null}
      <div className="org-ehg-card-glass__empty-body">
        <p className="org-ehg-card-glass__empty-title">Nothing to show</p>
        <p className="org-ehg-card-glass__empty-hint">
          Add a record or connect a source to fill this glass card.
        </p>
      </div>
      {footerPart}
    </article>
  );
}

function SamplePreview({
  sample,
  fill,
  shellClass,
  configurable,
  testIdSuffix,
}: {
  sample: GlassCardSample;
  fill: GlassCardFill;
  shellClass: string;
  configurable: ConfigurablePreviewProps;
  testIdSuffix: string;
}) {
  if (sample === "empty") {
    return <EmptyPreviewCard {...configurable} testIdSuffix={testIdSuffix} />;
  }
  const shellProps = {
    className: shellClass,
    "data-fill": fill,
    "data-testid": `ui-standard-org-ehg-glass-card-pg-preview-card${testIdSuffix}`,
  } as const;
  const body =
    sample === "kpi" ? (
      <OrgEhgCardGlassSoftKpi variant="sales" />
    ) : sample === "hybrid-kpi" ? (
      <OrgEhgCardGlassHybridKpi variant="conversion" />
    ) : sample === "solid" ? (
      <OrgEhgCardGlassSolid />
    ) : sample === "timeline" ? (
      <OrgEhgCardGlassTimeline />
    ) : sample === "table" ? (
      <OrgEhgCardGlassTable />
    ) : sample === "notification" ? (
      <OrgEhgCardGlassNotification />
    ) : sample === "settings" ? (
      <OrgEhgCardGlassSettings />
    ) : sample === "edit" ? (
      <OrgEhgCardGlassEditForm />
    ) : sample === "confirm" ? (
      <OrgEhgCardGlassConfirm />
    ) : sample === "profile" ? (
      <OrgEhgCardGlassProfile />
    ) : sample === "checklist" ? (
      <OrgEhgCardGlassChecklist />
    ) : sample === "pricing" ? (
      <OrgEhgCardGlassPricing />
    ) : sample === "invoice" ? (
      <OrgEhgCardGlassInvoice />
    ) : sample === "invite" ? (
      <OrgEhgCardGlassInvite />
    ) : sample === "schedule" ? (
      <OrgEhgCardGlassSchedule />
    ) : sample === "media" ? (
      <OrgEhgCardGlassMedia />
    ) : sample === "comparison" ? (
      <OrgEhgCardGlassComparison />
    ) : (
      <OrgEhgCardGlassLoading />
    );

  return (
    <div {...shellProps} data-sample={sample}>
      {body}
    </div>
  );
}

function PreviewLane({
  theme,
  title,
  background,
  sample,
  fill,
  shellClass,
  configurable,
  testId,
}: {
  theme: "light" | "dark";
  title: string;
  background: OrgEhgPlaygroundBgOption;
  sample: GlassCardSample;
  fill: GlassCardFill;
  shellClass: string;
  configurable: ConfigurablePreviewProps;
  testId: string;
}) {
  return (
    <div
      className="org-ehg org-ehg-pg-bg org-ehg-card-glass-panel org-ehg-glass-card-pg__lane"
      data-org-ehg
      data-org-ehg-theme={theme}
      data-bg-option={background}
      data-testid={testId}
      style={{
        ["--org-ehg-bg-intensity" as string]: String(ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY),
      }}
    >
      <div className="org-ehg-bg-lab__base" aria-hidden />
      <div className="org-ehg-bg-lab__decor" aria-hidden />
      <div className="org-ehg-pg-bg__content org-ehg-glass-card-pg__lane-body">
        <p className="org-ehg-glass-card-pg__lane-label">{title}</p>
        <SamplePreview
          sample={sample}
          fill={fill}
          shellClass={shellClass}
          configurable={{ ...configurable, testIdSuffix: theme === "light" ? "" : "-dark" }}
          testIdSuffix={theme === "light" ? "" : "-dark"}
        />
      </div>
    </div>
  );
}

/**
 * Org EHG Glass card playground — configure Card, Header, and Footer.
 * UI Standards / DEV only.
 */
export function OrgEhgGlassCardPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const [settingsTab, setSettingsTab] = useState<SettingsTab>("card");
  const [sample, setSample] = useState(DEFAULTS.sample);
  const [fill, setFill] = useState(DEFAULTS.fill);
  const [shape, setShape] = useState(DEFAULTS.shape);
  const [shadow, setShadow] = useState(DEFAULTS.shadow);
  const [showHeader, setShowHeader] = useState(DEFAULTS.showHeader);
  const [header, setHeader] = useState(DEFAULTS.header);
  const [title, setTitle] = useState(DEFAULTS.title);
  const [subtitle, setSubtitle] = useState(DEFAULTS.subtitle);
  const [showIcon, setShowIcon] = useState(DEFAULTS.showIcon);
  const [showBadge, setShowBadge] = useState(DEFAULTS.showBadge);
  const [badge, setBadge] = useState(DEFAULTS.badge);
  const [metric, setMetric] = useState(DEFAULTS.metric);
  const [hint, setHint] = useState(DEFAULTS.hint);
  const [showFooter, setShowFooter] = useState(DEFAULTS.showFooter);
  const [footer, setFooter] = useState(DEFAULTS.footer);
  const [footerAlign, setFooterAlign] = useState(DEFAULTS.footerAlign);
  const [caption, setCaption] = useState(DEFAULTS.caption);
  const [primaryAction, setPrimaryAction] = useState(DEFAULTS.primaryAction);
  const [secondaryAction, setSecondaryAction] = useState(DEFAULTS.secondaryAction);

  const customizeParts = sample === "empty";

  useLayoutEffect(() => {
    setBackground(GLASS_CARD_BG_DEFAULT);
  }, [setBackground]);

  function resetPreview() {
    setSettingsTab("card");
    setSample(DEFAULTS.sample);
    setFill(DEFAULTS.fill);
    setShape(DEFAULTS.shape);
    setShadow(DEFAULTS.shadow);
    setShowHeader(DEFAULTS.showHeader);
    setHeader(DEFAULTS.header);
    setTitle(DEFAULTS.title);
    setSubtitle(DEFAULTS.subtitle);
    setShowIcon(DEFAULTS.showIcon);
    setShowBadge(DEFAULTS.showBadge);
    setBadge(DEFAULTS.badge);
    setMetric(DEFAULTS.metric);
    setHint(DEFAULTS.hint);
    setShowFooter(DEFAULTS.showFooter);
    setFooter(DEFAULTS.footer);
    setFooterAlign(DEFAULTS.footerAlign);
    setCaption(DEFAULTS.caption);
    setPrimaryAction(DEFAULTS.primaryAction);
    setSecondaryAction(DEFAULTS.secondaryAction);
    setBackground(GLASS_CARD_BG_DEFAULT);
  }

  const shellClass = [
    "org-ehg-glass-card-pg__sample",
    glassCardFillClass(fill),
    `org-ehg-glass-card-pg__card--shape-${shape}`,
    shadow ? "" : "org-ehg-glass-card-pg__card--no-shadow",
  ]
    .filter(Boolean)
    .join(" ");

  const configurable: ConfigurablePreviewProps = {
    fill,
    shape,
    shadow,
    showHeader,
    header,
    title,
    subtitle,
    showIcon,
    showBadge,
    badge,
    metric,
    hint,
    showFooter,
    footer,
    footerAlign,
    caption,
    primaryAction,
    secondaryAction,
    testIdSuffix: "",
  };

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-glass-card-playground"
      data-bg-option={background}
      data-org-ehg-theme={theme}
      data-preview-theme={theme}
      data-sample={sample}
      style={{
        ["--org-ehg-bg-intensity" as string]: String(ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY),
      }}
    >
      <div className="org-ehg-bg-lab__base" aria-hidden />
      <div className="org-ehg-bg-lab__decor" aria-hidden />
      <div className="org-ehg-pg-bg__content org-ehg-glass-playground-card__body">
        <div className="flex items-start justify-between gap-3">
          <CardTitle>Glass card playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-glass-card-pg-theme"
              onClick={toggleTheme}
            >
              {theme === "dark" ? (
                <Moon className="size-4" aria-hidden />
              ) : (
                <Sun className="size-4" aria-hidden />
              )}
            </Button>
            <Button
              type="button"
              intent="neutral"
              appearance="outline"
              shape="pill"
              size="icon"
              aria-label="Reset"
              data-testid="ui-standard-org-ehg-glass-card-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>

        <PlaygroundSection testId="ui-standard-org-ehg-glass-card-playground" className="">
          <div
            className="org-ehg-glass-card-pg__preview org-ehg-glass-card-pg__preview--split"
            data-testid="ui-standard-org-ehg-glass-card-pg-preview"
            data-sample={sample}
          >
            <PreviewLane
              theme="light"
              title="Light mode"
              background={background}
              sample={sample}
              fill={fill}
              shellClass={shellClass}
              configurable={configurable}
              testId="ui-standard-org-ehg-glass-card-pg-lane-light"
            />
            <PreviewLane
              theme="dark"
              title="Dark mode"
              background={background}
              sample={sample}
              fill={fill}
              shellClass={shellClass}
              configurable={configurable}
              testId="ui-standard-org-ehg-glass-card-pg-lane-dark"
            />
          </div>

          <ExitsTabs
            variant="underline"
            layout="equal"
            ariaLabel="Glass card settings"
            value={settingsTab}
            onValueChange={(next) => setSettingsTab(next as SettingsTab)}
            testId="ui-standard-org-ehg-glass-card-pg-tabs"
            items={[
              { key: "card", label: "Card", testId: "ui-standard-org-ehg-glass-card-pg-tab-card" },
              {
                key: "header",
                label: "Header",
                testId: "ui-standard-org-ehg-glass-card-pg-tab-header",
              },
              {
                key: "footer",
                label: "Footer",
                testId: "ui-standard-org-ehg-glass-card-pg-tab-footer",
              },
            ]}
            panels={{
              card: (
                <div
                  className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3"
                  data-testid="ui-standard-org-ehg-glass-card-pg-card-settings"
                >
                  <CompactSelect
                    label="Sample"
                    value={sample}
                    options={[...SAMPLE_OPTIONS]}
                    onChange={setSample}
                    testId="ui-standard-org-ehg-glass-card-pg-sample"
                  />
                  <CompactSelect
                    label="Fill"
                    value={fill}
                    options={[...FILL_OPTIONS]}
                    onChange={setFill}
                    testId="ui-standard-org-ehg-glass-card-pg-fill"
                  />
                  <CompactSelect
                    label="Shape"
                    value={shape}
                    options={[...SHAPE_OPTIONS]}
                    onChange={setShape}
                    testId="ui-standard-org-ehg-glass-card-pg-shape"
                  />
                  <CompactSelect
                    label="Background"
                    value={background}
                    options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
                    onChange={setBackground}
                    testId="ui-standard-org-ehg-glass-card-pg-background"
                  />
                  <PlaygroundTextField
                    label="Metric"
                    value={metric}
                    onChange={setMetric}
                    testId="ui-standard-org-ehg-glass-card-pg-metric"
                  />
                  <PlaygroundTextField
                    label="Hint"
                    value={hint}
                    onChange={setHint}
                    testId="ui-standard-org-ehg-glass-card-pg-hint"
                  />
                  <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                    <Switch
                      checked={shadow}
                      onCheckedChange={setShadow}
                      aria-label="Shadow"
                      data-testid="ui-standard-org-ehg-glass-card-pg-shadow"
                    />
                    Shadow
                  </label>
                  <PartSwitch
                    label="Header"
                    checked={showHeader}
                    onCheckedChange={setShowHeader}
                    disabled={!customizeParts}
                    testId="ui-standard-org-ehg-glass-card-pg-show-header"
                  />
                  <PartSwitch
                    label="Footer"
                    checked={showFooter}
                    onCheckedChange={setShowFooter}
                    disabled={!customizeParts}
                    testId="ui-standard-org-ehg-glass-card-pg-show-footer"
                  />
                </div>
              ),
              header: (
                <div
                  className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3"
                  data-testid="ui-standard-org-ehg-glass-card-pg-header-settings"
                >
                  <PartSwitch
                    label="Header"
                    checked={showHeader}
                    onCheckedChange={setShowHeader}
                    disabled={!customizeParts}
                    testId="ui-standard-org-ehg-glass-card-pg-header-enabled"
                  />
                  <CompactSelect
                    label="Type"
                    value={header}
                    options={[...HEADER_OPTIONS]}
                    onChange={setHeader}
                    disabled={!customizeParts || !showHeader}
                    testId="ui-standard-org-ehg-glass-card-pg-header"
                  />
                  <PlaygroundTextField
                    label="Title"
                    value={title}
                    onChange={setTitle}
                    testId="ui-standard-org-ehg-glass-card-pg-title"
                  />
                  <PlaygroundTextField
                    label="Subtitle"
                    value={subtitle}
                    onChange={setSubtitle}
                    testId="ui-standard-org-ehg-glass-card-pg-subtitle"
                  />
                  <PlaygroundTextField
                    label="Badge"
                    value={badge}
                    onChange={setBadge}
                    testId="ui-standard-org-ehg-glass-card-pg-badge"
                  />
                  <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                    <Switch
                      checked={showIcon}
                      onCheckedChange={setShowIcon}
                      aria-label="Icon"
                      disabled={!customizeParts || !showHeader}
                      data-testid="ui-standard-org-ehg-glass-card-pg-icon"
                    />
                    Icon
                  </label>
                  <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                    <Switch
                      checked={showBadge}
                      onCheckedChange={setShowBadge}
                      aria-label="Badge"
                      disabled={!customizeParts || !showHeader}
                      data-testid="ui-standard-org-ehg-glass-card-pg-show-badge"
                    />
                    Badge
                  </label>
                </div>
              ),
              footer: (
                <div
                  className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3"
                  data-testid="ui-standard-org-ehg-glass-card-pg-footer-settings"
                >
                  <PartSwitch
                    label="Footer"
                    checked={showFooter}
                    onCheckedChange={setShowFooter}
                    disabled={!customizeParts}
                    testId="ui-standard-org-ehg-glass-card-pg-footer-enabled"
                  />
                  <CompactSelect
                    label="Type"
                    value={footer}
                    options={[...FOOTER_OPTIONS]}
                    onChange={setFooter}
                    disabled={!customizeParts || !showFooter}
                    testId="ui-standard-org-ehg-glass-card-pg-footer"
                  />
                  <CompactSelect
                    label="Align"
                    value={footerAlign}
                    options={[...ALIGN_OPTIONS]}
                    onChange={setFooterAlign}
                    disabled={!customizeParts || !showFooter || footer !== "actions"}
                    testId="ui-standard-org-ehg-glass-card-pg-footer-align"
                  />
                  <PlaygroundTextField
                    label="Caption"
                    value={caption}
                    onChange={setCaption}
                    testId="ui-standard-org-ehg-glass-card-pg-caption"
                  />
                  <PlaygroundTextField
                    label="Primary action"
                    value={primaryAction}
                    onChange={setPrimaryAction}
                    testId="ui-standard-org-ehg-glass-card-pg-primary-action"
                  />
                  <PlaygroundTextField
                    label="Secondary action"
                    value={secondaryAction}
                    onChange={setSecondaryAction}
                    testId="ui-standard-org-ehg-glass-card-pg-secondary-action"
                  />
                </div>
              ),
            }}
          />

          <UiStandardsCopyCommand
            standard="Org Glass Card"
            standardStatus="pilot"
            command={glassCardCommand(
              sample,
              fill,
              shape,
              shadow,
              showHeader,
              header,
              showIcon,
              showBadge,
              showFooter,
              footer,
              footerAlign,
            )}
            testId="ui-standard-org-ehg-glass-card-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}
