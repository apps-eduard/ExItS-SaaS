import type { Metadata } from "next";
import {
  Building2,
  Link2,
  ShieldCheck,
  UserRound,
} from "lucide-react";

import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsCtaSection } from "@/components/exits/ExItsCtaSection";
import { ExItsProductCard } from "@/components/exits/ExItsProductCard";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { productsCatalog, roadmapNotes } from "@/lib/products-content";

import { buildPageMetadata } from "@/lib/site-seo";

export const metadata: Metadata = buildPageMetadata("/products");

export default function ProductsPage() {
  return (
    <main>
      <ExItsSection className="border-b border-borderDefault py-16 lg:py-20">
        <ExItsContainer className="max-w-3xl">
          <h1 className="text-4xl font-semibold tracking-tight text-primary sm:text-5xl">
            Our Products
          </h1>
          <p className="mt-4 text-base leading-relaxed text-muted sm:text-lg">
            ExItS is a multi-product platform for Filipino businesses — with Pinoy Business
            POS available now, and more products on the roadmap.
          </p>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer>
          <ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {productsCatalog.map((product) => (
              <li key={product.name} className="h-full">
                <ExItsProductCard {...product} />
              </li>
            ))}
          </ul>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-20">
        <ExItsContainer>
          <div className="rounded-xl border border-borderDefault bg-surface px-6 py-10 sm:px-10">
            <h2 className="text-2xl font-semibold tracking-tight text-primary">
              One account. Multiple business tools. Built for Filipino businesses.
            </h2>
            <ul className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {[
                { label: "One ExItS account", icon: UserRound },
                { label: "Multi-branch ready", icon: Building2 },
                { label: "Role-based access", icon: ShieldCheck },
                { label: "Connected platform", icon: Link2 },
              ].map((item) => {
                const Icon = item.icon;
                return (
                  <li
                    key={item.label}
                    className="rounded-xl border border-borderDefault bg-base px-4 py-5"
                  >
                    <Icon className="h-5 w-5 text-brandBright" aria-hidden="true" />
                    <p className="mt-3 text-sm font-medium text-primary">{item.label}</p>
                  </li>
                );
              })}
            </ul>
          </div>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer>
          <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
            Roadmap products without dedicated pages yet
          </h2>
          <p className="mt-4 max-w-2xl text-base leading-relaxed text-muted">
            These products do not have confirmed public marketing routes. Details below are
            status notes only — not available features.
          </p>
          <ul className="mt-10 grid gap-4 lg:grid-cols-3">
            {roadmapNotes.map((note) => (
              <li
                key={note.id}
                id={note.id}
                className="scroll-mt-28 rounded-xl border border-borderDefault bg-surface p-6"
              >
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-brandBright">
                  {note.status}
                </p>
                <h3 className="mt-3 text-lg font-semibold text-primary">{note.name}</h3>
                <p className="mt-3 text-sm leading-relaxed text-muted">{note.body}</p>
              </li>
            ))}
          </ul>
        </ExItsContainer>
      </ExItsSection>

      <ExItsCtaSection
        headline="Start with Pinoy Business POS"
        primaryCta={{ href: "/pos", label: "Explore Pinoy Business POS" }}
        secondaryCta={{ href: "/contact", label: "Have questions?" }}
      />
    </main>
  );
}
