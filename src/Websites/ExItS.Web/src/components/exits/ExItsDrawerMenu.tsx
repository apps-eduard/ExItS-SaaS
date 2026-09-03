"use client";

import * as React from "react";
import Link from "next/link";

import { Sheet, SheetClose, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import { cn } from "@/lib/utils";

import { ExItsBadge } from "./ExItsBadge";

export type ExItsDrawerMenuProps = {
  trigger: React.ReactElement;
};

export function ExItsDrawerMenu({ trigger }: ExItsDrawerMenuProps) {
  return (
    <Sheet>
      <SheetTrigger asChild>{trigger}</SheetTrigger>
      <SheetContent
        className={cn(
          // Width behavior (from docs/02-information-architecture.md):
          // - desktop (>=1024): ~32-36vw
          // - tablet (640-1023): ~45-55vw
          // - mobile (<640): 100vw
          "w-full sm:w-[55vw] lg:w-[34vw] max-w-none sm:max-w-none lg:max-w-none p-0",
        )}
      >
        {/* Keep the close icon from overlapping the content. */}
        <div className="flex h-full flex-col overflow-hidden">
          <div className="h-full overflow-y-auto bg-surface p-6 pt-14">
            <nav aria-label="Site navigation" className="space-y-10">
              <div>
                <h3 className="text-sm font-semibold text-primary">Products</h3>
                <ul className="mt-3 space-y-2">
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/pos"
                        className="flex min-h-11 items-center justify-between rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        <span>Pinoy Business POS</span>
                        <ExItsBadge variant="available" className="shrink-0">
                          Available
                        </ExItsBadge>
                      </Link>
                    </SheetClose>
                  </li>
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/service-pro"
                        className="flex min-h-11 items-center justify-between rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        <span>Pinoy Service Pro</span>
                        <ExItsBadge variant="coming-soon" className="shrink-0">
                          Coming Soon
                        </ExItsBadge>
                      </Link>
                    </SheetClose>
                  </li>
                </ul>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-primary">Solutions</h3>
                <ul className="mt-3 space-y-2">
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/pos#personal-sellers"
                        className="flex min-h-11 items-center rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        Personal Sellers
                      </Link>
                    </SheetClose>
                  </li>
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/pos#small-businesses"
                        className="flex min-h-11 items-center rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        Small Businesses
                      </Link>
                    </SheetClose>
                  </li>
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/pos#multi-branch-businesses"
                        className="flex min-h-11 items-center rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        Multi-Branch Businesses
                      </Link>
                    </SheetClose>
                  </li>
                </ul>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-primary">Pricing</h3>
                <ul className="mt-3 space-y-2">
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/pricing"
                        className="flex min-h-11 items-center rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        Pricing
                      </Link>
                    </SheetClose>
                  </li>
                </ul>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-primary">Company</h3>
                <ul className="mt-3 space-y-2">
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/about"
                        className="flex min-h-11 items-center rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        About
                      </Link>
                    </SheetClose>
                  </li>
                  <li>
                    <SheetClose asChild>
                      <Link
                        href="/contact"
                        className="flex min-h-11 items-center rounded-md px-2 py-2 text-sm text-muted hover:text-primary focus-visible:outline-none"
                      >
                        Contact
                      </Link>
                    </SheetClose>
                  </li>
                </ul>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-primary">Resources</h3>
                <p className="mt-3 rounded-md px-2 py-2 text-sm text-muted">
                  TBD — blog, guides, and documentation.
                </p>
              </div>
            </nav>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
}

