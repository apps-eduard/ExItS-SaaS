import type { Metadata } from "next";

import { ExItsLegalDocument } from "@/components/exits/ExItsLegalDocument";
import TermsContent from "@/content/terms.mdx";

import { buildPageMetadata } from "@/lib/site-seo";

export const metadata: Metadata = buildPageMetadata("/terms");

export default function TermsPage() {
  return (
    <ExItsLegalDocument
      title="Terms of Service"
      description="The legal relationship between ExItS and users of exits.ph and ExItS products will be defined here after qualified legal review."
      lastUpdatedLabel="Pending legal review"
    >
      <TermsContent />
    </ExItsLegalDocument>
  );
}
