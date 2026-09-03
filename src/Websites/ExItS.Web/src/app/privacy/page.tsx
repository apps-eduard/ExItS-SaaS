import type { Metadata } from "next";

import { ExItsLegalDocument } from "@/components/exits/ExItsLegalDocument";
import PrivacyContent from "@/content/privacy.mdx";

import { buildPageMetadata } from "@/lib/site-seo";

export const metadata: Metadata = buildPageMetadata("/privacy");

export default function PrivacyPage() {
  return (
    <ExItsLegalDocument
      title="Privacy Policy"
      description="How ExItS intends to disclose data practices for exits.ph and ExItS products — subject to legal review before any final policy is published."
      lastUpdatedLabel="Pending legal review"
    >
      <PrivacyContent />
    </ExItsLegalDocument>
  );
}
