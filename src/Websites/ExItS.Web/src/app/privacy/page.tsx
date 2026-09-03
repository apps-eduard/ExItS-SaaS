import type { Metadata } from "next";

import { ExItsLegalDocument } from "@/components/exits/ExItsLegalDocument";
import PrivacyContent from "@/content/privacy.mdx";

export const metadata: Metadata = {
  title: "Privacy Policy",
  description:
    "ExItS privacy policy — being finalized pending legal review. Contact us with questions about your data.",
  alternates: {
    canonical: "https://exits.ph/privacy",
  },
  openGraph: {
    title: "Privacy Policy | ExItS",
    description:
      "ExItS privacy policy — being finalized pending legal review. Contact us with questions about your data.",
    url: "https://exits.ph/privacy",
  },
  twitter: {
    card: "summary_large_image",
    title: "Privacy Policy | ExItS",
    description:
      "ExItS privacy policy — being finalized pending legal review. Contact us with questions about your data.",
  },
};

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
