import { OrgEhgGlassCardPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgGlassCardPlayground";

/**
 * Glass card gallery — Org EHG card playground (Sample + Card / Header / Footer).
 * Preview splits light (left) and dark (right). UI Standards / DEV only.
 */
export function OrgEhgGlassCardGallery() {
  return (
    <div className="org-ehg-card-glass-gallery" data-testid="ui-standard-org-ehg-glass-card">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Glass card</strong>
        {" — "}
        Pick a sample from the dropdown (metrics, tables, forms, notifications, pricing, invoice,
        invite, schedule, media, comparison, loading, and more). Preview shows Light mode on the
        left and Dark mode on the right. Personal Soft Glass production cards are unchanged.
      </p>
      <OrgEhgGlassCardPlayground />
    </div>
  );
}
