import type { Metadata } from "next";

import { ExItsLegalDocument } from "@/components/exits/ExItsLegalDocument";
import TermsContent from "@/content/terms.mdx";

export const metadata: Metadata = {
  title: "Terms of Service",
  description:
    "ExItS terms of service — being finalized pending legal review. Contact us if you have questions.",
  alternates: {
    canonical: "https://exits.ph/terms",
  },
  openGraph: {
    title: "Terms of Service | ExItS",
    description:
      "ExItS terms of service — being finalized pending legal review. Contact us if you have questions.",
    url: "https://exits.ph/terms",
  },
  twitter: {
    card: "summary_large_image",
    title: "Terms of Service | ExItS",
    description:
      "ExItS terms of service — being finalized pending legal review. Contact us if you have questions.",
  },
};

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
