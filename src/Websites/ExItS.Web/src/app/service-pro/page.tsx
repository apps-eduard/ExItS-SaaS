import type { Metadata } from "next";
import Link from "next/link";

import { ExItsBadge } from "@/components/exits/ExItsBadge";
import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { ExItsWaitlistForm } from "@/components/exits/ExItsWaitlistForm";
import { ctaClassName } from "@/lib/cta";

export const metadata: Metadata = {
  title: "Pinoy Service Pro — Coming Soon",
  description:
    "Pinoy Service Pro by ExItS — service business management for Filipino service organizations. Coming soon.",
  alternates: {
    canonical: "https://exits.ph/service-pro",
  },
  robots: {
    index: true,
    follow: true,
  },
  openGraph: {
    title: "Pinoy Service Pro — Coming Soon | ExItS",
    description:
      "Pinoy Service Pro by ExItS — service business management for Filipino service organizations. Coming soon.",
    url: "https://exits.ph/service-pro",
  },
  twitter: {
    card: "summary_large_image",
    title: "Pinoy Service Pro — Coming Soon | ExItS",
    description:
      "Pinoy Service Pro by ExItS — service business management for Filipino service organizations. Coming soon.",
  },
};

const plannedAreas = [
  "Service appointment management (planned)",
  "Service job tracking (planned)",
  "Staff / service provider management (planned)",
  "Customer management (planned)",
  "Multi-branch support (planned)",
];

export default function ServiceProPage() {
  return (
    <main>
      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer className="max-w-3xl">
          <ExItsBadge variant="coming-soon">Coming Soon</ExItsBadge>
          <h1 className="mt-5 text-4xl font-semibold tracking-tight text-primary sm:text-5xl">
            Pinoy Service Pro
          </h1>
          <p className="mt-6 text-base leading-relaxed text-muted sm:text-lg">
            Designed for Filipino service organizations — barbershops, salons, spas, repair
            shops, cleaning teams, and more. This product is planned. Implementation has not
            started, and no features are available today.
          </p>
          <ExItsWaitlistForm idPrefix="service-pro-waitlist" submitLabel="Get Notified" />
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="border-b border-borderDefault py-16 lg:py-24">
        <ExItsContainer className="max-w-3xl">
          <h2 className="text-3xl font-semibold tracking-tight text-primary">
            What to expect
          </h2>
          <p className="mt-4 text-base leading-relaxed text-muted">
            Intended product areas from planning documentation only. Nothing below is
            implemented or available.
          </p>
          <ul className="mt-8 space-y-3">
            {plannedAreas.map((item) => (
              <li
                key={item}
                className="rounded-xl border border-borderDefault bg-surface px-5 py-4 text-sm text-primary"
              >
                {item}
              </li>
            ))}
          </ul>
        </ExItsContainer>
      </ExItsSection>

      <ExItsSection className="py-16 lg:py-20">
        <ExItsContainer>
          <div className="flex flex-col items-start justify-between gap-6 rounded-xl border border-borderDefault bg-surface px-6 py-10 sm:flex-row sm:items-center sm:px-10">
            <div className="max-w-xl">
              <h2 className="text-2xl font-semibold tracking-tight text-primary">
                Already need a business management tool?
              </h2>
              <p className="mt-3 text-sm leading-relaxed text-muted">
                Try Pinoy Business POS today — the ExItS flagship product with a working
                implementation.
              </p>
            </div>
            <Link href="/pos" className={ctaClassName("primary")}>
              Explore Pinoy Business POS
            </Link>
          </div>
        </ExItsContainer>
      </ExItsSection>
    </main>
  );
}
