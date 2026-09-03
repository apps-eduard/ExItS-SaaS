import { describe, expect, it } from "vitest";

import {
  absoluteUrl,
  buildPageMetadata,
  faqPageJsonLd,
  ogImageDefinitions,
  siteRoutes,
} from "./site-seo";

describe("site-seo", () => {
  it("lists only existing public routes and keeps privacy/terms low priority", () => {
    const paths = siteRoutes.map((route) => route.path);
    expect(paths).toEqual([
      "/",
      "/products",
      "/pos",
      "/service-pro",
      "/pricing",
      "/about",
      "/contact",
      "/privacy",
      "/terms",
    ]);
    expect(paths).not.toContain("/loan-manager");
    expect(paths).not.toContain("/bnpl");
    expect(paths).not.toContain("/pawn-manager");

    const privacy = siteRoutes.find((route) => route.path === "/privacy");
    const terms = siteRoutes.find((route) => route.path === "/terms");
    expect(privacy?.priority).toBeLessThanOrEqual(0.2);
    expect(terms?.priority).toBeLessThanOrEqual(0.2);
  });

  it("builds canonical metadata and OG image URLs for every route", () => {
    for (const route of siteRoutes) {
      const metadata = buildPageMetadata(route.path);
      expect(metadata.alternates?.canonical).toBe(absoluteUrl(route.path));
      expect(metadata.openGraph?.images).toEqual([
        {
          url: absoluteUrl(route.ogImage),
          width: 1200,
          height: 630,
          alt: route.ogImageAlt,
        },
      ]);
      const ogFile = route.ogImage.replace("/og/", "");
      expect(ogImageDefinitions[ogFile]).toBeDefined();
    }
  });

  it("builds FAQPage JSON-LD from real Q&A only", () => {
    const jsonLd = faqPageJsonLd([
      { question: "What is ExItS?", answer: "A multi-product SaaS platform." },
    ]);
    expect(jsonLd).toEqual({
      "@context": "https://schema.org",
      "@type": "FAQPage",
      mainEntity: [
        {
          "@type": "Question",
          name: "What is ExItS?",
          acceptedAnswer: {
            "@type": "Answer",
            text: "A multi-product SaaS platform.",
          },
        },
      ],
    });
  });
});
