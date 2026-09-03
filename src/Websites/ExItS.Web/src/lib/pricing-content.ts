export const pricingFaq = [
  {
    question: "How is Pinoy Business POS priced?",
    answer:
      "Pricing details are being finalized. Contact us for a conversation about your business needs, or check back here when commercial plans are published.",
  },
  {
    question: "Is there a free plan or trial?",
    answer:
      "Free trial and free plan availability have not been announced yet. We will publish this after the commercial pricing decision is complete.",
  },
  {
    question: "Can I change plans later?",
    answer:
      "Plan change policies have not been published yet. Contact us if you need guidance while pricing is being finalized.",
  },
  {
    question: "Are there limits on branches or staff per plan?",
    answer:
      "Branch and staff limits per plan have not been published yet. Confirmed product capabilities exist in Pinoy Business POS; packaging by plan is still TBD.",
  },
] as const;

export const pricingCapabilityRows = [
  { feature: "POS selling", availability: "Confirmed" },
  { feature: "Catalog management", availability: "Confirmed" },
  { feature: "Inventory tracking", availability: "Confirmed" },
  { feature: "Customer management", availability: "Confirmed" },
  { feature: "Utang (customer credit)", availability: "Confirmed" },
  { feature: "Basic reporting and cashier shifts", availability: "Confirmed" },
  { feature: "Multiple branches", availability: "Confirmed" },
  { feature: "Area grouping", availability: "Confirmed" },
  { feature: "Connected ExItS suppliers", availability: "Confirmed" },
  { feature: "Customer storefront / ordering", availability: "Confirmed" },
] as const;

/** Recommended plan first in DOM for mobile stacking; CSS order centers it on desktop. */
export const pricingCardSlots = [
  {
    planName: "Growing business",
    price: "Pricing TBD",
    priceNote: "Commercial plan name and amount are not published yet.",
    features: [
      "Intended for small multi-branch operations",
      "Included features per plan TBD",
      "Branch and staff limits TBD",
    ],
    recommended: true,
    orderClassName: "order-1 lg:order-2",
  },
  {
    planName: "Solo / single-branch",
    price: "Pricing TBD",
    priceNote: "Commercial plan name and amount are not published yet.",
    features: [
      "Intended for solo sellers and single locations",
      "Included features per plan TBD",
      "Branch and staff limits TBD",
    ],
    recommended: false,
    orderClassName: "order-2 lg:order-1",
  },
  {
    planName: "Larger multi-branch",
    price: "Pricing TBD",
    priceNote: "Commercial plan name and amount are not published yet.",
    features: [
      "Intended for larger multi-branch retailers",
      "Included features per plan TBD",
      "Branch and staff limits TBD",
    ],
    recommended: false,
    orderClassName: "order-3 lg:order-3",
  },
] as const;
