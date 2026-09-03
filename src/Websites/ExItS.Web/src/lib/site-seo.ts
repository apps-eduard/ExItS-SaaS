import type { Metadata } from "next";

export const SITE_URL = "https://exits.ph";
export const SITE_NAME = "ExItS";

export type SiteRoutePath =
  | "/"
  | "/products"
  | "/pos"
  | "/service-pro"
  | "/pricing"
  | "/about"
  | "/contact"
  | "/privacy"
  | "/terms";

export type SiteRouteSeo = {
  path: SiteRoutePath;
  /** Absolute document title (already includes brand where needed). */
  title: string;
  /** Use Next title template (`%s | ExItS`) when true. */
  useTitleTemplate: boolean;
  description: string;
  ogImage: string;
  ogImageAlt: string;
  changeFrequency: "always" | "hourly" | "daily" | "weekly" | "monthly" | "yearly" | "never";
  priority: number;
};

/**
 * Authoritative public marketing routes for sitemap + metadata.
 * Do not add WEB-D-05 TBD product routes here.
 */
export const siteRoutes: readonly SiteRouteSeo[] = [
  {
    path: "/",
    title: "ExItS — Business Management Platform for Filipino Businesses",
    useTitleTemplate: false,
    description:
      "ExItS is a multi-product SaaS platform built for Filipino businesses. Manage sales, inventory, staff, and customers — all in one place.",
    ogImage: "/og/exits-og-home.png",
    ogImageAlt: "ExItS — Business Management Platform for Filipino Businesses",
    changeFrequency: "weekly",
    priority: 1,
  },
  {
    path: "/products",
    title: "ExItS Products",
    useTitleTemplate: true,
    description:
      "Discover all ExItS products for Filipino businesses — Pinoy Business POS and more coming soon.",
    ogImage: "/og/products-og.png",
    ogImageAlt: "ExItS Products",
    changeFrequency: "weekly",
    priority: 0.9,
  },
  {
    path: "/pos",
    title: "Pinoy Business POS — Point of Sale for Filipino Businesses",
    useTitleTemplate: true,
    description:
      "Pinoy Business POS by ExItS — point-of-sale, inventory, customer credit (Utang), supplier ordering, and multi-branch management for Filipino retailers.",
    ogImage: "/og/exits-og-pos.png",
    ogImageAlt: "Pinoy Business POS by ExItS",
    changeFrequency: "weekly",
    priority: 0.9,
  },
  {
    path: "/service-pro",
    title: "Pinoy Service Pro — Coming Soon",
    useTitleTemplate: true,
    description:
      "Pinoy Service Pro by ExItS — coming soon. Service business management designed for Filipino service organizations.",
    ogImage: "/og/service-pro-og.png",
    ogImageAlt: "Pinoy Service Pro — Coming Soon",
    changeFrequency: "monthly",
    priority: 0.7,
  },
  {
    path: "/pricing",
    title: "Pricing",
    useTitleTemplate: true,
    description:
      "ExItS pricing plans — find the right plan for your business. Commercial amounts are being finalized.",
    ogImage: "/og/pricing-og.png",
    ogImageAlt: "ExItS Pricing",
    changeFrequency: "weekly",
    priority: 0.8,
  },
  {
    path: "/about",
    title: "About ExItS",
    useTitleTemplate: true,
    description:
      "About ExItS — the mission and platform behind Pinoy Business POS and a growing suite of business tools for Filipino businesses.",
    ogImage: "/og/about-og.png",
    ogImageAlt: "About ExItS",
    changeFrequency: "monthly",
    priority: 0.6,
  },
  {
    path: "/contact",
    title: "Contact ExItS",
    useTitleTemplate: true,
    description:
      "Contact ExItS — sales inquiries, partnerships, and support.",
    ogImage: "/og/contact-og.png",
    ogImageAlt: "Contact ExItS",
    changeFrequency: "monthly",
    priority: 0.7,
  },
  {
    path: "/privacy",
    title: "Privacy Policy",
    useTitleTemplate: true,
    description:
      "ExItS privacy policy — being finalized pending legal review. Contact us with questions about your data.",
    ogImage: "/og/privacy-og.png",
    ogImageAlt: "ExItS Privacy Policy",
    changeFrequency: "yearly",
    priority: 0.2,
  },
  {
    path: "/terms",
    title: "Terms of Service",
    useTitleTemplate: true,
    description:
      "ExItS terms of service — being finalized pending legal review. Contact us if you have questions.",
    ogImage: "/og/terms-og.png",
    ogImageAlt: "ExItS Terms of Service",
    changeFrequency: "yearly",
    priority: 0.2,
  },
] as const;

