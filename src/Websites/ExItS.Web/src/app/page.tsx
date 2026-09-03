import type { Metadata } from "next";
import Link from "next/link";
import {
  Building2,
  CreditCard,
  Lock,
  MapPinned,
  ShieldCheck,
  ShoppingBag,
  Store,
  Truck,
  WifiOff,
} from "lucide-react";

import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsCtaSection } from "@/components/exits/ExItsCtaSection";
import { ExItsFaq } from "@/components/exits/ExItsFaq";
import { ExItsHero } from "@/components/exits/ExItsHero";
import { ExItsNewsletter } from "@/components/exits/ExItsNewsletter";
import { ExItsProductShowcase } from "@/components/exits/ExItsProductShowcase";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { ExItsSegmentedTabs } from "@/components/exits/ExItsSegmentedTabs";
import { ExItsStatsStrip } from "@/components/exits/ExItsStatsStrip";
import { ExItsTrustStrip } from "@/components/exits/ExItsTrustStrip";
import { ExItsBadge } from "@/components/exits/ExItsBadge";
import { ExItsVisualPlaceholder } from "@/components/exits/ExItsVisualPlaceholder";
import { ctaClassName } from "@/lib/cta";
import { homepageFaq, organizationJsonLd } from "@/lib/homepage-content";
import { buildPageMetadata, faqPageJsonLd } from "@/lib/site-seo";

export const metadata: Metadata = buildPageMetadata("/");

const audiences = [
  {
    title: "Personal sellers / solo business",
    body: "Starting out? Pinoy Business POS grows with you — from your first sale to your first hire.",
    href: "/pos#personal-sellers",
  },
  {
    title: "Small / growing businesses",
    body: "Manage your team, your stock, and your customers from one place. Know your numbers every day.",
    href: "/pos#small-businesses",
  },
  {
    title: "Established multi-branch retailers",
    body: "Add branches, group them into areas, and stay in control across your entire operation.",
    href: "/pos#multi-branch-businesses",
  },
];

const growthSteps = [
  {
    title: "Start selling today",
    body: "Set up your catalog, open your register, and process your first sale in minutes.",
  },
  {
    title: "Build your team",
    body: "Invite staff, assign roles, and keep every shift accountable.",
  },
  {
    title: "Expand to more locations",
    body: "Open new branches, manage inventory per location, and group branches into areas for easier oversight.",
  },
  {
    title: "Connect your supply chain",
    body: "Order from suppliers directly in the system — even from other ExItS businesses.",
  },
];

const otherProducts = [
  {
    name: "Pinoy Service Pro",
    description: "Planned service-business management for Filipino service organizations.",
    badge: "coming-soon" as const,
    badgeLabel: "Coming Soon",
    href: "/service-pro",
  },
  {
    name: "Pinoy Loan Manager",
    description: "Planned lending operations product. Implementation is not available yet.",
    badge: "coming-soon" as const,
    badgeLabel: "Coming Soon",
    href: "/products",
  },
  {
    name: "Pinoy Buy Now Pay Later",
    description: "In development. Project scaffold only — no financing capability yet.",
    badge: "in-development" as const,
    badgeLabel: "In Development",
    href: "/products",
  },
  {
    name: "Pinoy Pawn Manager",
    description: "In development. Project scaffold only — no pawn operations yet.",
    badge: "in-development" as const,
    badgeLabel: "In Development",
    href: "/products",
  },
];

