import type { Metadata } from "next";

import { ExItsBreadcrumbs } from "@/components/exits/ExItsBreadcrumbs";
import { ExItsFooter } from "@/components/exits/ExItsFooter";
import { ExItsHeader } from "@/components/exits/ExItsHeader";
import { absoluteUrl } from "@/lib/site-seo";
import type { ReactNode } from "react";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "ExItS",
    template: "%s | ExItS",
  },
  description: "ExItS — Business Management Platform for Filipino Businesses",
  metadataBase: new URL("https://exits.ph"),
  openGraph: {
    type: "website",
    siteName: "ExItS",
    locale: "en_PH",
    title: "ExItS",
    description:
      "ExItS is a multi-product platform for Filipino businesses. Built to help you sell, manage, and grow.",
    url: "https://exits.ph/",
    images: [
      {
        url: absoluteUrl("/og/default-og.png"),
        width: 1200,
        height: 630,
        alt: "ExItS",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "ExItS",
    description:
      "ExItS is a multi-product platform for Filipino businesses. Built to help you sell, manage, and grow.",
    images: [absoluteUrl("/og/default-og.png")],
  },
  icons: {
    icon: "/icon.svg",
    shortcut: "/favicon.svg",
  },
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en-PH" className="min-h-full">
      <body className="flex min-h-[100dvh] flex-col bg-base text-primary antialiased">
        <a
          href="#main-content"
          className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-[100] focus:inline-flex focus:min-h-11 focus:items-center focus:rounded-lg focus:border focus:border-borderDefault focus:bg-elevated focus:px-4 focus:py-3 focus:text-sm focus:font-semibold focus:text-primary"
        >
          Skip to main content
        </a>
        <ExItsHeader />
        <ExItsBreadcrumbs />
        <div id="main-content" className="min-h-0 flex-1" tabIndex={-1}>
          {children}
        </div>
        <ExItsFooter />
      </body>
    </html>
  );
}
