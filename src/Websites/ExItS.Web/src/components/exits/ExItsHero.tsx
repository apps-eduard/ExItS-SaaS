"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { Sparkles } from "lucide-react";
import { useReducedMotion } from "framer-motion";

import { ctaClassName } from "@/lib/cta";
import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsOutlineHeading } from "@/components/exits/ExItsOutlineHeading";
import { ExItsAnimatedGradient } from "@/components/exits/ExItsAnimatedGradient";
import { MotionDiv } from "@/lib/motion";

export function ExItsHero({
  outline,
  solid,
  subHeadline,
  primaryCta,
  secondaryCta,
  visual,
  accentPhrase,
}: {
  outline: string;
  solid: string;
  subHeadline: string;
  primaryCta: { href: string; label: string };
  secondaryCta: { href: string; label: string };
  visual: ReactNode;
  /** Optional trailing phrase rendered with gradient text inside the solid line. */
  accentPhrase?: string;
}) {
  const reducedMotion = useReducedMotion();

  const step = (delay: number, node: ReactNode) =>
    reducedMotion ? (
      node
    ) : (
      <MotionDiv
        // LCP-safe: never start at opacity 0 (avoids blank/black screen if JS is slow)
        initial={{ y: 14 }}
        animate={{ y: 0 }}
        transition={{ duration: 0.45, ease: "easeOut", delay }}
      >
        {node}
      </MotionDiv>
    );

  return (
    <section className="relative overflow-hidden exits-section-fade">
      <ExItsAnimatedGradient intensity="strong" />
      <ExItsContainer className="relative z-10 grid min-h-[80vh] items-center gap-12 py-16 lg:min-h-[calc(100svh-4.5rem)] lg:grid-cols-2 lg:py-24">
        <div>
          {step(
            0.04,
            <p className="mb-4 text-xs font-semibold uppercase tracking-[0.22em] text-brandBright">
              ExItS Platform
            </p>,
          )}
          {step(
            0.1,
            <ExItsOutlineHeading outline={outline} solid={solid} accentPhrase={accentPhrase} />,
          )}
          {step(
            0.18,
            <p className="mt-6 max-w-xl text-base leading-relaxed text-muted sm:text-lg">
              {subHeadline}
            </p>,
          )}
          {step(
            0.26,
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Link href={primaryCta.href} className={ctaClassName("primary")}>
                <Sparkles className="h-4 w-4 opacity-90" aria-hidden="true" />
                {primaryCta.label}
                <span
                  aria-hidden="true"
                  className="transition-transform duration-300 group-hover:translate-x-1"
                >
                  →
                </span>
              </Link>
              <Link href={secondaryCta.href} className={ctaClassName("secondary")}>
                {secondaryCta.label}
                <span
                  aria-hidden="true"
                  className="transition-transform duration-300 group-hover/cta:translate-x-1"
                >
                  →
                </span>
              </Link>
            </div>,
          )}
        </div>
        {step(0.32, <div className="lg:justify-self-end lg:w-full">{visual}</div>)}
      </ExItsContainer>
    </section>
  );
}
