import type { ReactNode } from "react";
import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsSection } from "@/components/exits/ExItsSection";
import { ctaClassName } from "@/lib/cta";

export type ExItsLegalDocumentProps = {
  title: string;
  description: string;
  lastUpdatedLabel: string;
  children: ReactNode;
};

export function ExItsLegalDocument({
  title,
  description,
  lastUpdatedLabel,
  children,
}: ExItsLegalDocumentProps) {
  return (
    <main className="legal-document">
      <ExItsSection className="border-b border-borderDefault py-16 lg:py-20 print:border-0 print:py-8">
        <ExItsContainer className="max-w-3xl">
          <article className="legal-article">
            <header className="space-y-4">
              <Badge variant="comingSoon">Draft — pending legal review</Badge>
              <h1 className="text-4xl font-semibold tracking-tight text-primary sm:text-5xl">
                {title}
              </h1>
              <p className="text-base leading-relaxed text-muted sm:text-lg">{description}</p>
              <p className="text-sm text-muted">
                <span className="font-medium text-primary">Last updated:</span>{" "}
                {lastUpdatedLabel}
              </p>
            </header>

            <div
              role="status"
              className="mt-8 rounded-xl border border-amber-400/30 bg-amber-400/10 px-5 py-4 text-sm leading-relaxed text-primary print:border-neutral-400 print:bg-transparent"
            >
              Legal review is not complete. The content below is a temporary notice and planned
              section outline only — not ExItS’s final legal documents.
            </div>

            <div className="legal-prose mt-10">{children}</div>

            <div className="mt-12 border-t border-borderDefault pt-8 print:hidden">
              <p className="text-sm text-muted">
                Questions about this page? Reach us through the contact form.
              </p>
              <Link href="/contact" className={ctaClassName("secondary", "mt-4")}>
                Contact ExItS
              </Link>
            </div>
          </article>
        </ExItsContainer>
      </ExItsSection>
    </main>
  );
}
