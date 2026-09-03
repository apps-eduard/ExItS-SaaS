import Link from "next/link";
import type { ReactNode } from "react";

import { ctaClassName } from "@/lib/cta";
import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { cn } from "@/lib/utils";

export function ExItsProductShowcase({
  label,
  headline,
  body,
  benefits,
  cta,
  visual,
  reversed = false,
}: {
  label: string;
  headline: string;
  body: string;
  benefits: string[];
  cta: { href: string; label: string };
  visual: ReactNode;
  reversed?: boolean;
}) {
  return (
    <ExItsSection className="border-b border-borderDefault py-20 lg:py-28">
      <ExItsContainer>
        <div
          className={cn(
            "grid items-center gap-12 lg:grid-cols-2",
            reversed && "lg:[&>*:first-child]:order-2",
          )}
        >
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-brandBright">
              {label}
            </p>
            <h2 className="mt-3 text-3xl font-semibold tracking-tight text-primary lg:text-4xl">
              {headline}
            </h2>
            <p className="mt-4 max-w-xl text-base leading-relaxed text-muted">{body}</p>
            <ul className="mt-6 space-y-3">
              {benefits.map((benefit) => (
                <li key={benefit} className="flex gap-3 text-sm leading-relaxed text-primary">
                  <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-brand" aria-hidden="true" />
                  <span>{benefit}</span>
                </li>
              ))}
            </ul>
            <Link href={cta.href} className={cn(ctaClassName("primary"), "mt-8")}>
              {cta.label}
            </Link>
          </div>
          <div>{visual}</div>
        </div>
      </ExItsContainer>
    </ExItsSection>
  );
}
