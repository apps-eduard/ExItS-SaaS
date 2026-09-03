import type { Metadata } from "next";

import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsContactFormsPanel } from "@/components/exits/ExItsContactFormsPanel";
import { ExItsSection } from "@/components/exits/ExItsSection";

export const metadata: Metadata = {
  title: {
    absolute: "Contact ExItS | Sales, Partnerships, and Support",
  },
  description:
    "Get in touch with ExItS — for sales inquiries, partnerships, and general questions about the ExItS platform.",
  alternates: {
    canonical: "https://exits.ph/contact",
  },
  openGraph: {
    title: "Contact ExItS | Sales, Partnerships, and Support",
    description:
      "Get in touch with ExItS — for sales inquiries, partnerships, and general questions about the ExItS platform.",
    url: "https://exits.ph/contact",
  },
  twitter: {
    card: "summary_large_image",
    title: "Contact ExItS | Sales, Partnerships, and Support",
    description:
      "Get in touch with ExItS — for sales inquiries, partnerships, and general questions about the ExItS platform.",
  },
};

export default function ContactPage() {
  return (
    <main>
      <ExItsSection className="border-b border-borderDefault py-16 lg:py-20">
        <ExItsContainer className="max-w-3xl">
          <h1 className="text-4xl font-semibold tracking-tight text-primary sm:text-5xl">
            Contact ExItS
          </h1>
          <p className="mt-4 text-base leading-relaxed text-muted sm:text-lg">
            Reach out for general questions, sales conversations, or partnership ideas.
            Physical address, phone, and public email details are not published yet.
          </p>
          <p className="mt-4 rounded-xl border border-borderDefault bg-surface px-5 py-4 text-sm leading-relaxed text-muted">
            Form submission is not connected yet. You can fill and validate these forms now;
            messages will be delivered when the inquiry endpoint is ready.
          </p>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="py-16 lg:py-24">
        <ExItsContainer className="max-w-4xl">
          <ExItsContactFormsPanel />
        </ExItsContainer>
      </ExItsSection>
    </main>
  );
}
