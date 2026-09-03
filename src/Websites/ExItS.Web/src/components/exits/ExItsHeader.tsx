"use client";

import * as React from "react";
import Link from "next/link";
import { Menu } from "lucide-react";

import { cn } from "@/lib/utils";

import { ExItsContainer } from "./ExItsContainer";
import { ExItsDrawerMenu } from "./ExItsDrawerMenu";

export type ExItsHeaderProps = {
  transparent?: boolean;
};

export function ExItsHeader({ transparent = false }: ExItsHeaderProps) {
  const [scrolled, setScrolled] = React.useState(false);

  React.useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 0);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  const backgroundClassName = scrolled
    ? "bg-elevated backdrop-blur"
    : transparent
      ? "bg-transparent"
      : "bg-base backdrop-blur";

  const borderClassName = scrolled ? "border-borderDefault" : "border-transparent";

  return (
    <header
      className={cn(
        "sticky top-0 z-50 border-b transition-colors",
        backgroundClassName,
        borderClassName,
      )}
    >
      <ExItsContainer>
        <div className="flex min-h-16 items-center justify-between py-3">
          <Link href="/" className="flex items-center gap-2 text-primary">
            <span className="text-lg font-bold tracking-tight">ExItS</span>
            <span className="h-2 w-2 rounded-full bg-brand" aria-hidden="true" />
          </Link>

          <div className="flex items-center gap-3">
            <Link
              href="/contact"
              className={cn(
                "hidden h-11 items-center justify-center gap-2 whitespace-nowrap rounded-md border px-5",
                "bg-gradient-to-r from-brand to-brandBright text-primary border-borderDefault hover:brightness-110",
                "text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright",
                "ring-offset-base",
                "sm:inline-flex",
              )}
            >
              Get Started
            </Link>

            <ExItsDrawerMenu
              trigger={
                <button
                  type="button"
                  aria-label="Open menu"
                  className={cn(
                    "inline-flex h-11 w-11 items-center justify-center rounded-md border border-borderDefault text-muted transition-colors",
                    "bg-transparent hover:bg-surface",
                    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright",
                  )}
                >
                  <Menu className="h-5 w-5" aria-hidden="true" />
                </button>
              }
            />
          </div>
        </div>
      </ExItsContainer>
    </header>
  );
}

