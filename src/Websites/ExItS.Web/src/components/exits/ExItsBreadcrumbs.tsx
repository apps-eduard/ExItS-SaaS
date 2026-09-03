"use client";

import * as React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";

import { ExItsContainer } from "./ExItsContainer";

const allowedSegments = new Set([
  "products",
  "pos",
  "service-pro",
  "pricing",
  "about",
  "contact",
]);

const labelBySegment: Record<string, string> = {
  products: "Products",
  pos: "Pinoy Business POS",
  "service-pro": "Pinoy Service Pro",
  pricing: "Pricing",
  about: "About",
  contact: "Contact",
};

export function ExItsBreadcrumbs() {
  const pathname = usePathname();

  const segments = React.useMemo(
    () => pathname.split("/").filter(Boolean),
    [pathname],
  );

  const first = segments[0] ?? "";

  // Per IA: breadcrumbs are for secondary pages only.
  if (!allowedSegments.has(first)) return null;

  const currentLabel = labelBySegment[first] ?? first;

  return (
    <div>
      <ExItsContainer className="py-6">
        <nav aria-label="Breadcrumb">
          <ol className="flex items-center gap-2 text-sm text-muted">
            <li>
              <Link href="/" className="hover:text-primary">
                Home
              </Link>
            </li>
            <li aria-hidden="true">/</li>
            <li aria-current="page" className="text-primary">
              {currentLabel}
            </li>
          </ol>
        </nav>
      </ExItsContainer>
    </div>
  );
}

