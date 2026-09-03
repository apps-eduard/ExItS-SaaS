import Link from "next/link";

import { ctaClassName } from "@/lib/cta";
import { ExItsContainer } from "@/components/exits/ExItsContainer";

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
    <section className="border-t border-borderDefault bg-elevated">
      <ExItsContainer className="py-20 text-center lg:py-24">
        <h2 className="text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
          {headline}
        </h2>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <Link href={primaryCta.href} className={ctaClassName("primary")}>
            {primaryCta.label}
          </Link>
          {secondaryCta ? (
            <Link href={secondaryCta.href} className={ctaClassName("secondary")}>
              {secondaryCta.label}
            </Link>
          ) : null}
        </div>
      </ExItsContainer>
    </section>
  );
}