export const ogImageDefinitions: Record<
  string,
  { title: string; subtitle: string }
> = {
  "exits-og-home.png": {
    title: "Business management for Filipino businesses",
    subtitle: "Pinoy Business POS — available now. Other products coming soon.",
  },
  "exits-og-pos.png": {
    title: "Pinoy Business POS",
    subtitle: "Point of sale and business management for Filipino retailers.",
  },
  "products-og.png": {
    title: "ExItS Products",
    subtitle: "Pinoy Business POS available now. More tools on the roadmap.",
  },
  "service-pro-og.png": {
    title: "Pinoy Service Pro",
    subtitle: "Coming soon — service business management for Filipino teams.",
  },
  "pricing-og.png": {
    title: "ExItS Pricing",
    subtitle: "Commercial plans are being finalized. Contact us to talk through needs.",
  },
  "about-og.png": {
    title: "About ExItS",
    subtitle: "Multi-product SaaS built for Filipino businesses.",
  },
  "contact-og.png": {
    title: "Contact ExItS",
    subtitle: "Sales, partnerships, and general questions.",
  },
  "privacy-og.png": {
    title: "Privacy Policy",
    subtitle: "Draft — pending legal review.",
  },
  "terms-og.png": {
    title: "Terms of Service",
    subtitle: "Draft — pending legal review.",
  },
  "default-og.png": {
    title: "Business management for Filipino businesses",
    subtitle: "ExItS — Pinoy Business POS and more.",
  },
};

export function getSiteRoute(path: SiteRoutePath): SiteRouteSeo {
  const route = siteRoutes.find((entry) => entry.path === path);
  if (!route) {
    throw new Error(`Unknown site route: ${path}`);
  }
  return route;
}

export function absoluteUrl(path: string): string {
  if (path.startsWith("http://") || path.startsWith("https://")) return path;
  if (path === "/") return SITE_URL;
  return `${SITE_URL}${path.startsWith("/") ? path : `/${path}`}`;
}

export function buildPageMetadata(path: SiteRoutePath): Metadata {
  const route = getSiteRoute(path);
  const canonical = absoluteUrl(route.path);
  const ogImageUrl = absoluteUrl(route.ogImage);

  return {
    title: route.useTitleTemplate
      ? route.title
      : { absolute: route.title },
    description: route.description,
    alternates: {
      canonical,
    },
    openGraph: {
      type: "website",
      siteName: SITE_NAME,
      locale: "en_PH",
      title: route.useTitleTemplate ? `${route.title} | ${SITE_NAME}` : route.title,
      description: route.description,
      url: canonical,
      images: [
        {
          url: ogImageUrl,
          width: 1200,
          height: 630,
          alt: route.ogImageAlt,
        },
      ],
    },
    twitter: {
      card: "summary_large_image",
      // twitter:site handle intentionally omitted — social handles TBD
      title: route.useTitleTemplate ? `${route.title} | ${SITE_NAME}` : route.title,
      description: route.description,
      images: [ogImageUrl],
    },
  };
}

export function faqPageJsonLd(
  items: readonly { question: string; answer: string }[],
) {
  return {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: items.map((item) => ({
      "@type": "Question",
      name: item.question,
      acceptedAnswer: {
        "@type": "Answer",
        text: item.answer,
      },
    })),
  };
}
