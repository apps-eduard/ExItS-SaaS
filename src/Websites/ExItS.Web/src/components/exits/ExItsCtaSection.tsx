import Link from "next/link";
import { Sparkles } from "lucide-react";

import { ctaClassName } from "@/lib/cta";
import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsAnimatedGradient } from "@/components/exits/ExItsAnimatedGradient";

export function ExItsCtaSection({
  headline,
  primaryCta,
  secondaryCta,
}: {
  headline: string;
  primaryCta: { href: string; label: string };
  secondaryCta?: { href: string; label: string };
}) {
  return (
    <section className="relative z-0 overflow-hidden exits-section-tone-energy">
      <ExItsAnimatedGradient intensity="strong" />
      <ExItsContainer className="relative z-10 py-20 text-center lg:py-24">
        <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
          {headline}
        </h2>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <Link href={primaryCta.href} className={ctaClassName("primary")}>
            <Sparkles className="h-4 w-4 opacity-90" aria-hidden="true" />
            {primaryCta.label}
            <span aria-hidden="true" className="transition-transform group-hover:translate-x-1">
              →
            </span>
          </Link>
          {secondaryCta ? (
            <Link href={secondaryCta.href} className={ctaClassName("secondary")}>
              {secondaryCta.label}
              <span
                aria-hidden="true"
                className="transition-transform group-hover/cta:translate-x-1"
              >
                →
              </span>
            </Link>
          ) : null}
        </div>
      </ExItsContainer>
    </section>
  );
}
