import type { Metadata } from "next";
import Link from "next/link";
import {
  Building2,
  CreditCard,
  Package,
  ShieldCheck,
  ShoppingCart,
  Truck,
  Users,
  WifiOff,
} from "lucide-react";

import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsCtaSection } from "@/components/exits/ExItsCtaSection";
import { ExItsFaq } from "@/components/exits/ExItsFaq";
import { ExItsFeatureGrid } from "@/components/exits/ExItsFeatureGrid";
import { ExItsProductShowcase } from "@/components/exits/ExItsProductShowcase";
import { ExItsReveal } from "@/components/exits/ExItsReveal";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { ExItsStatsStrip } from "@/components/exits/ExItsStatsStrip";
import { ExItsVisualPlaceholder } from "@/components/exits/ExItsVisualPlaceholder";
import { ctaClassName } from "@/lib/cta";
import { posFaq, posSoftwareApplicationJsonLd } from "@/lib/pos-content";

export const metadata: Metadata = {
  title: "Pinoy Business POS — Point of Sale for Filipino Businesses",
  description:
    "Pinoy Business POS by ExItS — point-of-sale, inventory, customer credit (Utang), supplier ordering, and multi-branch management for Filipino retailers.",
  alternates: {
    canonical: "https://exits.ph/pos",
  },
  openGraph: {
    title: "Pinoy Business POS — Point of Sale for Filipino Businesses | ExItS",
    description:
      "Pinoy Business POS by ExItS — point-of-sale, inventory, customer credit (Utang), supplier ordering, and multi-branch management for Filipino retailers.",
    url: "https://exits.ph/pos",
    images: [
      {
        url: "/og/exits-og-pos.png",
        width: 1200,
        height: 630,
        alt: "Pinoy Business POS by ExItS",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "Pinoy Business POS — Point of Sale for Filipino Businesses | ExItS",
    description:
      "Pinoy Business POS by ExItS — point-of-sale, inventory, customer credit (Utang), supplier ordering, and multi-branch management for Filipino retailers.",
  },
};

const growthSteps = [
  {
    id: "personal-sellers",
    title: "Single branch — start simple",
    body: "Set up your catalog, open your register, and process your first sale.",
  },
  {
    id: "small-businesses",
    title: "Add staff and stay accountable",
    body: "Invite team members, assign roles, and track inventory as you grow.",
  },
  {
    id: "multi-branch-businesses",
    title: "Expand with multi-branch control",
    body: "Open new locations, manage stock per branch, and group branches into Areas for oversight.",
  },
  {
    title: "Connect your supply chain",
    body: "Order from suppliers in the system — including other ExItS businesses.",
  },
];

export default function PosPage() {
  return (
    <main>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(posSoftwareApplicationJsonLd),
        }}
      />

      <section className="relative overflow-hidden border-b border-borderDefault">
        <div
          className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,rgba(16,185,129,0.12),transparent_48%)]"
          aria-hidden="true"
        />
        <ExItsContainer className="relative grid items-center gap-12 py-16 lg:min-h-[70vh] lg:grid-cols-2 lg:py-24">
          <ExItsReveal>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-brandBright">
              Flagship product
            </p>
            <h1 className="mt-3 text-4xl font-semibold tracking-tight text-primary sm:text-5xl lg:text-6xl">
              Pinoy Business POS
            </h1>
            <p className="mt-6 max-w-xl text-base leading-relaxed text-muted sm:text-lg">
              The complete point-of-sale and business management system for Filipino
              retailers. Sell, track inventory, manage staff, and grow — even without
              the internet.
            </p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Link href="/contact" className={ctaClassName("primary")}>
                Get Started
              </Link>
              <Link href="/pricing" className={ctaClassName("secondary")}>
                See Pricing
              </Link>
            </div>
          </ExItsReveal>
          <ExItsVisualPlaceholder
            title="Pinoy Business POS"
            caption="Interface preview reserved for a real product screenshot. This is not an application capture."
          />
        </ExItsContainer>
      </section>

      <ExItsStatsStrip
        columns={5}
        items={[
          { label: "Sell online and offline", icon: WifiOff },
          { label: "Multi-branch and Area management", icon: Building2 },
          { label: "Built-in Utang (customer credit)", icon: CreditCard },
          { label: "Supplier purchase orders", icon: Truck },
          { label: "Role-based staff access", icon: ShieldCheck },
        ]}
      />

      <ExItsProductShowcase
        headline="Sell confidently — online or offline"
        body="Pinoy Business POS keeps your register running even when your connection doesn't. Transactions sync automatically when you're back online. Every sale is protected against duplicates so your records stay accurate."
        benefits={[
          "Real-time POS cart and sale recording",
          "Offline mode — sell without internet",
          "Cashier shift management with cash tracking",
          "Sale returns with stock adjustment",
          "Register management per branch",
        ]}
        visual={
          <ExItsVisualPlaceholder
            title="Selling"
            caption="Screenshot slot for the POS selling screen. Real images will replace this treatment when design assets are ready."
          />
        }
      />

      <ExItsProductShowcase
        headline="Your catalog, your way"
        body="Organize your products the way your business works. Set different prices per branch, track stock levels, and flag items nearing expiration."
        benefits={[
          "Categories, variants, and product images",
          "Branch-specific pricing overrides",
          "Stock tracking per branch",
          "Expiration tracking",
          "Catalog import for bulk setup",
        ]}
        reversed
        visual={
          <ExItsVisualPlaceholder
            title="Catalog & inventory"
            caption="Screenshot slot for catalog and inventory views. Not a live product capture."
          />
        }
      />

      <ExItsProductShowcase
        headline="Know your customers. Manage Utang properly."
        body="Utang is part of how Filipino businesses work. Pinoy Business POS gives you a proper system — track credit limits, outstanding balances, and payment history without a spreadsheet."
        benefits={[
          "Customer list per branch",
          "Utang (customer credit) with credit limit management",
          "Link ExItS Personal users as digital customers",
          "Customer digital storefront for online orders",
        ]}
        visual={
          <ExItsVisualPlaceholder
            title="Customers & Utang"
            caption="Screenshot slot for customer and Utang management. Placeholder only."
          />
        }
      />

      <ExItsProductShowcase
        headline="Stay stocked. Stay connected."
        body="Manage your suppliers and raise purchase orders without leaving the system. Connect with other ExItS businesses to order stock directly from your supply network."
        benefits={[
          "Supplier management",
          "Purchase orders and direct purchase receipts",
          "Supplier payable tracking",
          "Connected ExItS supplier network",
          "Supplier connection request and approval workflow",
        ]}
        reversed
        visual={
          <ExItsVisualPlaceholder
            title="Purchasing"
            caption="Screenshot slot for purchasing and supplier workflows. Placeholder only."
          />
        }
      />

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <div className="max-w-3xl">
            <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
              From one branch to many — stay in control
            </h2>
            <p className="mt-4 text-base leading-relaxed text-muted">
              Open new branches as your business grows. Group them into areas for easier
              oversight. Each branch manages its own stock, staff, and customer ordering
              settings.
            </p>
          </div>
          <div className="mt-10">
            <ExItsFeatureGrid
              columns={2}
              items={[
                {
                  title: "Multi-branch setup",
                  body: "Run multiple locations under one organization, each with its own operations.",
                  icon: Building2,
                },
                {
                  title: "Branch-owned stock",
                  body: "Stock, pricing, and staff stay scoped to each branch — not to Areas.",
                  icon: Package,
                },
                {
                  title: "Area grouping",
                  body: "Group branches into Areas for staff oversight and rollup reporting. Areas do not own inventory.",
                  icon: Users,
                },
                {
                  title: "Per-branch ordering",
                  body: "Control ordering availability, operating hours, and readiness checks per branch.",
                  icon: ShoppingCart,
                },
              ]}
            />
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <div className="max-w-3xl">
            <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
              The right access for every person
            </h2>
            <p className="mt-4 text-base leading-relaxed text-muted">
              Give each team member exactly what they need to do their job — no more, no
              less. Owners control everything. Managers and cashiers have appropriate
              access for their role.
            </p>
          </div>
          <div className="mt-10">
            <ExItsFeatureGrid
              columns={3}
              items={[
                {
                  title: "Owner",
                  body: "Full control across the organization, including sensitive settings and oversight.",
                  icon: ShieldCheck,
                },
                {
                  title: "Manager",
                  body: "Operational access for day-to-day branch and team management.",
                  icon: Users,
                },
                {
                  title: "Cashier",
                  body: "Focused selling access for register work, with limits on sensitive reports.",
                  icon: ShoppingCart,
                },
              ]}
            />
          </div>
          <ul className="mt-8 space-y-3 text-sm leading-relaxed text-muted">
            <li>Per-branch staff scoping</li>
            <li>Staff invitation and onboarding</li>
            <li>Grant-based authorization — not hard-coded role names alone</li>
            <li>Audit trail on sensitive operations</li>
          </ul>
        </ExItsContainer>
      </ExItsSection>

      <ExItsProductShowcase
        headline="Let customers order from you online"
        body="Enable your digital storefront and let linked ExItS Personal customers browse your catalog and place orders. Your team reviews and accepts orders in the same system you use every day."
        benefits={[
          "Public store page with ordering availability",
          "Customer order accept / reject workflow",
          "Order status notifications to buyers",
          "Per-branch ordering toggle",
        ]}
        visual={
          <ExItsVisualPlaceholder
            title="Customer storefront"
            caption="Screenshot slot for the customer storefront experience. Placeholder only."
          />
        }
      />

      <ExItsProductShowcase
        headline="Know your numbers"
        body="Get visibility into sales, inventory, and supplier activity across your business."
        benefits={[
          "Cashier shift reports",
          "Sales summaries",
          "Inventory levels per branch",
          "Supplier payable statements",
        ]}
        reversed
        visual={
          <ExItsVisualPlaceholder
            title="Reports & shifts"
            caption="Screenshot slot for reporting views. Placeholder only."
          />
        }
      />

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Grow with Pinoy Business POS
          </h2>
          <ol className="mt-10 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {growthSteps.map((step, index) => (
              <li
                key={step.title}
                id={step.id}
                className="scroll-mt-28 rounded-xl border border-borderDefault bg-surface p-6"
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
              Find the right plan for your business.
            </h2>
            <p className="mt-3 text-sm leading-relaxed text-muted">
              Pricing is being finalized. See the pricing page for current information —
              no peso amounts are published until pricing is approved.
            </p>
          </div>
          <Link href="/pricing" className={ctaClassName("secondary")}>
            See Pricing
          </Link>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
        <ExItsContainer className="max-w-3xl">
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Frequently asked questions
          </h2>
          <div className="mt-8">
            <ExItsFaq items={[...posFaq]} />
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsCtaSection
        headline="Start using Pinoy Business POS today"
        primaryCta={{ href: "/contact", label: "Get Started" }}
        secondaryCta={{ href: "/contact", label: "Have questions?" }}
      />
    </main>
  );
}
