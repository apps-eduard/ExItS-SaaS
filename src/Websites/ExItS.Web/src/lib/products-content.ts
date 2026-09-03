import type { ExItsProductCardProps } from "@/components/exits/ExItsProductCard";

export const productsCatalog: ExItsProductCardProps[] = [
  {
    id: "pinoy-business-pos",
    name: "Pinoy Business POS",
    description:
      "Complete point-of-sale and business management for Filipino retailers — selling, inventory, Utang, suppliers, and multi-branch operations.",
    badge: "available",
    badgeLabel: "Available",
    cta: { label: "Explore", href: "/pos" },
    featured: true,
  },
  {
    id: "pinoy-service-pro",
    name: "Pinoy Service Pro",
    description:
      "Planned service-business management for Filipino service organizations such as salons, repair shops, and field teams.",
    badge: "coming-soon",
    badgeLabel: "Coming Soon",
    cta: { label: "Learn More", href: "/service-pro" },
  },
  {
    id: "pinoy-loan-manager",
    name: "Pinoy Loan Manager",
    description:
      "Planned lending-operations product for Filipino organizations. Implementation is not available yet.",
    badge: "coming-soon",
    badgeLabel: "Coming Soon",
    cta: { label: "Learn More", href: "#pinoy-loan-manager-notes" },
  },
  {
    id: "pinoy-buy-now-pay-later",
    name: "Pinoy Buy Now Pay Later",
    description:
      "In development. Project scaffold only — no financing capability is available yet.",
    badge: "in-development",
    badgeLabel: "In Development",
    cta: { label: "Learn More", href: "#pinoy-buy-now-pay-later-notes" },
  },
  {
    id: "pinoy-pawn-manager",
    name: "Pinoy Pawn Manager",
    description:
      "In development. Project scaffold only — no pawn operations are available yet.",
    badge: "in-development",
    badgeLabel: "In Development",
    cta: { label: "Learn More", href: "#pinoy-pawn-manager-notes" },
  },
];

export const roadmapNotes = [
  {
    id: "pinoy-loan-manager-notes",
    name: "Pinoy Loan Manager",
    status: "Coming Soon",
    body: "Documented as a planned lending-operations product. Implementation is absent and paused. No dedicated marketing route is published until the product name and route are confirmed.",
  },
  {
    id: "pinoy-buy-now-pay-later-notes",
    name: "Pinoy Buy Now Pay Later",
    status: "In Development",
    body: "Scaffold only. No financing domain, entities, or operational flows are available. Display name and route remain provisional.",
  },
  {
    id: "pinoy-pawn-manager-notes",
    name: "Pinoy Pawn Manager",
    status: "In Development",
    body: "Scaffold only. No pawn agreements, appraisal, or custody workflows are available. Marketing name and route remain provisional.",
  },
] as const;
