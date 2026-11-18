import type { ReactNode } from "react";
import {
  OrgEhgCardGlassChecklist,
  OrgEhgCardGlassComparison,
  OrgEhgCardGlassConfirm,
  OrgEhgCardGlassEditForm,
  OrgEhgCardGlassEmpty,
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
  empty: {
    title: "Empty",
    blurb: "Empty-state glass card with header, placeholder body, and call to action.",
  },
  edit: {
    title: "Edit form",
    blurb: "Product edit glass card with fields and Cancel / Save footer actions.",
  },
  confirm: {
    title: "Confirm action",
    blurb: "Destructive confirm glass card with warning lead and danger footer action.",
  },
  profile: {
    title: "Profile",
    blurb: "Staff profile glass card with avatar, contact facts, and schedule actions.",
  },
  checklist: {
    title: "Checklist",
    blurb: "Closing checklist glass card with done/pending rows and progress pill.",
  },
  notification: {
    title: "Notifications",
    blurb: "Unread alert list with severity dots and mark-all-read link.",
  },
  pricing: {
    title: "Pricing plan",
    blurb: "Plan card with price metric, feature bullets, and trial CTA.",
  },
  invoice: {
    title: "Invoice",
    blurb: "Invoice summary with totals, unpaid status, and record-payment action.",
  },
  invite: {
    title: "Staff invite",
    blurb: "Invite form with contact fields and org-scoped login hint.",
  },
  schedule: {
    title: "Schedule event",
    blurb: "Calendar event card with time, place, lead, and open-brief actions.",
  },
  media: {
    title: "Media preview",
    blurb: "Product photo frame with replace / use-photo footer actions.",
  },
  comparison: {
    title: "Branch comparison",
    blurb: "Side-by-side branch sales metrics with up/down badges.",
  },
  loading: {
    title: "Loading skeleton",
    blurb: "Busy glass card skeleton for async metric placeholders.",
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
  if (kind === "empty") {
    return (
      <Sample caption="No activity yet">
        <OrgEhgCardGlassEmpty />
      </Sample>
    );
  }
  if (kind === "edit") {
    return (
      <Sample caption="Edit product">
        <OrgEhgCardGlassEditForm />
      </Sample>
    );
  }
  if (kind === "confirm") {
    return (
      <Sample caption="Archive transfer">
        <OrgEhgCardGlassConfirm />
      </Sample>
    );
  }
  if (kind === "profile") {
    return (
      <Sample caption="Ana Reyes">
        <OrgEhgCardGlassProfile />
      </Sample>
    );
  }
  if (kind === "checklist") {
    return (
      <Sample caption="Closing checklist">
        <OrgEhgCardGlassChecklist />
      </Sample>
    );
  }
  if (kind === "notification") {
    return (
      <Sample caption="Notifications">
        <OrgEhgCardGlassNotification />
      </Sample>
    );
  }
  if (kind === "pricing") {
    return (
      <Sample caption="Growth plan">
        <OrgEhgCardGlassPricing />
      </Sample>
    );
  }
  if (kind === "invoice") {
    return (
      <Sample caption="Invoice INV-2084">
        <OrgEhgCardGlassInvoice />
      </Sample>
    );
  }
  if (kind === "invite") {
    return (
      <Sample caption="Invite staff">
        <OrgEhgCardGlassInvite />
      </Sample>
    );
  }
  if (kind === "schedule") {
    return (
      <Sample caption="Stock count">
        <OrgEhgCardGlassSchedule />
      </Sample>
    );
  }
  if (kind === "media") {
    return (
      <Sample caption="Product photo">
        <OrgEhgCardGlassMedia />
      </Sample>
    );
  }
  if (kind === "comparison") {
    return (
      <Sample caption="Branch comparison">
        <OrgEhgCardGlassComparison />
      </Sample>
    );
  }
  if (kind === "loading") {
    return (
      <Sample caption="Loading">
        <OrgEhgCardGlassLoading />
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
