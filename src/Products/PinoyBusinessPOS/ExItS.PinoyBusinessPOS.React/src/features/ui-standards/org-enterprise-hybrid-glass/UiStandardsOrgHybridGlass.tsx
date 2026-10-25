import type { UiStandardFilterCategory } from "@/features/ui-standards/ui-standard-catalog";
import { OrgEhgButtonGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgButtonGlassGallery";
import { OrgEhgInputGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInputGlassGallery";
import { OrgEhgDateGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassGallery";
import { OrgEhgQuickActionGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import { OrgEhgCardGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgCardGlassGallery";
import { parseOrgEhgCardGlassKind } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgCardGlassKinds";
import { OrgEhgDashboardLive } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDashboardLive";
import { OrgEhgInventoryLive } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInventoryLive";
import { OrgEhgOrdersLive } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgOrdersLive";
import { OrgEhgStockRequestsLive } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgStockRequestsLive";
import { OrgEhgBranchSettingsLive } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgBranchSettingsLive";

/** Live Organization workflow reference pages (authenticated APIs). */
export const ORG_EHG_LIVE_CATEGORIES = [
  "org-ehg-dashboard",
  "org-ehg-inventory",
  "org-ehg-orders",
  "org-ehg-stock-requests",
  "org-ehg-branch-settings",
] as const;

const ORG_EHG_CARD_GLASS_CATEGORIES = [
  "org-ehg-card-glass-menu",
  "org-ehg-card-glass-soft-kpi",
  "org-ehg-card-glass-hybrid-kpi",
  "org-ehg-card-glass-solid",
  "org-ehg-card-glass-timeline",
  "org-ehg-card-glass-table",
  "org-ehg-card-glass-settings",
] as const;

/** All Organization Hybrid Glass UI Standards categories (live pages + glass samples). */
export const ORG_EHG_CATEGORIES = [
  ...ORG_EHG_LIVE_CATEGORIES,
  "org-ehg-button-glass",
  "org-ehg-input-glass",
  "org-ehg-date-glass",
  "org-ehg-quick-action-glass",
  ...ORG_EHG_CARD_GLASS_CATEGORIES,
] as const;

export type OrgEhgCategory = (typeof ORG_EHG_CATEGORIES)[number];

export function isOrgEhgCategory(category: UiStandardFilterCategory): category is OrgEhgCategory {
  return (ORG_EHG_CATEGORIES as readonly string[]).includes(category);
}

/**
 * Organization — Enterprise Hybrid Glass host (UI Standards / DEV only).
 * Live workflow pages + glass component samples.
 */
export function UiStandardsOrgHybridGlass({ category }: { category: OrgEhgCategory }) {
  const cardKind = parseOrgEhgCardGlassKind(category);
  if (cardKind) {
    return <OrgEhgCardGlassGallery kind={cardKind} />;
  }

  switch (category) {
    case "org-ehg-dashboard":
      return <OrgEhgDashboardLive />;
    case "org-ehg-inventory":
      return <OrgEhgInventoryLive />;
    case "org-ehg-orders":
      return <OrgEhgOrdersLive />;
    case "org-ehg-stock-requests":
      return <OrgEhgStockRequestsLive />;
    case "org-ehg-branch-settings":
      return <OrgEhgBranchSettingsLive />;
    case "org-ehg-button-glass":
      return <OrgEhgButtonGlassGallery />;
    case "org-ehg-input-glass":
      return <OrgEhgInputGlassGallery />;
    case "org-ehg-date-glass":
      return <OrgEhgDateGlassGallery />;
    case "org-ehg-quick-action-glass":
      return <OrgEhgQuickActionGlassGallery />;
    default:
      return null;
  }
}
