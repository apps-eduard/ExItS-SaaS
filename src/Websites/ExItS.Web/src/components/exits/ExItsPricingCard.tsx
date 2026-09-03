import Link from "next/link";
import { Check } from "lucide-react";

import { ctaClassName } from "@/lib/cta";
import { cn } from "@/lib/utils";

export type ExItsPricingCardProps = {
  planName: string;
  price: string;
  priceNote?: string;
  features: string[];
  recommended?: boolean;
  cta: { href: string; label: string };
  className?: string;
};

export function ExItsPricingCard({
  planName,
  price,
  priceNote,
  features,
  recommended = false,
  cta,
  className,
}: ExItsPricingCardProps) {
  return (
    <article
      className={cn(
        "exits-gradient-border relative flex h-full flex-col transition-transform duration-300 hover:-translate-y-1",
        recommended && "lg:scale-[1.03]",
        className,
      )}
      data-featured={recommended ? "true" : "false"}
      data-animated={recommended ? "true" : "false"}
    >
      <div
        className={cn(
          "exits-gradient-border__inner flex h-full flex-col p-6 pt-8",
          recommended
            ? "bg-[radial-gradient(ellipse_at_top,rgba(217,70,239,0.28),transparent_55%),linear-gradient(180deg,#21114b,#130b2b)]"
            : "bg-[radial-gradient(ellipse_at_top_right,rgba(99,102,241,0.18),transparent_50%),var(--color-surface)]",
        )}
      >
        {recommended ? (
          <p className="absolute -top-3 left-6 rounded-pill border border-magenta/50 bg-gradient-to-r from-brand to-magenta px-3 py-1 text-xs font-semibold text-white shadow-cta">
            Recommended
          </p>
        ) : null}
        <h3 className="text-xl font-semibold tracking-tight text-primary">{planName}</h3>
        <p className="mt-4 text-3xl font-semibold tracking-tight text-primary">{price}</p>
        {priceNote ? (
          <p className="mt-2 text-sm leading-relaxed text-muted">{priceNote}</p>
        ) : null}
        <ul className="mt-6 flex-1 space-y-3">
          {features.map((feature) => (
            <li key={feature} className="flex gap-3 text-sm leading-relaxed text-primary">
              <span className="mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-brand/20 text-secondary">
                <Check className="h-3.5 w-3.5" aria-hidden="true" />
              </span>
              <span>{feature}</span>
            </li>
          ))}
        </ul>
        <Link
          href={cta.href}
          className={cn(ctaClassName(recommended ? "primary" : "secondary"), "mt-8 w-full")}
        >
          {cta.label}
        </Link>
      </div>
    </article>
  );
}
