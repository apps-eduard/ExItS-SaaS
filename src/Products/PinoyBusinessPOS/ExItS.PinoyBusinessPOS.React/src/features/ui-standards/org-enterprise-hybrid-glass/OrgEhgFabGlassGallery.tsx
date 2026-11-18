import {
  FilePlus,
  Plus,
  ShoppingCart,
  UserPlus,
  X,
  type LucideIcon,
} from "lucide-react";
import type { ReactNode } from "react";
import { OrgEhgQaBtnGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQaBtnGlassPlayground";
import type {
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import {
  ORG_EHG_SPEED_DIAL_LABEL_TYPES,
  ORG_EHG_SPEED_DIAL_SHOW_DEFAULT,
  ORG_EHG_SPEED_DIAL_SHOW_OPTIONS,
  ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT,
  type OrgEhgSpeedDialLabelType,
  type OrgEhgSpeedDialShow,
  type OrgEhgSpeedDialTransition,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgSpeedDial";

type FabAppearance =
  | "flat"
  | "soft"
  | "outline"
  | "glossy"
  | "glossy-raise"
  | "raised";

const SPEED_DIAL_ACTIONS = [
  { label: "New person", Icon: UserPlus, tone: "info" as const },
  { label: "New item", Icon: FilePlus, tone: "success" as const },
  { label: "Cart", Icon: ShoppingCart, tone: "secondary" as const },
] as const;

function fabClassName({
  tone = "primary",
  appearance = "flat",
  size = "lg",
  extended = false,
}: {
  tone?: OrgEhgQaGlassTone;
  appearance?: FabAppearance;
  size?: OrgEhgQaGlassSize;
  extended?: boolean;
}): string {
  return [
    "org-ehg-qa-glass",
    "org-ehg-qa-btn-glass",
    "org-ehg-qa-btn-glass--fab",
    "org-ehg-qa-glass--no-shadow",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    "org-ehg-qa-btn-glass--density-balance",
    `org-ehg-qa-btn-glass--appearance-${appearance}`,
    extended ? "org-ehg-qa-btn-glass--fab-extended" : "",
  ]
    .filter(Boolean)
    .join(" ");
}

function FabBtn({
  label,
  tone = "primary",
  appearance = "flat",
  size = "lg",
  icon: Icon,
  badge,
  caption,
  testId,
}: {
  label: string;
  tone?: OrgEhgQaGlassTone;
  appearance?: FabAppearance;
  size?: OrgEhgQaGlassSize;
  icon: LucideIcon;
  badge?: string;
  caption?: string;
  testId?: string;
}) {
  return (
    <div className="org-ehg-qa-glass-sample">
      <button
        type="button"
        className={fabClassName({ tone, appearance, size })}
        data-testid={testId}
        data-card-shape="pill"
        data-hover="on"
        data-fab="true"
        aria-label={label}
      >
        <Icon
          className="org-ehg-qa-glass__glyph org-ehg-qa-glass__glyph--bare"
          aria-hidden
          strokeWidth={2.25}
        />
        {badge ? (
          <span className="org-ehg-qa-btn-glass__badge" aria-hidden>
            {badge}
          </span>
        ) : null}
      </button>
      {caption ? <span className="org-ehg-qa-glass-sample__caption">{caption}</span> : null}
    </div>
  );
}

function SpeedDialSample({
  open,
  theme,
  labelType,
  transition = ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT,
  show = ORG_EHG_SPEED_DIAL_SHOW_DEFAULT,
  testId,
  caption,
}: {
  open: boolean;
  theme: "light" | "dark";
  labelType: OrgEhgSpeedDialLabelType;
  transition?: OrgEhgSpeedDialTransition;
  show?: OrgEhgSpeedDialShow;
  testId: string;
  caption: string;
}) {
  const isExtended = labelType === "extended";
  return (
    <div className="org-ehg-qa-glass-sample org-ehg-btn-glass-sample--speed-dial">
      <div
        className="org-ehg org-ehg-btn-glass-speed-dial"
        data-org-ehg-theme={theme}
        data-open={open ? "true" : "false"}
        data-label-type={labelType}
        data-transition={transition}
        data-show={show}
        data-testid={testId}
      >
        <div className="org-ehg-btn-glass-speed-dial__actions" role="menu" aria-hidden={!open}>
          {SPEED_DIAL_ACTIONS.map((action) => {
            const ActionIcon = action.Icon;
            const showSideLabel =
              open && labelType !== "icon-only" && labelType !== "extended";
            return (
              <div key={action.label} className="org-ehg-btn-glass-speed-dial__action-wrap">
                {showSideLabel ? (
                  <span className="org-ehg-btn-glass-speed-dial__label" aria-hidden>
                    {action.label}
                    {labelType === "label-tooltip" ? (
                      <span className="org-ehg-btn-glass-speed-dial__label-caret" />
                    ) : null}
                  </span>
                ) : null}
                <button
                  type="button"
                  role="menuitem"
                  className={fabClassName({
                    tone: action.tone,
                    size: "md",
                    extended: isExtended,
                  })}
                  data-card-shape="pill"
                  data-hover="on"
                  data-fab="true"
                  aria-label={action.label}
                  tabIndex={open ? 0 : -1}
                >
                  <ActionIcon
                    className="org-ehg-qa-glass__glyph org-ehg-qa-glass__glyph--bare"
                    aria-hidden
                    strokeWidth={2.25}
                  />
                  {isExtended ? (
                    <span className="org-ehg-btn-glass-speed-dial__extended-text">{action.label}</span>
                  ) : null}
                </button>
              </div>
            );
          })}
        </div>
        <button
          type="button"
          className={
            fabClassName({ tone: "primary", size: "lg" }) +
            " org-ehg-btn-glass-speed-dial__trigger"
          }
          data-card-shape="pill"
          data-hover="on"
          data-fab="true"
          aria-label="Add"
          aria-expanded={open}
          aria-haspopup="menu"
        >
          <Plus
            className="org-ehg-qa-glass__glyph org-ehg-qa-glass__glyph--bare"
            aria-hidden
            strokeWidth={2.25}
          />
        </button>
      </div>
      <span className="org-ehg-qa-glass-sample__caption">{caption}</span>
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="org-ehg-qa-glass-panel__section">
      <h3 className="org-ehg-qa-glass-panel__section-title">{title}</h3>
      <div className="org-ehg-qa-glass-panel__row org-ehg-btn-glass-panel__row--fab">{children}</div>
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
        <h2 className="org-ehg-qa-glass-panel__title">{title} – Speed dial button glass</h2>
      </header>

      <section className="org-ehg-qa-glass-panel__section">
        <h3 className="org-ehg-qa-glass-panel__section-title">1. Corner placement</h3>
        <div
          className="org-ehg-btn-glass-fab-scene"
          data-testid={`${testId}-fab-scene`}
          data-fab-position="right-bottom"
        >
          <FabBtn
            label="Add"
            size="lg"
            tone="primary"
            icon={Plus}
            caption="Corner FAB"
            testId={`${testId}-fab`}
          />
        </div>
      </section>

      <section className="org-ehg-qa-glass-panel__section">
        <h3 className="org-ehg-qa-glass-panel__section-title">2. Speed dial</h3>
        <div
          className="org-ehg-btn-glass-fab-scene org-ehg-btn-glass-fab-scene--speed-dial"
          data-testid={`${testId}-speed-dial-scene`}
        >
          <SpeedDialSample
            open
            theme={theme}
            labelType="label-tooltip"
            testId={`${testId}-speed-dial`}
            caption="Label tooltip"
          />
        </div>
        <div className="org-ehg-qa-glass-panel__row org-ehg-btn-glass-panel__row--fab">
          <SpeedDialSample
            open={false}
            theme={theme}
            labelType="label-tooltip"
            testId={`${testId}-speed-dial-closed`}
            caption="Closed"
          />
          {ORG_EHG_SPEED_DIAL_SHOW_OPTIONS.filter((item) => item.value !== "top").map((item) => (
            <SpeedDialSample
              key={item.value}
              open
              theme={theme}
              labelType="icon-only"
              show={item.value}
              testId={`${testId}-speed-dial-show-${item.value}`}
              caption={`Show ${item.label}`}
            />
          ))}
          {ORG_EHG_SPEED_DIAL_LABEL_TYPES.filter((item) => item.value !== "label-tooltip").map(
            (item) => (
              <SpeedDialSample
                key={item.value}
                open
                theme={theme}
                labelType={item.value}
                testId={`${testId}-speed-dial-${item.value}`}
                caption={item.label}
              />
            ),
          )}
        </div>
      </section>

      <Section title="3. Size ladder">
        <FabBtn label="Add SM" size="sm" icon={Plus} caption="FAB SM" />
        <FabBtn label="Add MD" size="md" icon={Plus} caption="FAB MD" />
        <FabBtn label="Add LG" size="lg" icon={Plus} caption="FAB LG" />
      </Section>

      <Section title="4. Tones">
        <FabBtn label="Primary" tone="primary" icon={Plus} caption="Primary" />
        <FabBtn
          label="Success"
          tone="success"
          appearance="soft"
          icon={ShoppingCart}
          badge="3"
          caption="Success · badge"
          testId={`${testId}-fab-badge`}
        />
        <FabBtn label="Info" tone="info" icon={Plus} caption="Info" />
        <FabBtn label="Warning" tone="warning" icon={Plus} caption="Warning" />
        <FabBtn label="Danger" tone="danger" icon={X} caption="Danger" />
        <FabBtn label="Neutral" tone="neutral" icon={Plus} caption="Neutral" />
      </Section>

      <Section title="5. Appearance">
        <FabBtn label="Flat" appearance="flat" icon={Plus} caption="Flat" />
        <FabBtn label="Soft" appearance="soft" icon={Plus} caption="Soft" />
        <FabBtn label="Outline" appearance="outline" icon={Plus} caption="Outline" />
        <FabBtn label="Glossy" appearance="glossy" icon={Plus} caption="Glossy" />
        <FabBtn label="Glossy raise" appearance="glossy-raise" icon={Plus} caption="Glossy raise" />
        <FabBtn label="Raised" appearance="raised" icon={Plus} caption="Raised" />
      </Section>
    </div>
  );
}

/**
 * Organization — Enterprise Hybrid Glass Speed dial button glass (UI Standards / DEV only).
 * Uses the same QA btn glass recipe as Button glass (circular FAB).
 */
export function OrgEhgFabGlassGallery() {
  return (
    <div className="org-ehg-qa-glass-gallery" data-testid="ui-standard-org-ehg-fab-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Speed dial button glass</strong>
        {" — "}
        Circular QA Button glass FAB. Same surface recipe as Button glass. Speed dial types: icon
        only, label tooltip, rounded label, flat label, extended.
      </p>
      <OrgEhgQaBtnGlassPlayground defaultFab />
      <div className="org-ehg-qa-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-fab-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-fab-glass-dark" />
      </div>
    </div>
  );
}
