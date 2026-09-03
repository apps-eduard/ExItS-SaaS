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

type Crumb = {
  label: string;
  href?: string;
};

function crumbsForPath(pathname: string): Crumb[] | null {
  const segments = pathname.split("/").filter(Boolean);
  const first = segments[0] ?? "";
  if (!allowedSegments.has(first)) return null;

  if (first === "pos") {
    return [
      { label: "Products", href: "/products" },
      { label: "Pinoy Business POS" },
    ];
  }

  return [{ label: labelBySegment[first] ?? first }];
}

export function ExItsBreadcrumbs() {
  const pathname = usePathname();
  const crumbs = React.useMemo(() => crumbsForPath(pathname), [pathname]);

  if (!crumbs) return null;

  return (
    <div>
      <ExItsContainer className="py-6">
        <nav aria-label="Breadcrumb">
          <ol className="flex flex-wrap items-center gap-2 text-sm text-muted">
            <li>
              <Link href="/" className="hover:text-primary">
                Home
              </Link>
            </li>
            {crumbs.map((crumb) => (
              <React.Fragment key={crumb.label}>
                <li aria-hidden="true">/</li>
                <li
                  aria-current={crumb.href ? undefined : "page"}
                  className={crumb.href ? undefined : "text-primary"}
                >
                  {crumb.href ? (
                    <Link href={crumb.href} className="hover:text-primary">
                      {crumb.label}
                    </Link>
                  ) : (
                    crumb.label
                  )}
                </li>
              </React.Fragment>
            ))}
          </ol>
        </nav>
      </ExItsContainer>
    </div>
  );
}
