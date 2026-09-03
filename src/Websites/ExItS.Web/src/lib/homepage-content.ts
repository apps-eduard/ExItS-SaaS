export const homepageFaq = [
  {
    question: "What is ExItS?",
    answer:
      "ExItS is a multi-product SaaS platform built for Filipino businesses. Our flagship product, Pinoy Business POS, is a complete point-of-sale and business management system. We are building additional products for service businesses, lending, and more.",
  },
  {
    question: "Is Pinoy Business POS available now?",
    answer:
      "Yes. Pinoy Business POS is the primary ExItS product with a working implementation. Get started by creating your account.",
  },
  {
    question: "Do I need the internet to use Pinoy Business POS?",
    answer:
      "No. Pinoy Business POS supports offline selling. You can continue processing sales when your internet connection is unavailable, and your transactions sync when you're back online.",
  },
  {
    question: "Can I manage multiple branches?",
    answer:
      "Yes. Pinoy Business POS supports multi-branch organizations. Each branch manages its own stock, staff, pricing, and customer ordering settings. You can also group branches into Areas for easier oversight.",
  },
  {
    question: "What other products does ExItS offer?",
    answer:
      "ExItS is building Pinoy Service Pro (for service businesses), Pinoy Loan Manager, Pinoy Buy Now Pay Later, and Pinoy Pawn Manager. These products are currently in planning or early development. You can join our waitlist to be notified when they launch.",
  },
  {
    question: "How do I get started?",
    answer:
      'Click "Get Started" to create your ExItS account. From there, you can set up your organization and begin using Pinoy Business POS.',
  },
  {
    question: "How much does ExItS cost?",
    answer:
      "Our pricing is currently being finalized. Visit the Pricing page for the latest information, or contact us for a conversation about your business needs.",
  },
] as const;

export const organizationJsonLd = {
  "@context": "https://schema.org",
  "@type": "Organization",
  name: "ExItS",
  url: "https://exits.ph",
  logo: "https://exits.ph/icon.svg",
  sameAs: [] as string[],
};
