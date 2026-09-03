import Link from "next/link";

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
        "relative flex h-full flex-col rounded-xl border bg-surface p-6",
        recommended
          ? "border-brand/50 bg-elevated pt-8 ring-1 ring-brand/30 lg:scale-[1.02]"
          : "border-borderDefault",
        className,
      )}
    >
      {recommended ? (
        <p className="absolute -top-3 left-6 rounded-full border border-brand/40 bg-base px-3 py-1 text-xs font-semibold text-brandBright">
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
            <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-brand" aria-hidden="true" />
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
    </article>
  );
}
