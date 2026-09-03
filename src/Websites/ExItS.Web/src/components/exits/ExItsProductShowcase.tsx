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
  id,
}: {
  label?: string;
  headline: string;
  body: string;
  benefits: string[];
  cta?: { href: string; label: string };
  visual?: ReactNode;
  reversed?: boolean;
  id?: string;
}) {
  return (
    <ExItsSection id={id} className="border-b border-borderDefault py-20 lg:py-28">
      <ExItsContainer>
        <div
          className={cn(
            "grid items-center gap-12",
            visual ? "lg:grid-cols-2" : "lg:grid-cols-1",
            reversed && visual && "lg:[&>*:first-child]:order-2",
          )}
        >
          <div>
            {label ? (
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-brandBright">
                {label}
              </p>
            ) : null}
            <h2
              className={cn(
                "text-3xl font-semibold tracking-tight text-primary lg:text-4xl",
                label && "mt-3",
              )}
            >
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
            {cta ? (
              <Link href={cta.href} className={cn(ctaClassName("primary"), "mt-8")}>
                {cta.label}
              </Link>
            ) : null}
          </div>
          {visual ? <div>{visual}</div> : null}
        </div>
      </ExItsContainer>
    </ExItsSection>
  );
}
