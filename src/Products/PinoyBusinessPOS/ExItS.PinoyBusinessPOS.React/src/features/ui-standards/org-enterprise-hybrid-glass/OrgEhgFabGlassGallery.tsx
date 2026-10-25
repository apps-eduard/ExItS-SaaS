import {
  FilePlus,
  Plus,
  ShoppingCart,
  UserPlus,
  X,
  type LucideIcon,
} from "lucide-react";
import type { ReactNode } from "react";
import { OrgEhgButtonGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgButtonGlassPlayground";
import {
  ORG_EHG_SPEED_DIAL_LABEL_TYPES,
  ORG_EHG_SPEED_DIAL_SHOW_DEFAULT,
  ORG_EHG_SPEED_DIAL_SHOW_OPTIONS,
  ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT,
  type OrgEhgSpeedDialLabelType,
  type OrgEhgSpeedDialShow,
  type OrgEhgSpeedDialTransition,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgSpeedDial";

type GlassIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
type GlassTone =
  | "primary"
  | "secondary"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "neutral";
type GlassSize = "sm" | "md" | "lg" | "xl";

const SPEED_DIAL_ACTIONS = [
  { label: "New person", Icon: UserPlus, tone: "info" as const },
  { label: "New item", Icon: FilePlus, tone: "success" as const },
  { label: "Cart", Icon: ShoppingCart, tone: "secondary" as const },
] as const;

function FabBtn({
  label,
  intensity = "medium",
  tone = "primary",
  size = "lg",
  icon: Icon,
  badge,
  caption,
  testId,
}: {
  label: string;
  intensity?: GlassIntensity;
  tone?: GlassTone;
  size?: GlassSize;
  icon: LucideIcon;
  badge?: string;
  caption?: string;
  testId?: string;
}) {
  const className = [
    "org-ehg-btn-glass",
    `org-ehg-btn-glass--${intensity}`,
    `org-ehg-btn-glass--tone-${tone}`,
    `org-ehg-btn-glass--size-${size}`,
    "org-ehg-btn-glass--icon-only",
    "org-ehg-btn-glass--fab",
  ].join(" ");

  return (
    <div className="org-ehg-btn-glass-sample">
      <button type="button" className={className} data-testid={testId} aria-label={label}>
        <Icon className="org-ehg-btn-glass__icon" aria-hidden strokeWidth={2.25} />
        {badge ? (
          <span className="org-ehg-btn-glass__badge" aria-hidden>
            {badge}
          </span>
        ) : null}
      </button>
      {caption ? <span className="org-ehg-btn-glass-sample__caption">{caption}</span> : null}
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
    <div className="org-ehg-btn-glass-sample org-ehg-btn-glass-sample--speed-dial">
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
                  className={[
                    "org-ehg-btn-glass",
                    "org-ehg-btn-glass--medium",
                    `org-ehg-btn-glass--tone-${action.tone}`,
                    "org-ehg-btn-glass--size-md",
                    isExtended ? "org-ehg-btn-glass--fab-extended" : "org-ehg-btn-glass--icon-only",
                    "org-ehg-btn-glass--fab",
                  ].join(" ")}
                  aria-label={action.label}
                  tabIndex={open ? 0 : -1}
                >
                  <ActionIcon className="org-ehg-btn-glass__icon" aria-hidden strokeWidth={2.25} />
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
          className={[
            "org-ehg-btn-glass",
            "org-ehg-btn-glass--medium",
            "org-ehg-btn-glass--tone-primary",
            "org-ehg-btn-glass--size-lg",
            "org-ehg-btn-glass--icon-only",
            "org-ehg-btn-glass--fab",
            "org-ehg-btn-glass-speed-dial__trigger",
          ].join(" ")}
          aria-label="Add"
          aria-expanded={open}
          aria-haspopup="menu"
        >
          <Plus className="org-ehg-btn-glass__icon" aria-hidden strokeWidth={2.25} />
        </button>
      </div>
      <span className="org-ehg-btn-glass-sample__caption">{caption}</span>
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="org-ehg-btn-glass-panel__section">
      <h3 className="org-ehg-btn-glass-panel__section-title">{title}</h3>
      <div className="org-ehg-btn-glass-panel__row org-ehg-btn-glass-panel__row--fab">{children}</div>
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
      className="org-ehg org-ehg-btn-glass-panel"
      data-org-ehg-theme={theme}
      data-testid={testId}
    >
      <header className="org-ehg-btn-glass-panel__header">
        <h2 className="org-ehg-btn-glass-panel__title">{title} – Floating Action Glass</h2>
      </header>

      <section className="org-ehg-btn-glass-panel__section">
        <h3 className="org-ehg-btn-glass-panel__section-title">1. Corner placement</h3>
        <div className="org-ehg-btn-glass-fab-scene" data-testid={`${testId}-fab-scene`}>
          <FabBtn
            label="Add"
            size="lg"
            intensity="medium"
            tone="primary"
            icon={Plus}
            caption="Corner FAB"
            testId={`${testId}-fab`}
          />
        </div>
      </section>

      <section className="org-ehg-btn-glass-panel__section">
        <h3 className="org-ehg-btn-glass-panel__section-title">2. Speed dial</h3>
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
        <div className="org-ehg-btn-glass-panel__row org-ehg-btn-glass-panel__row--fab">
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
        <FabBtn label="Add XL" size="xl" icon={Plus} caption="FAB XL" />
      </Section>

      <Section title="4. Tones">
        <FabBtn label="Primary" tone="primary" icon={Plus} caption="Primary" />
        <FabBtn
          label="Success"
          tone="success"
          intensity="soft"
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

      <Section title="5. Intensity">
        <FabBtn label="Solid" intensity="solid" icon={Plus} caption="Solid" />
        <FabBtn label="Soft" intensity="soft" icon={Plus} caption="Soft" />
        <FabBtn label="Medium" intensity="medium" icon={Plus} caption="Medium" />
        <FabBtn label="Strong" intensity="strong" icon={Plus} caption="Strong" />
        <FabBtn label="Ultra" intensity="ultra" icon={Plus} caption="Ultra" />
      </Section>
    </div>
  );
}

/**
 * Organization — Enterprise Hybrid Glass Floating Action (UI Standards / DEV only).
 */
export function OrgEhgFabGlassGallery() {
  return (
    <div className="org-ehg-btn-glass-gallery" data-testid="ui-standard-org-ehg-fab-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Floating Action</strong>
        {" — "}
        Circular Hybrid Glass FAB. Speed dial types: icon only, label tooltip, rounded label, flat
        label, extended.
      </p>
      <OrgEhgButtonGlassPlayground defaultFab />
      <div className="org-ehg-btn-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-fab-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-fab-glass-dark" />
      </div>
    </div>
  );
}
