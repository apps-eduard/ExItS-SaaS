import Link from "next/link";
import type { ReactNode } from "react";

import { socialLinks, type SocialNetwork } from "@/lib/social-links";
import { ExItsContainer } from "./ExItsContainer";
import { ExItsNewsletter } from "./ExItsNewsletter";

function SocialGlyph({ network }: { network: SocialNetwork }) {
  const common = {
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    className: "h-4 w-4",
    "aria-hidden": true as const,
  };

  switch (network) {
    case "facebook":
      return (
        <svg {...common}>
          <path d="M14 9h3V6h-3c-1.7 0-3 1.3-3 3v2H8v3h3v7h3v-7h2.5l.5-3H14V9z" />
        </svg>
      );
    case "instagram":
      return (
        <svg {...common}>
          <rect x="3" y="3" width="18" height="18" rx="5" />
          <circle cx="12" cy="12" r="4" />
          <circle cx="17.5" cy="6.5" r="1" fill="currentColor" stroke="none" />
        </svg>
      );
    case "linkedin":
      return (
        <svg {...common}>
          <path d="M6 9v12M6 5.5v.5M10 21V14c0-2 1-3 3-3s3 1 3 3v7M10 10v11" />
        </svg>
      );
    case "x":
      return (
        <svg {...common}>
          <path d="M4 4l16 16M20 4L4 20" />
        </svg>
      );
    case "youtube":
      return (
        <svg {...common}>
          <rect x="2" y="6" width="20" height="12" rx="3" />
          <path d="M10 9l6 3-6 3V9z" fill="currentColor" stroke="none" />
        </svg>
      );
    case "tiktok":
      return (
        <svg {...common}>
          <path d="M14 4v10a4 4 0 11-4-4" />
          <path d="M14 4c1 3 3 4 6 4" />
        </svg>
      );
    default:
      return null;
  }
}

function FooterLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link
      href={href}
      className="group relative inline-flex text-muted transition-colors hover:text-brandBright"
    >
      {children}
      <span className="absolute inset-x-0 -bottom-0.5 h-px origin-left scale-x-0 bg-gradient-to-r from-brand via-magenta to-secondary transition-transform duration-300 group-hover:scale-x-100" />
    </Link>
  );
}

export function ExItsFooter() {
  const year = new Date().getFullYear();
  const verifiedSocial = socialLinks.filter((link) => Boolean(link.href));

  return (
    <footer className="relative z-20 shrink-0 overflow-hidden bg-base print:hidden">
      <div className="h-px w-full bg-exits-footer-line" aria-hidden="true" />
      <div
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top,rgba(139,92,246,0.2),transparent_45%),radial-gradient(ellipse_at_bottom_right,rgba(217,70,239,0.12),transparent_40%)]"
        aria-hidden="true"
      />

      <ExItsContainer className="relative z-10 py-14">
        <div className="relative mb-12 overflow-hidden rounded-3xl border border-borderDefault bg-gradient-to-br from-raised/80 via-purpleDeep/70 to-night/90 p-6 sm:p-8">
          <h2 className="text-2xl font-semibold tracking-tight text-primary">Stay close to ExItS</h2>
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
            Product updates and launch announcements. Submission is not connected until the waitlist
            endpoint is ready.
          </p>
          <div className="mt-5 max-w-lg">
            <ExItsNewsletter />
          </div>
        </div>

        <p
          className="pointer-events-none mb-10 select-none text-6xl font-semibold tracking-tight text-primary/[0.07] sm:text-7xl lg:text-8xl"
          aria-hidden="true"
        >
          ExItS
        </p>

        <div className="grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Products</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <FooterLink href="/pos">Pinoy Business POS</FooterLink>
              </li>
              <li>
                <FooterLink href="/products">All products</FooterLink>
              </li>
              <li>
                <FooterLink href="/service-pro">Pinoy Service Pro</FooterLink>
              </li>
            </ul>
          </div>

          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Solutions</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <FooterLink href="/pos#personal-sellers">Personal Sellers</FooterLink>
              </li>
              <li>
                <FooterLink href="/pos#small-businesses">Small Businesses</FooterLink>
              </li>
              <li>
                <FooterLink href="/pos#multi-branch-businesses">Multi-Branch Businesses</FooterLink>
              </li>
            </ul>
          </div>

          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Company</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <FooterLink href="/about">About</FooterLink>
              </li>
              <li>
                <FooterLink href="/contact">Contact</FooterLink>
              </li>
            </ul>
          </div>

          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Legal</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <FooterLink href="/privacy">Privacy</FooterLink>
              </li>
              <li>
                <FooterLink href="/terms">Terms</FooterLink>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-12 flex flex-col gap-4 border-t border-borderDefault/70 pt-8 sm:flex-row sm:items-center sm:justify-between">
          {verifiedSocial.length > 0 ? (
            <ul className="flex flex-wrap gap-3">
              {verifiedSocial.map((link) => (
                <li key={link.network}>
                  <a
                    href={link.href}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={link.label}
                    className="inline-flex h-10 w-10 items-center justify-center rounded-pill border border-borderDefault bg-elevated/60 text-muted transition-all hover:border-borderActive hover:text-brandBright hover:shadow-glow"
                  >
                    <SocialGlyph network={link.network} />
                  </a>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-muted">Social profiles will appear here when confirmed.</p>
          )}
          <div className="text-sm text-muted">© {year} ExItS. All rights reserved.</div>
        </div>
      </ExItsContainer>
    </footer>
  );
}
