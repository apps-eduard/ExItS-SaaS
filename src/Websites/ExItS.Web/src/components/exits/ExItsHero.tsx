import Link from "next/link";
import type { ReactNode } from "react";

import { ctaClassName } from "@/lib/cta";
import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsOutlineHeading } from "@/components/exits/ExItsOutlineHeading";
import { ExItsReveal } from "@/components/exits/ExItsReveal";

export function ExItsHero({
  outline,
  solid,
  subHeadline,
  primaryCta,
  secondaryCta,
  visual,
}: {
  outline: string;
  solid: string;
  subHeadline: string;
  primaryCta: { href: string; label: string };
  secondaryCta: { href: string; label: string };
  visual: ReactNode;
}) {
  return (
    <section className="relative overflow-hidden border-b border-borderDefault">
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,rgba(16,185,129,0.14),transparent_46%)]"
        aria-hidden="true"
      />
      <ExItsContainer className="relative grid min-h-[80vh] items-center gap-12 py-16 lg:min-h-[calc(100svh-4.5rem)] lg:grid-cols-2 lg:py-24">
        <ExItsReveal>
          <ExItsOutlineHeading outline={outline} solid={solid} />
          <p className="mt-6 max-w-xl text-base leading-relaxed text-muted sm:text-lg">
            {subHeadline}
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Link href={primaryCta.href} className={ctaClassName("primary")}>
              {primaryCta.label}
            </Link>
            <Link href={secondaryCta.href} className={ctaClassName("secondary")}>
              {secondaryCta.label}
            </Link>
          </div>
        </ExItsReveal>
        <div className="lg:justify-self-end lg:w-full">{visual}</div>
      </ExItsContainer>
    </section>
  );
}