export default function Home() {
  return (
    <main>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(organizationJsonLd) }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(faqPageJsonLd(homepageFaq)),
        }}
      />

      <ExItsHero
        outline="One platform."
        solid="Every tool your business needs."
        subHeadline="ExItS gives Filipino businesses the tools to sell, manage inventory, track customers, and grow — from a single branch to many."
        primaryCta={{ href: "/contact", label: "Get Started" }}
        secondaryCta={{ href: "/products", label: "See All Products" }}
        visual={
          <ExItsVisualPlaceholder
            title="Pinoy Business POS"
            caption="Interface preview reserved for a real product screenshot. No application capture is shown here."
          />
        }
      />

      <ExItsStatsStrip
        items={[
          { label: "Sell online and offline", icon: WifiOff },
          { label: "Multi-branch management", icon: Building2 },
          { label: "Built-in Utang (customer credit)", icon: CreditCard },
          { label: "Supplier purchase orders", icon: Truck },
          { label: "Role-based staff access", icon: ShieldCheck },
          { label: "Customer digital storefront", icon: Store },
        ]}
      />

      <ExItsProductShowcase
        label="Featured Product"
        headline="Pinoy Business POS"
        body="Your complete point-of-sale and business management system. Manage sales, inventory, suppliers, staff, and customers — all from one dashboard. Works even when the internet doesn't."
        benefits={[
          "Sell at the counter or through an online storefront",
          "Track every item in every branch",
          "Record Utang — and never lose track of who owes what",
          "Order from suppliers with connected purchase orders",
          "Give each staff member the right access — no more, no less",
        ]}
        cta={{ href: "/pos", label: "Explore Pinoy Business POS" }}
        visual={
          <ExItsVisualPlaceholder
            title="Featured product"
            caption="Screenshot slot for Pinoy Business POS. Real images will replace this treatment when design assets are ready."
          />
        }
      />

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            What Pinoy Business POS can do
          </h2>
          <p className="mt-4 max-w-2xl text-base leading-relaxed text-muted">
            Depth without leaving the homepage — only capabilities confirmed in the product.
          </p>
          <div className="mt-10">
            <ExItsSegmentedTabs
              items={[
                {
                  id: "selling",
                  label: "Selling",
                  title: "Sell in real time, even offline",
                  body: "Real-time POS, offline-capable selling, cashier shifts, and sale returns.",
                },
                {
                  id: "inventory",
                  label: "Inventory",
                  title: "Know what each branch holds",
                  body: "Catalog management, branch-owned stock, and expiration tracking.",
                },
                {
                  id: "customers",
                  label: "Customers",
                  title: "Customers and Utang in one place",
                  body: "Customer lists, Utang (credit), and linked customer orders.",
                },
                {
                  id: "suppliers",
                  label: "Suppliers",
                  title: "Purchase from your suppliers",
                  body: "Supplier management, connected ExItS suppliers, and purchase orders.",
                },
              ]}
            />
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Who it is for
          </h2>
          <div className="mt-10 grid gap-4 md:grid-cols-3">
            {audiences.map((audience) => (
              <article
                key={audience.title}
                className="flex flex-col rounded-xl border border-borderDefault bg-surface p-6"
              >
                <h3 className="text-lg font-semibold text-primary">{audience.title}</h3>
                <p className="mt-3 flex-1 text-sm leading-relaxed text-muted">{audience.body}</p>
                <Link
                  href={audience.href}
                  className={ctaClassName("ghost", "mt-6 justify-start text-sm")}
                >
                  Learn more
                </Link>
              </article>
            ))}
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Other ExItS products
          </h2>
          <p className="mt-4 max-w-2xl text-base leading-relaxed text-muted">
            The platform is growing. These products are planned or in early development — they are not generally available.
          </p>
          <div className="mt-10 grid gap-4 sm:grid-cols-2">
            {otherProducts.map((product) => (
              <article
                key={product.name}
                className="rounded-xl border border-borderDefault bg-surface p-6"
              >
                <div className="flex flex-wrap items-center gap-3">
                  <h3 className="text-lg font-semibold text-primary">{product.name}</h3>
                  <ExItsBadge variant={product.badge}>{product.badgeLabel}</ExItsBadge>
                </div>
                <p className="mt-3 text-sm leading-relaxed text-muted">{product.description}</p>
                <Link
                  href={product.href}
                  className={ctaClassName("ghost", "mt-5 justify-start text-sm")}
                >
                  Learn More
                </Link>
              </article>
            ))}
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Grow with ExItS
          </h2>
          <ol className="mt-10 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {growthSteps.map((step, index) => (
              <li
                key={step.title}
                className="rounded-xl border border-borderDefault bg-surface p-6"
              >
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-brandBright">
                  Step {index + 1}
                </p>
                <h3 className="mt-3 text-lg font-semibold text-primary">{step.title}</h3>
                <p className="mt-3 text-sm leading-relaxed text-muted">{step.body}</p>
              </li>
            ))}
          </ol>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-20">
        <ExItsContainer className="flex flex-col items-start justify-between gap-6 rounded-xl border border-borderDefault bg-surface px-6 py-10 sm:flex-row sm:items-center sm:px-10">
          <div className="max-w-xl">
            <h2 className="text-2xl font-semibold tracking-tight text-primary">
              Simple, transparent pricing for every stage.
            </h2>
            <p className="mt-3 text-sm leading-relaxed text-muted">
              Plans are being finalized. See the pricing page for current information — no peso amounts are published until pricing is approved.
            </p>
          </div>
          <Link href="/pricing" className={ctaClassName("secondary")}>
            See Pricing
          </Link>
        </ExItsContainer>
      </ExItsSection>

      <ExItsTrustStrip
        items={[
          { label: "Works offline — keep selling when the internet drops", icon: WifiOff },
          { label: "Branch-level control — each location manages its own stock and staff", icon: MapPinned },
          { label: "Built-in Utang — Filipino-style customer credit, managed properly", icon: CreditCard },
          { label: "Your data stays yours", icon: Lock },
        ]}
      />

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer className="max-w-3xl">
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Frequently asked questions
          </h2>
          <div className="mt-8">
            <ExItsFaq items={[...homepageFaq]} />
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <div className="flex items-start gap-3">
            <ShoppingBag className="mt-1 h-5 w-5 text-brandBright" aria-hidden="true" />
            <div>
              <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
                Stay updated on ExItS
              </h2>
              <p className="mt-4 max-w-xl text-base leading-relaxed text-muted">
                Product updates and launch announcements. Submission is not connected until the waitlist endpoint is ready.
              </p>
              <ExItsNewsletter />
            </div>
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsCtaSection
        headline="Ready to run your business better?"
        primaryCta={{ href: "/contact", label: "Get Started" }}
        secondaryCta={{ href: "/contact", label: "Talk to Us" }}
      />
    </main>
  );
}
