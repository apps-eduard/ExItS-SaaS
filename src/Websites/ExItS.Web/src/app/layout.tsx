import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";

import { ExItsBreadcrumbs } from "@/components/exits/ExItsBreadcrumbs";
import { ExItsFooter } from "@/components/exits/ExItsFooter";
import { ExItsHeader } from "@/components/exits/ExItsHeader";

export const metadata: Metadata = {
  title: {
    default: "ExItS",
    template: "%s | ExItS",
  },
  description: "ExItS — Business Management Platform for Filipino Businesses",
  metadataBase: new URL("https://exits.ph"),
  alternates: {
    canonical: "https://exits.ph/",
  },
  openGraph: {
    type: "website",
    siteName: "ExItS",
    locale: "en_PH",
    title: "ExItS",
    description:
      "ExItS is a multi-product platform for Filipino businesses. Built to help you sell, manage, and grow.",
    url: "https://exits.ph/",
    images: [{ url: "https://exits.ph/og/default-og.png", width: 1200, height: 630, alt: "ExItS" }],
  },
  twitter: {
    card: "summary_large_image",
    title: "ExItS",
    description:
      "ExItS is a multi-product platform for Filipino businesses. Built to help you sell, manage, and grow.",
  },
  icons: {
    icon: "/icon.svg",
    shortcut: "/favicon.svg",
  },
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en-PH" className="h-full">
      <body className="flex min-h-[100dvh] flex-col bg-base text-primary antialiased">
        <ExItsHeader />
        <ExItsBreadcrumbs />
        <div className="flex-1">{children}</div>
        <ExItsFooter />
      </body>
    </html>
  );
}
