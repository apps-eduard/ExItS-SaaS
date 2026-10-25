import type { ReactNode } from "react";
import {
  OrgEhgMenuGlassCard,
  OrgEhgMenuGlassPlayground,
  type OrgEhgMenuGlassIntensity,
  type OrgEhgMenuGlassPreset,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgMenuGlassPlayground";
import { ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY } from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";

function Sample({ caption, children }: { caption: string; children: ReactNode }) {
  return (
    <div className="org-ehg-menu-glass-sample">
      {children}
      <span className="org-ehg-menu-glass-sample__caption">{caption}</span>
    </div>
  );
}

function Section({ title, detail, children }: { title: string; detail?: string; children: ReactNode }) {
  return (
    <section className="org-ehg-menu-glass-panel__section">
      <div className="org-ehg-menu-glass-panel__section-head">
        <h3 className="org-ehg-menu-glass-panel__section-title">{title}</h3>
        {detail ? <p className="org-ehg-menu-glass-panel__section-detail">{detail}</p> : null}
      </div>
      <div className="org-ehg-menu-glass-panel__row">{children}</div>
    </section>
  );
}

const PRESET_SAMPLES: ReadonlyArray<{
  preset: OrgEhgMenuGlassPreset;
  caption: string;
  selected: string;
  intensity?: OrgEhgMenuGlassIntensity;
}> = [
  {
    preset: "workspace",
    caption: "Workspace nav",
    selected: "inventory",
    intensity: "medium",
  },
  {
    preset: "account",
    caption: "Account menu",
    selected: "alerts",
    intensity: "strong",
  },
  {
    preset: "quick-actions",
    caption: "Quick actions",
    selected: "new-order",
    intensity: "soft",
  },
];

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
      className="org-ehg org-ehg-pg-bg org-ehg-menu-glass-panel"
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
      <div className="org-ehg-pg-bg__content org-ehg-menu-glass-panel__body">
        <header className="org-ehg-menu-glass-panel__header">
          <h2 className="org-ehg-menu-glass-panel__title">{title} – Menu cards</h2>
          <p className="org-ehg-menu-glass-panel__blurb">
            Real Organization menu surfaces: workspace navigation, account sheet, and shift quick
            actions.
          </p>
        </header>

        <Section
          title="1. Useful presets"
          detail="Pick the pattern that matches the product surface."
        >
          {PRESET_SAMPLES.map((sample) => (
            <Sample key={sample.preset} caption={sample.caption}>
              <OrgEhgMenuGlassCard
                preset={sample.preset}
                intensity={sample.intensity}
                density="comfortable"
                eyebrow={
                  sample.preset === "workspace"
                    ? "Workspace"
                    : sample.preset === "account"
                      ? "Signed in"
                      : "Shortcuts"
                }
                title={
                  sample.preset === "workspace"
                    ? "ExItS Retail Coop"
                    : sample.preset === "account"
                      ? "Maria Santos"
                      : "Quick actions"
                }
                subtitle={
                  sample.preset === "workspace"
                    ? "Main branch · Owner"
                    : sample.preset === "account"
                      ? "maria@exits.example · Owner"
                      : "Common ops for this shift"
                }
                footerLabel={
                  sample.preset === "quick-actions" ? undefined : sample.preset === "workspace"
                    ? "Switch branch"
                    : "Manage account"
                }
                selected={sample.selected}
                testId={`${testId}-${sample.preset}`}
              />
            </Sample>
          ))}
        </Section>

        <Section
          title="2. Intensity ladder"
          detail="Same workspace menu, different glass strength."
        >
          {(["solid", "soft", "medium", "strong", "ultra"] as const).map((intensity) => (
            <Sample key={intensity} caption={intensity}>
              <OrgEhgMenuGlassCard
                preset="workspace"
                intensity={intensity}
                density="compact"
                eyebrow="Workspace"
                title="ExItS Retail Coop"
                subtitle="Main branch"
                selected="orders"
                showHints={false}
                testId={`${testId}-${intensity}`}
              />
            </Sample>
          ))}
        </Section>
      </div>
    </div>
  );
}

/**
 * Organization — Enterprise Hybrid Glass Menu Card Styles (UI Standards / DEV only).
 */
export function OrgEhgMenuGlassGallery() {
  return (
    <div className="org-ehg-menu-glass-gallery" data-testid="ui-standard-org-ehg-menu-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Card glass · Menu card</strong>
        {" — "}
        Workspace, account, and quick-action menu cards with header, badges, hints, and footer.
        Personal Soft Glass is unchanged.
      </p>
      <OrgEhgMenuGlassPlayground />
      <div className="org-ehg-menu-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-menu-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-menu-glass-dark" />
      </div>
    </div>
  );
}
