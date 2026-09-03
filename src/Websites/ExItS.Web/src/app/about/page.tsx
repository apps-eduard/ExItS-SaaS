import type { Metadata } from "next";
import Link from "next/link";

import { ExItsBadge } from "@/components/exits/ExItsBadge";
import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsCtaSection } from "@/components/exits/ExItsCtaSection";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { ctaClassName } from "@/lib/cta";
import { organizationJsonLd } from "@/lib/homepage-content";

export const metadata: Metadata = {
  title: {
    absolute: "About ExItS | Multi-Product SaaS for Filipino Businesses",
  },
  description:
    "Learn about ExItS — the platform behind Pinoy Business POS and a growing suite of business management tools for Filipino businesses.",
  alternates: {
    canonical: "https://exits.ph/about",
  },
  openGraph: {
    title: "About ExItS | Multi-Product SaaS for Filipino Businesses",
    description:
      "Learn about ExItS — the platform behind Pinoy Business POS and a growing suite of business management tools for Filipino businesses.",
    url: "https://exits.ph/about",
  },
  twitter: {
    card: "summary_large_image",
    title: "About ExItS | Multi-Product SaaS for Filipino Businesses",
    description:
      "Learn about ExItS — the platform behind Pinoy Business POS and a growing suite of business management tools for Filipino businesses.",
  },
};

export default function AboutPage() {
  return (
    <main>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(organizationJsonLd) }}
      />

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-20">
        <ExItsContainer className="max-w-3xl">
          <h1 className="text-4xl font-semibold tracking-tight text-primary sm:text-5xl">
            About ExItS
          </h1>
          <p className="mt-4 text-base leading-relaxed text-muted sm:text-lg">
            ExItS is a multi-product SaaS platform built for Filipino businesses — one account
            identity, multiple business tools, and a flagship product that is ready to use today.
          </p>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer className="max-w-3xl">
          <h2 className="text-3xl font-semibold tracking-tight text-primary">
            Mission
          </h2>
          <p className="mt-4 text-base leading-relaxed text-muted">
            Filipino businesses deserve software designed for how they actually work. ExItS
            exists to provide purpose-built tools under one platform identity — including
            offline-capable selling, multi-branch operations, and Utang (customer credit)
            where those capabilities are confirmed in product.
          </p>
          <p className="mt-4 text-base leading-relaxed text-muted">
            We do not invent founding stories, team size, customer counts, or other metrics
            here. What we share is grounded in the platform and product work already built.
          </p>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary">
            The ExItS product ecosystem
          </h2>
          <p className="mt-4 max-w-3xl text-base leading-relaxed text-muted">
            Pinoy Business POS is the confirmed flagship product. Additional products are on
            the roadmap with clear readiness labels — planned or in early development, not
            generally available.
          </p>
          <ul className="mt-8 grid gap-4 sm:grid-cols-2">
            <li className="rounded-xl border border-borderDefault bg-surface p-6">
              <div className="flex flex-wrap items-center gap-3">
                <h3 className="text-lg font-semibold text-primary">Pinoy Business POS</h3>
                <ExItsBadge variant="available">Available</ExItsBadge>
              </div>
              <p className="mt-3 text-sm leading-relaxed text-muted">
                Point of sale and business management for Filipino retailers.
              </p>
              <Link href="/pos" className={ctaClassName("ghost", "mt-4 justify-start text-sm")}>
                Explore Pinoy Business POS
              </Link>
            </li>
            <li className="rounded-xl border border-borderDefault bg-surface p-6">
              <div className="flex flex-wrap items-center gap-3">
                <h3 className="text-lg font-semibold text-primary">More ExItS products</h3>
                <ExItsBadge variant="coming-soon">Coming Soon</ExItsBadge>
              </div>
              <p className="mt-3 text-sm leading-relaxed text-muted">
                Service Pro, Loan Manager, Buy Now Pay Later, and Pawn Manager are planned or
                in development — not released as available products.
              </p>
              <Link
                href="/products"
                className={ctaClassName("ghost", "mt-4 justify-start text-sm")}
              >
                See all products
              </Link>
            </li>
          </ul>
        </ExItsContainer>
      </ExItsSection>

      <ExItsCtaSection
        headline="Want to learn more or partner with us?"
        primaryCta={{ href: "/contact", label: "Contact ExItS" }}
        secondaryCta={{ href: "/products", label: "See All Products" }}
      />
    </main>
  );
}
