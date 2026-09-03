import type { Metadata } from "next";
import Link from "next/link";

import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsCtaSection } from "@/components/exits/ExItsCtaSection";
import { ExItsFaq } from "@/components/exits/ExItsFaq";
import { ExItsPricingCard } from "@/components/exits/ExItsPricingCard";
import { ExItsPricingComparisonTable } from "@/components/exits/ExItsPricingComparisonTable";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { ctaClassName } from "@/lib/cta";
import {
  pricingCapabilityRows,
  pricingCardSlots,
  pricingFaq,
} from "@/lib/pricing-content";
import { buildPageMetadata, faqPageJsonLd } from "@/lib/site-seo";

export const metadata: Metadata = buildPageMetadata("/pricing");

export default function PricingPage() {
  return (
    <main>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(faqPageJsonLd(pricingFaq)),
        }}
      />

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-20">
        <ExItsContainer className="max-w-3xl">
          <h1 className="text-4xl font-semibold tracking-tight text-primary sm:text-5xl">
            Simple pricing for every stage of your business
          </h1>
          <p className="mt-4 text-base leading-relaxed text-muted sm:text-lg">
            Pinoy Business POS is the flagship ExItS product. Commercial plan names, prices,
            and limits are being finalized — no peso amounts are published on this page yet.
          </p>
          <p className="mt-4 rounded-xl border border-borderDefault bg-surface px-5 py-4 text-sm leading-relaxed text-muted">
            Status: commercial pricing is still being finalized. The layout below is ready to
            receive approved plan data without inventing commercial terms.
          </p>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer>
          <div className="grid gap-6 lg:grid-cols-3">
            {pricingCardSlots.map((slot) => (
              <ExItsPricingCard
                key={slot.planName}
                planName={slot.planName}
                price={slot.price}
                priceNote={slot.priceNote}
                features={[...slot.features]}
                recommended={slot.recommended}
                className={slot.orderClassName}
                cta={{ href: "/contact", label: "Get Started" }}
              />
            ))}
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Confirmed capabilities
          </h2>
          <p className="mt-4 max-w-3xl text-base leading-relaxed text-muted">
            These capabilities are confirmed in Pinoy Business POS. Which commercial plan
            packages which limits will be published when pricing is approved.
          </p>
          <div className="mt-10">
            <ExItsPricingComparisonTable
              caption="Confirmed Pinoy Business POS capabilities and pending plan packaging"
              rows={[...pricingCapabilityRows]}
            />
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer className="max-w-3xl">
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Pricing FAQ
          </h2>
          <div className="mt-8">
            <ExItsFaq items={[...pricingFaq]} />
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-12">
        <ExItsContainer className="flex flex-col items-start justify-between gap-4 rounded-xl border border-borderDefault bg-surface px-6 py-8 sm:flex-row sm:items-center sm:px-10">
          <p className="max-w-xl text-base leading-relaxed text-muted">
            Prefer a conversation while plans are being finalized?
          </p>
          <Link href="/contact" className={ctaClassName("secondary")}>
            Request a Demo
          </Link>
        </ExItsContainer>
      </ExItsSection>

      <ExItsCtaSection
        headline="Not sure which plan is right? Talk to us."
        primaryCta={{ href: "/contact", label: "Talk to Us" }}
        secondaryCta={{ href: "/pos", label: "Explore Pinoy Business POS" }}
      />
    </main>
  );
}
