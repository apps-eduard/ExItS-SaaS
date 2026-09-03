"use client";

import * as React from "react";
import Link from "next/link";
import { Sparkles } from "lucide-react";

import { cn } from "@/lib/utils";
import { ctaClassName, headerCtaActions } from "@/lib/cta";

import { ExItsContainer } from "./ExItsContainer";
import { ExItsDrawerMenu } from "./ExItsDrawerMenu";

export type ExItsHeaderProps = {
  transparent?: boolean;
};

export function ExItsHeader({ transparent = false }: ExItsHeaderProps) {
  const [scrolled, setScrolled] = React.useState(false);
  const enabledCtas = headerCtaActions.filter((action) => action.enabled);

  React.useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      className={cn(
        "sticky top-0 z-50 shrink-0 transition-all duration-300 print:hidden",
        scrolled
          ? "border-b border-borderDefault/70 bg-night/80 shadow-[0_12px_40px_-20px_rgba(88,28,135,0.7)] backdrop-blur-xl"
          : transparent
            ? "border-b border-transparent bg-transparent"
            : "border-b border-transparent bg-night/45 backdrop-blur-md",
      )}
    >
      <div
        className="pointer-events-none absolute inset-x-0 bottom-0 h-px bg-gradient-to-r from-transparent via-magenta/50 to-transparent"
        aria-hidden="true"
      />
      <ExItsContainer>
        <div className="flex min-h-[4.25rem] items-center justify-between py-3">
          <Link href="/" className="group flex items-center gap-2.5 text-primary">
            <span className="text-xl font-bold tracking-tight transition-colors group-hover:text-brandBright">
              ExItS
            </span>
            <span
              className="exits-glow-breathe h-2.5 w-2.5 rounded-full bg-gradient-to-br from-brand to-magenta shadow-[0_0_14px_rgba(217,70,239,0.9)]"
              aria-hidden="true"
            />
          </Link>

          <div className="flex items-center gap-3">
            {enabledCtas.map((action) => (
              <Link
                key={action.id}
                href={action.href}
                className={ctaClassName(action.variant ?? "primary", "h-12 px-5 sm:px-6")}
              >
                {action.id === "get-started" ? (
                  <Sparkles className="h-4 w-4 opacity-90" aria-hidden="true" />
                ) : null}
                {action.label}
                <span
                  aria-hidden="true"
                  className="transition-transform duration-300 group-hover:translate-x-0.5"
                >
                  →
                </span>
              </Link>
            ))}

            <ExItsDrawerMenu
              trigger={
                <button
                  type="button"
                  aria-label="Open menu"
                  className={ctaClassName("menu", "h-12 gap-2.5 px-4 sm:px-5")}
                >
                  <span className="text-xs font-semibold uppercase tracking-[0.18em]">Menu</span>
                  <span className="flex flex-col gap-1" aria-hidden="true">
                    <span className="h-0.5 w-4 rounded-full bg-current" />
                    <span className="h-0.5 w-4 rounded-full bg-current" />
                    <span className="h-0.5 w-3 rounded-full bg-current" />
                  </span>
                </button>
              }
            />
          </div>
        </div>
      </ExItsContainer>
    </header>
  );
}
