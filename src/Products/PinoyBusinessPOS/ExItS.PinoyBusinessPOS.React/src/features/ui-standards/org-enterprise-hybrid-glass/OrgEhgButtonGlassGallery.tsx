import {
  AlertTriangle,
  ArrowRight,
  Check,
  ChevronDown,
  Circle,
  Heart,
  Info,
  Loader2,
  Minus,
  Plus,
  Save,
  Settings,
  ShoppingCart,
  Sparkles,
  X,
  type LucideIcon,
} from "lucide-react";
import type { ReactNode } from "react";
import { OrgEhgButtonGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgButtonGlassPlayground";

type GlassIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
type GlassTone =
  | "primary"
  | "secondary"
  | "outline"
  | "ghost"
  | "text"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "neutral";
type GlassSize = "xs" | "sm" | "md" | "lg" | "xl";
type GlassState = "default" | "hover" | "focused" | "pressed" | "disabled" | "loading";
type GlassBgAnimation = "none" | "shimmer" | "pulse" | "drift";
type GlassFocusAnimation = "none" | "ring-pulse" | "glow" | "breathe";

function GlassBtn({
  label,
  intensity = "medium",
  tone = "primary",
  size = "md",
  state,
  icon: Icon,
  iconEnd: IconEnd,
  iconOnly = false,
  badge,
  split = false,
  caption,
  testId,
  bgAnimation = "none",
  focusAnimation = "none",
}: {
  label: string;
  intensity?: GlassIntensity;
  tone?: GlassTone;
  size?: GlassSize;
  state?: GlassState;
  icon?: LucideIcon;
  iconEnd?: LucideIcon;
  iconOnly?: boolean;
  badge?: string;
  split?: boolean;
  caption?: string;
  testId?: string;
  bgAnimation?: GlassBgAnimation;
  focusAnimation?: GlassFocusAnimation;
}) {
  const className = [
    "org-ehg-btn-glass",
    `org-ehg-btn-glass--${intensity}`,
    `org-ehg-btn-glass--tone-${tone}`,
    `org-ehg-btn-glass--size-${size}`,
    state ? `org-ehg-btn-glass--state-${state}` : "",
    iconOnly ? "org-ehg-btn-glass--icon-only" : "",
    split ? "org-ehg-btn-glass--split" : "",
    bgAnimation !== "none" ? `org-ehg-btn-glass--bg-${bgAnimation}` : "",
    focusAnimation !== "none" ? `org-ehg-btn-glass--focus-${focusAnimation}` : "",
  ]
    .filter(Boolean)
    .join(" ");

  const control = split ? (
    <div className={className} data-testid={testId} role="group" aria-label={label}>
      <button type="button" className="org-ehg-btn-glass__split-main" disabled={state === "disabled"}>
        <Save className="org-ehg-btn-glass__icon" aria-hidden strokeWidth={2.25} />
        <span>{label}</span>
      </button>
      <button
        type="button"
        className="org-ehg-btn-glass__split-caret"
        aria-label={`${label} menu`}
        disabled={state === "disabled"}
      >
        <ChevronDown className="org-ehg-btn-glass__icon" aria-hidden strokeWidth={2.25} />
      </button>
    </div>
  ) : (
    <button
      type="button"
      className={className}
      data-testid={testId}
      disabled={state === "disabled" || state === "loading"}
      aria-label={iconOnly ? label : undefined}
      aria-busy={state === "loading" || undefined}
    >
      {state === "loading" ? null : Icon ? (
        <Icon className="org-ehg-btn-glass__icon" aria-hidden strokeWidth={2.25} />
      ) : null}
      {iconOnly ? null : <span>{label}</span>}
      {state === "loading" ? (
        <Loader2 className="org-ehg-btn-glass__icon org-ehg-btn-glass__spin" aria-hidden strokeWidth={2.25} />
      ) : IconEnd ? (
        <IconEnd className="org-ehg-btn-glass__icon" aria-hidden strokeWidth={2.25} />
      ) : null}
      {badge ? (
        <span className="org-ehg-btn-glass__badge" aria-hidden>
          {badge}
        </span>
      ) : null}
    </button>
  );

  return (
    <div className="org-ehg-btn-glass-sample">
      {control}
      {caption ? <span className="org-ehg-btn-glass-sample__caption">{caption}</span> : null}
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="org-ehg-btn-glass-panel__section">
      <h3 className="org-ehg-btn-glass-panel__section-title">{title}</h3>
      <div className="org-ehg-btn-glass-panel__row">{children}</div>
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
        <h2 className="org-ehg-btn-glass-panel__title">
          {title} – Button Glass Styles
        </h2>
      </header>

      <Section title="1. Glass Variations (Primary)">
        <GlassBtn
          label="Solid"
          intensity="solid"
          icon={Check}
          caption="0% transparency"
          testId={`${testId}-solid`}
        />
        <GlassBtn
          label="Soft Glass"
          intensity="soft"
          icon={Check}
          caption="10–20% transparency"
          testId={`${testId}-soft`}
        />
        <GlassBtn
          label="Medium Glass"
          intensity="medium"
          icon={Check}
          caption="30–40% transparency"
          testId={`${testId}-medium`}
        />
        <GlassBtn
          label="Strong Glass"
          intensity="strong"
          icon={Check}
          caption="50–70% transparency"
          testId={`${testId}-strong`}
        />
        <GlassBtn
          label="Ultra Glass"
          intensity="ultra"
          icon={Check}
          caption="80–90% transparency"
          testId={`${testId}-ultra`}
        />
      </Section>

      <Section title="2. Button Types (Glass Style)">
        <GlassBtn
          label="Primary"
          tone="primary"
          intensity="solid"
          icon={Check}
          caption="Main action"
        />
        <GlassBtn
          label="Secondary"
          tone="secondary"
          intensity="soft"
          icon={Circle}
          caption="Supporting"
        />
        <GlassBtn
          label="Outline"
          tone="outline"
          intensity="ultra"
          icon={Sparkles}
          caption="Bordered"
        />
        <GlassBtn label="Ghost" tone="ghost" intensity="ultra" icon={Minus} caption="Quiet" />
        <GlassBtn label="Text" tone="text" intensity="ultra" icon={Check} caption="Link-like" />
      </Section>

      <Section title="3. Status Variations (Glass Style)">
        <GlassBtn label="Success" tone="success" intensity="soft" icon={Check} caption="Confirm" />
        <GlassBtn
          label="Warning"
          tone="warning"
          intensity="soft"
          icon={AlertTriangle}
          caption="Caution"
        />
        <GlassBtn label="Danger" tone="danger" intensity="soft" icon={X} caption="Destructive" />
        <GlassBtn label="Info" tone="info" intensity="soft" icon={Info} caption="Notice" />
        <GlassBtn
          label="Neutral"
          tone="neutral"
          intensity="soft"
          icon={Circle}
          caption="Default"
        />
      </Section>

      <Section title="4. Interaction States (Primary Glass)">
        <GlassBtn
          label="Default"
          state="default"
          intensity="medium"
          icon={Check}
          caption="Resting"
        />
        <GlassBtn label="Hover" state="hover" intensity="medium" icon={Check} caption="Pointer" />
        <GlassBtn
          label="Focused"
          state="focused"
          intensity="medium"
          icon={Check}
          caption="Keyboard"
        />
        <GlassBtn
          label="Pressed"
          state="pressed"
          intensity="medium"
          icon={Check}
          caption="Active"
        />
        <GlassBtn
          label="Disabled"
          state="disabled"
          intensity="medium"
          icon={Check}
          caption="Unavailable"
        />
        <GlassBtn label="Loading" state="loading" intensity="medium" caption="In progress" />
      </Section>

      <Section title="5. Size Variations">
        <GlassBtn label="XS" size="xs" intensity="medium" icon={Check} caption="Extra small" />
        <GlassBtn label="SM" size="sm" intensity="medium" icon={Check} caption="Small" />
        <GlassBtn label="MD" size="md" intensity="medium" icon={Check} caption="Medium" />
        <GlassBtn label="LG" size="lg" intensity="medium" icon={Check} caption="Large" />
        <GlassBtn label="XL" size="xl" intensity="medium" icon={Check} caption="Extra large" />
      </Section>

      <Section title="6. Icon & Content Variations">
        <GlassBtn label="Icon Left" icon={Plus} intensity="medium" caption="Leading mark" />
        <GlassBtn
          label="Next"
          iconEnd={ArrowRight}
          intensity="medium"
          caption="Trailing mark"
        />
        <GlassBtn
          label="Icon Only"
          icon={Heart}
          iconOnly
          intensity="medium"
          caption="Favorite"
          testId={`${testId}-icon-only`}
        />
        <GlassBtn
          label="With Badge"
          icon={ShoppingCart}
          badge="3"
          intensity="medium"
          caption="Notification"
          testId={`${testId}-badge`}
        />
        <GlassBtn
          label="Settings"
          icon={Settings}
          iconEnd={ChevronDown}
          intensity="medium"
          caption="With dropdown"
        />
        <GlassBtn
          label="Save"
          split
          intensity="medium"
          caption="Split button"
          testId={`${testId}-split`}
        />
      </Section>

      <Section title="7. Animation">
        <GlassBtn
          label="Shimmer"
          icon={Sparkles}
          intensity="medium"
          bgAnimation="shimmer"
          caption="Bg shimmer"
          testId={`${testId}-anim-bg-shimmer`}
        />
        <GlassBtn label="Pulse" icon={Check} intensity="medium" bgAnimation="pulse" caption="Bg pulse" />
        <GlassBtn label="Drift" icon={Check} intensity="medium" bgAnimation="drift" caption="Bg drift" />
        <GlassBtn
          label="Ring"
          icon={Check}
          intensity="medium"
          state="focused"
          focusAnimation="ring-pulse"
          caption="Focus ring pulse"
          testId={`${testId}-anim-focus-ring`}
        />
        <GlassBtn
          label="Glow"
          icon={Check}
          intensity="medium"
          state="focused"
          focusAnimation="glow"
          caption="Focus glow"
        />
        <GlassBtn
          label="Breathe"
          icon={Check}
          intensity="medium"
          state="focused"
          focusAnimation="breathe"
          caption="Focus breathe"
        />
      </Section>

      <section className="org-ehg-btn-glass-panel__section">
        <h3 className="org-ehg-btn-glass-panel__section-title">8. Glass Intensity Reference</h3>
        <div className="org-ehg-btn-glass-panel__scene" data-testid={`${testId}-scene`}>
          <GlassBtn label="Solid" intensity="solid" icon={Check} caption="0%" />
          <GlassBtn label="Soft" intensity="soft" icon={Check} caption="10–20%" />
          <GlassBtn label="Medium" intensity="medium" icon={Check} caption="30–40%" />
          <GlassBtn label="Strong" intensity="strong" icon={Check} caption="50–70%" />
          <GlassBtn label="Ultra" intensity="ultra" icon={Check} caption="80–90%" />
        </div>
      </section>
    </div>
  );
}

/**
 * Organization — Enterprise Hybrid Glass Button Glass Styles (UI Standards / DEV only).
 * Spec sample: light | dark columns. Org-scoped radii and glass — not Personal Soft Glass.
 */
export function OrgEhgButtonGlassGallery() {
  return (
    <div className="org-ehg-btn-glass-gallery" data-testid="ui-standard-org-ehg-button-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Button Glass Styles</strong>
        {" — "}
        Organization UI Standards visual reference. Rounded rectangles (8–12px), Hybrid Glass washes.
        Personal Soft Glass is unchanged.
      </p>
      <OrgEhgButtonGlassPlayground />
      <div className="org-ehg-btn-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-btn-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-btn-glass-dark" />
      </div>
    </div>
  );
}
