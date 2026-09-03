"use client";

import * as React from "react";
import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { useReducedMotion } from "framer-motion";

import { Sheet, SheetClose, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import { MotionDiv } from "@/lib/motion";

import { ExItsBadge } from "./ExItsBadge";

export type ExItsDrawerMenuProps = {
  trigger: React.ReactElement;
};

type NavLink = {
  href: string;
  label: string;
  badge?: "available" | "coming-soon" | "in-development";
  badgeLabel?: string;
  arrow?: boolean;
};

type NavGroup = {
  title: string;
  links?: NavLink[];
  note?: string;
};

const groups: NavGroup[] = [
  {
    title: "Products",
    links: [
      {
        href: "/pos",
        label: "Pinoy Business POS",
        badge: "available",
        badgeLabel: "Available",
      },
      {
        href: "/service-pro",
        label: "Pinoy Service Pro",
        badge: "coming-soon",
        badgeLabel: "Coming Soon",
      },
    ],
  },
  {
    title: "Solutions",
    links: [
      { href: "/pos#personal-sellers", label: "Personal Sellers", arrow: true },
      { href: "/pos#small-businesses", label: "Small Businesses", arrow: true },
      {
        href: "/pos#multi-branch-businesses",
        label: "Multi-Branch Businesses",
        arrow: true,
      },
    ],
  },
  {
    title: "Pricing",
    links: [{ href: "/pricing", label: "Pricing", arrow: true }],
  },
  {
    title: "Company",
    links: [
      { href: "/about", label: "About", arrow: true },
      { href: "/contact", label: "Contact", arrow: true },
    ],
  },
  {
    title: "Resources",
    note: "Coming later",
  },
];

export function ExItsDrawerMenu({ trigger }: ExItsDrawerMenuProps) {
  const reducedMotion = useReducedMotion();

  return (
    <Sheet>
      <SheetTrigger asChild>{trigger}</SheetTrigger>
      <SheetContent
        className={cn(
          "w-full sm:w-[55vw] lg:w-[34vw] max-w-none sm:max-w-none lg:max-w-none p-0",
        )}
      >
        <div className="flex h-full flex-col overflow-hidden">
          <div className="h-full overflow-y-auto p-6 pt-16">
            <nav aria-label="Site navigation" className="space-y-9">
              {groups.map((group, groupIndex) => (
                <MotionDiv
                  key={group.title}
                  initial={reducedMotion ? false : { opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.05 + groupIndex * 0.06, duration: 0.35 }}
                >
                  <h3 className="text-xs font-semibold uppercase tracking-[0.22em] text-brandBright/90">
                    {group.title}
                  </h3>
                  {group.note ? (
                    <p className="mt-3 rounded-2xl border border-borderDefault/70 bg-elevated/40 px-3 py-3 text-sm text-muted">
                      {group.note}
                    </p>
                  ) : null}
                  {group.links ? (
                    <ul className="mt-3 space-y-1.5">
                      {group.links.map((link, linkIndex) => (
                        <li key={link.href + link.label}>
                          <MotionDiv
                            initial={reducedMotion ? false : { opacity: 0, x: 10 }}
                            animate={{ opacity: 1, x: 0 }}
                            transition={{
                              delay: 0.08 + groupIndex * 0.06 + linkIndex * 0.04,
                              duration: 0.3,
                            }}
                          >
                            <SheetClose asChild>
                              <Link
                                href={link.href}
                                className="group flex min-h-12 items-center justify-between gap-3 rounded-2xl border border-transparent px-3 py-2 text-sm text-muted transition-all hover:border-borderDefault hover:bg-gradient-to-r hover:from-brand/15 hover:to-magenta/10 hover:text-primary focus-visible:outline-none"
                              >
                                <span className="font-medium">{link.label}</span>
                                <span className="flex items-center gap-2">
                                  {link.badge && link.badgeLabel ? (
                                    <ExItsBadge variant={link.badge} className="shrink-0">
                                      {link.badgeLabel}
                                    </ExItsBadge>
                                  ) : null}
                                  {link.arrow ? (
                                    <ArrowRight
                                      className="h-4 w-4 text-brandBright transition-transform duration-300 group-hover:translate-x-1"
                                      aria-hidden="true"
                                    />
                                  ) : null}
                                </span>
                              </Link>
                            </SheetClose>
                          </MotionDiv>
                        </li>
                      ))}
                    </ul>
                  ) : null}
                  <div className="mt-6 h-px bg-gradient-to-r from-brand/40 via-magenta/30 to-transparent" />
                </MotionDiv>
              ))}
            </nav>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
}
