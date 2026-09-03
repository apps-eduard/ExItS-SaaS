export const posFaq = [
  {
    question: "What is Pinoy Business POS?",
    answer:
      "Pinoy Business POS is ExItS's point-of-sale and business management product for Filipino retailers. It covers selling, inventory, customer management, purchasing, supplier ordering, and staff management.",
  },
  {
    question: "Does it work offline?",
    answer:
      "Yes. Pinoy Business POS includes offline selling mode. Sales processed offline are queued and automatically synced when connectivity is restored.",
  },
  {
    question: "What is Utang, and how does POS handle it?",
    answer:
      "Utang is customer credit — letting trusted customers take goods and pay later. Pinoy Business POS gives you a proper system to track credit limits, balances, and payment history for each customer.",
  },
  {
    question: "Can I manage multiple branches?",
    answer:
      "Yes. Each branch in Pinoy Business POS has its own stock, staff assignments, pricing overrides, and customer ordering settings. You can also group branches into Areas for rollup reporting and area-level oversight.",
  },
  {
    question: "What is an Area?",
    answer:
      "An Area is an organizational grouping of branches — useful when you have many locations and want to assign an Area Manager to oversee a group. Inventory and stock belong to branches; Areas provide reporting and management scope, not inventory ownership.",
  },
  {
    question: "Can customers order from my store online?",
    answer:
      "Yes. You can enable your customer storefront for linked ExItS Personal users. When your branch is set up and ordering is available, customers can browse your catalog and place orders. You review and accept or reject orders in Pinoy Business POS.",
  },
  {
    question: "How do I manage suppliers?",
    answer:
      "You can create and manage your supplier list, raise purchase orders, and record direct purchase receipts. You can also connect with other ExItS-registered businesses as suppliers, enabling a connected supply network.",
  },
  {
    question: "What staff roles are available?",
    answer:
      "Pinoy Business POS supports Owner, Manager, and Cashier access levels, among others. Each role has appropriate permissions — for example, cashiers can process sales but may not have access to sensitive financial reports. Access is enforced server-side.",
  },
  {
    question: "Is my data secure?",
    answer:
      "ExItS enforces role-based access at the server level. Staff only see and do what their role allows. Sensitive operations are audit-logged.",
  },
] as const;

export const posSoftwareApplicationJsonLd = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  name: "Pinoy Business POS",
  applicationCategory: "BusinessApplication",
  operatingSystem: "Web",
  description:
    "Pinoy Business POS by ExItS — point-of-sale, inventory, customer credit (Utang), supplier ordering, and multi-branch management for Filipino retailers.",
  url: "https://exits.ph/pos",
};
