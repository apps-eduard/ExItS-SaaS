import Link from "next/link";

import { ExItsBadge, type ExItsBadgeVariant } from "@/components/exits/ExItsBadge";
import { ctaClassName } from "@/lib/cta";
import { cn } from "@/lib/utils";

export type ExItsProductCardProps = {
  id?: string;
  name: string;
  description: string;
  badge: ExItsBadgeVariant;
  badgeLabel: string;
  cta: {
    label: string;
    href?: string;
  };
  featured?: boolean;
};

export function ExItsProductCard({
  id,
  name,
  description,
  badge,
  badgeLabel,
  cta,
  featured = false,
}: ExItsProductCardProps) {
  return (
    <article
      id={id}
      className={cn(
        "exits-gradient-border flex h-full scroll-mt-28 flex-col transition-transform duration-300 hover:-translate-y-1",
      )}
      data-featured={featured ? "true" : "false"}
      data-animated={featured ? "true" : "false"}
    >
      <div className="exits-gradient-border__inner flex h-full flex-col p-6">
        <div className="flex flex-wrap items-center gap-3">
          <h2 className="text-xl font-semibold tracking-tight text-primary">{name}</h2>
          <ExItsBadge variant={badge}>{badgeLabel}</ExItsBadge>
        </div>
        <p className="mt-4 flex-1 text-sm leading-relaxed text-muted">{description}</p>
        {cta.href ? (
          <Link
            href={cta.href}
            className={cn(
              ctaClassName(featured ? "primary" : "secondary"),
              "mt-6 w-full sm:w-auto",
            )}
          >
            {cta.label}
          </Link>
        ) : (
          <p className="mt-6 text-sm text-muted">{cta.label}</p>
        )}
      </div>
    </article>
  );
}
