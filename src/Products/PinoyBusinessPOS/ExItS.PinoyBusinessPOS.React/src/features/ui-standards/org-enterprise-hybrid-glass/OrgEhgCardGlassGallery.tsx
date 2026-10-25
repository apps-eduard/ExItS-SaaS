import type { ReactNode } from "react";
import {
  OrgEhgCardGlassHybridKpi,
  OrgEhgCardGlassSettings,
  OrgEhgCardGlassSoftKpi,
  OrgEhgCardGlassSolid,
  OrgEhgCardGlassTable,
  OrgEhgCardGlassTimeline,
  type OrgEhgCardGlassKind,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgCardGlassKinds";
import { OrgEhgMenuGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgMenuGlassGallery";
import { ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY } from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";

const KIND_META: Record<
  Exclude<OrgEhgCardGlassKind, "menu">,
  { title: string; blurb: string }
> = {
  "soft-kpi": {
    title: "Soft KPI",
    blurb: "Progress-led soft glass metrics for sales, fulfillment, and shelf health.",
  },
  "hybrid-kpi": {
    title: "Hybrid KPI",
    blurb: "Sparkline hybrid glass cards for conversion, AOV, and traffic.",
  },
  solid: {
    title: "Solid data",
    blurb: "Dense inventory breakdown with proportional stock bars.",
  },
  timeline: {
    title: "Activity timeline",
    blurb: "Live ops feed with actor chips, detail lines, and severity dots.",
  },
  table: {
    title: "Orders table",
    blurb: "Recent orders with customer, total, time, and status chips.",
  },
  settings: {
    title: "Settings form",
    blurb: "Branch alert settings with glass fields, switches, and footer actions.",
  },
};

function Sample({ caption, children }: { caption: string; children: ReactNode }) {
  return (
    <div className="org-ehg-card-glass-sample">
      {children}
      <span className="org-ehg-card-glass-sample__caption">{caption}</span>
    </div>
  );
}

function KindSamples({ kind }: { kind: Exclude<OrgEhgCardGlassKind, "menu"> }) {
  if (kind === "soft-kpi") {
    return (
      <>
        <Sample caption="Weekly sales">
          <OrgEhgCardGlassSoftKpi variant="sales" />
        </Sample>
        <Sample caption="Same-day fulfillment">
          <OrgEhgCardGlassSoftKpi variant="fulfillment" />
        </Sample>
        <Sample caption="Shelf availability">
          <OrgEhgCardGlassSoftKpi variant="stock" />
        </Sample>
      </>
    );
  }
  if (kind === "hybrid-kpi") {
    return (
      <>
        <Sample caption="Checkout conversion">
          <OrgEhgCardGlassHybridKpi variant="conversion" />
        </Sample>
        <Sample caption="Average order value">
          <OrgEhgCardGlassHybridKpi variant="aov" />
        </Sample>
        <Sample caption="Store visits">
          <OrgEhgCardGlassHybridKpi variant="visits" />
        </Sample>
      </>
    );
  }
  if (kind === "solid") {
    return (
      <Sample caption="Active SKUs">
        <OrgEhgCardGlassSolid />
      </Sample>
    );
  }
  if (kind === "timeline") {
    return (
      <Sample caption="Operations activity">
        <OrgEhgCardGlassTimeline />
      </Sample>
    );
  }
  if (kind === "table") {
    return (
      <Sample caption="Recent customer orders">
        <OrgEhgCardGlassTable />
      </Sample>
    );
  }
  return (
    <Sample caption="Inventory alert settings">
      <OrgEhgCardGlassSettings />
    </Sample>
  );
}

function ModePanel({
  theme,
  title,
  kind,
  testId,
}: {
  theme: "light" | "dark";
  title: string;
  kind: Exclude<OrgEhgCardGlassKind, "menu">;
  testId: string;
}) {
  const meta = KIND_META[kind];
  return (
    <div
      className="org-ehg org-ehg-pg-bg org-ehg-card-glass-panel"
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
      <div className="org-ehg-pg-bg__content org-ehg-card-glass-panel__body">
        <header className="org-ehg-card-glass-panel__header">
          <h2 className="org-ehg-card-glass-panel__title">
            {title} – {meta.title}
          </h2>
          <p className="org-ehg-card-glass-panel__blurb">{meta.blurb}</p>
        </header>
        <div className="org-ehg-card-glass-panel__row">
          <KindSamples kind={kind} />
        </div>
      </div>
    </div>
  );
}

/**
 * Organization / Soft Glass playground host for Card glass kinds (UI Standards / DEV only).
 */
export function OrgEhgCardGlassGallery({ kind }: { kind: OrgEhgCardGlassKind }) {
  if (kind === "menu") {
    return <OrgEhgMenuGlassGallery />;
  }

  const meta = KIND_META[kind];
  return (
    <div className="org-ehg-card-glass-gallery" data-testid={`ui-standard-org-ehg-card-glass-${kind}`}>
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Card glass · {meta.title}</strong>
        {" — "}
        {meta.blurb} Personal Soft Glass production cards are unchanged.
      </p>
      <div className="org-ehg-card-glass-gallery__columns">
        <ModePanel
          theme="light"
          title="Light Mode"
          kind={kind}
          testId={`org-ehg-card-glass-${kind}-light`}
        />
        <ModePanel
          theme="dark"
          title="Dark Mode"
          kind={kind}
          testId={`org-ehg-card-glass-${kind}-dark`}
        />
      </div>
    </div>
  );
}
