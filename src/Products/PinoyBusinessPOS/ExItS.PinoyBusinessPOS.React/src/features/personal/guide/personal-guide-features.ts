import type { MessageKey } from "@/i18n/messages";

/**
 * Learning-progress catalog for Explore ExItS.
 * These codes are NOT feature flags, entitlements, or authorization.
 */
export const PERSONAL_GUIDE_ROUTE = "/personal/guide";
export const PERSONAL_GUIDE_SCHEMA_VERSION = 1;

export const PERSONAL_GUIDE_CATEGORIES = [
  "account",
  "people",
  "money",
  "productivity",
  "shopping",
  "activity",
  "business",
] as const;

export type PersonalGuideCategory = (typeof PERSONAL_GUIDE_CATEGORIES)[number];

export type PersonalGuideFeature = {
  code: string;
  category: PersonalGuideCategory;
  titleKey: MessageKey;
  descriptionKey: MessageKey;
  bulletKeys: readonly MessageKey[];
  /** Existing app route. Destination remains authoritative for access. */
  route: string;
  availabilityNoteKey?: MessageKey;
};

export const PERSONAL_GUIDE_FEATURES: readonly PersonalGuideFeature[] = [
  {
    code: "profile",
    category: "account",
    titleKey: "personal.guide.feature.profile.title",
    descriptionKey: "personal.guide.feature.profile.description",
    bulletKeys: [
      "personal.guide.feature.profile.bullet1",
      "personal.guide.feature.profile.bullet2",
      "personal.guide.feature.profile.bullet3",
    ],
    route: "/personal/profile",
  },
  {
    code: "personal-qr",
    category: "account",
    titleKey: "personal.guide.feature.personal-qr.title",
    descriptionKey: "personal.guide.feature.personal-qr.description",
    bulletKeys: [
      "personal.guide.feature.personal-qr.bullet1",
      "personal.guide.feature.personal-qr.bullet2",
      "personal.guide.feature.personal-qr.bullet3",
    ],
    route: "/personal/my-qr",
  },
  {
    code: "account-security",
    category: "account",
    titleKey: "personal.guide.feature.account-security.title",
    descriptionKey: "personal.guide.feature.account-security.description",
    bulletKeys: [
      "personal.guide.feature.account-security.bullet1",
      "personal.guide.feature.account-security.bullet2",
      "personal.guide.feature.account-security.bullet3",
    ],
    route: "/settings/preferences",
  },
  {
    code: "install-pwa",
    category: "account",
    titleKey: "personal.guide.feature.install-pwa.title",
    descriptionKey: "personal.guide.feature.install-pwa.description",
    bulletKeys: [
      "personal.guide.feature.install-pwa.bullet1",
      "personal.guide.feature.install-pwa.bullet2",
      "personal.guide.feature.install-pwa.bullet3",
    ],
    route: "/personal/more",
    availabilityNoteKey: "personal.guide.availableWhenSupported",
  },
  {
    code: "people",
    category: "people",
    titleKey: "personal.guide.feature.people.title",
    descriptionKey: "personal.guide.feature.people.description",
    bulletKeys: [
      "personal.guide.feature.people.bullet1",
      "personal.guide.feature.people.bullet2",
      "personal.guide.feature.people.bullet3",
    ],
    route: "/personal/people",
  },
  {
    code: "invitations",
    category: "people",
    titleKey: "personal.guide.feature.invitations.title",
    descriptionKey: "personal.guide.feature.invitations.description",
    bulletKeys: [
      "personal.guide.feature.invitations.bullet1",
      "personal.guide.feature.invitations.bullet2",
      "personal.guide.feature.invitations.bullet3",
    ],
    route: "/personal/invitations",
  },
  {
    code: "customer-links",
    category: "people",
    titleKey: "personal.guide.feature.customer-links.title",
    descriptionKey: "personal.guide.feature.customer-links.description",
    bulletKeys: [
      "personal.guide.feature.customer-links.bullet1",
      "personal.guide.feature.customer-links.bullet2",
      "personal.guide.feature.customer-links.bullet3",
    ],
    route: "/personal/customer-links",
    availabilityNoteKey: "personal.guide.accessDeterminedOnOpen",
  },
  {
    code: "utang",
    category: "money",
    titleKey: "personal.guide.feature.utang.title",
    descriptionKey: "personal.guide.feature.utang.description",
    bulletKeys: [
      "personal.guide.feature.utang.bullet1",
      "personal.guide.feature.utang.bullet2",
      "personal.guide.feature.utang.bullet3",
    ],
    route: "/personal/utang",
  },
  {
    code: "utang-settlement",
    category: "money",
    titleKey: "personal.guide.feature.utang-settlement.title",
    descriptionKey: "personal.guide.feature.utang-settlement.description",
    bulletKeys: [
      "personal.guide.feature.utang-settlement.bullet1",
      "personal.guide.feature.utang-settlement.bullet2",
      "personal.guide.feature.utang-settlement.bullet3",
    ],
    route: "/personal/utang",
  },
  {
    code: "todo",
    category: "productivity",
    titleKey: "personal.guide.feature.todo.title",
    descriptionKey: "personal.guide.feature.todo.description",
    bulletKeys: [
      "personal.guide.feature.todo.bullet1",
      "personal.guide.feature.todo.bullet2",
      "personal.guide.feature.todo.bullet3",
    ],
    route: "/personal/todo",
  },
  {
    code: "todo-reminders",
    category: "productivity",
    titleKey: "personal.guide.feature.todo-reminders.title",
    descriptionKey: "personal.guide.feature.todo-reminders.description",
    bulletKeys: [
      "personal.guide.feature.todo-reminders.bullet1",
      "personal.guide.feature.todo-reminders.bullet2",
      "personal.guide.feature.todo-reminders.bullet3",
    ],
    route: "/personal/todo",
  },
  {
    code: "stores",
    category: "shopping",
    titleKey: "personal.guide.feature.stores.title",
    descriptionKey: "personal.guide.feature.stores.description",
    bulletKeys: [
      "personal.guide.feature.stores.bullet1",
      "personal.guide.feature.stores.bullet2",
      "personal.guide.feature.stores.bullet3",
    ],
    route: "/personal/linked-merchants",
    availabilityNoteKey: "personal.guide.accessDeterminedOnOpen",
  },
  {
    code: "business-qr",
    category: "shopping",
    titleKey: "personal.guide.feature.business-qr.title",
    descriptionKey: "personal.guide.feature.business-qr.description",
    bulletKeys: [
      "personal.guide.feature.business-qr.bullet1",
      "personal.guide.feature.business-qr.bullet2",
      "personal.guide.feature.business-qr.bullet3",
    ],
    route: "/personal/linked-merchants",
    availabilityNoteKey: "personal.guide.availableWhenApplicable",
  },
  {
    code: "shopping-cart",
    category: "shopping",
    titleKey: "personal.guide.feature.shopping-cart.title",
    descriptionKey: "personal.guide.feature.shopping-cart.description",
    bulletKeys: [
      "personal.guide.feature.shopping-cart.bullet1",
      "personal.guide.feature.shopping-cart.bullet2",
      "personal.guide.feature.shopping-cart.bullet3",
    ],
    route: "/personal/linked-merchants",
  },
  {
    code: "checkout",
    category: "shopping",
    titleKey: "personal.guide.feature.checkout.title",
    descriptionKey: "personal.guide.feature.checkout.description",
    bulletKeys: [
      "personal.guide.feature.checkout.bullet1",
      "personal.guide.feature.checkout.bullet2",
      "personal.guide.feature.checkout.bullet3",
    ],
    route: "/personal/linked-merchants",
    availabilityNoteKey: "personal.guide.accessDeterminedOnOpen",
  },
  {
    code: "my-orders",
    category: "shopping",
    titleKey: "personal.guide.feature.my-orders.title",
    descriptionKey: "personal.guide.feature.my-orders.description",
    bulletKeys: [
      "personal.guide.feature.my-orders.bullet1",
      "personal.guide.feature.my-orders.bullet2",
      "personal.guide.feature.my-orders.bullet3",
    ],
    route: "/personal/orders",
  },
  {
    code: "notifications",
    category: "activity",
    titleKey: "personal.guide.feature.notifications.title",
    descriptionKey: "personal.guide.feature.notifications.description",
    bulletKeys: [
      "personal.guide.feature.notifications.bullet1",
      "personal.guide.feature.notifications.bullet2",
      "personal.guide.feature.notifications.bullet3",
    ],
    route: "/personal/notifications",
  },
  {
    code: "start-business",
    category: "business",
    titleKey: "personal.guide.feature.start-business.title",
    descriptionKey: "personal.guide.feature.start-business.description",
    bulletKeys: [
      "personal.guide.feature.start-business.bullet1",
      "personal.guide.feature.start-business.bullet2",
      "personal.guide.feature.start-business.bullet3",
    ],
    route: "/personal/explore-pos",
    availabilityNoteKey: "personal.guide.accessDeterminedOnOpen",
  },
  {
    code: "business-switching",
    category: "business",
    titleKey: "personal.guide.feature.business-switching.title",
    descriptionKey: "personal.guide.feature.business-switching.description",
    bulletKeys: [
      "personal.guide.feature.business-switching.bullet1",
      "personal.guide.feature.business-switching.bullet2",
      "personal.guide.feature.business-switching.bullet3",
    ],
    route: "/personal/more",
    availabilityNoteKey: "personal.guide.availableWhenApplicable",
  },
  {
    code: "ownership-transfer",
    category: "business",
    titleKey: "personal.guide.feature.ownership-transfer.title",
    descriptionKey: "personal.guide.feature.ownership-transfer.description",
    bulletKeys: [
      "personal.guide.feature.ownership-transfer.bullet1",
      "personal.guide.feature.ownership-transfer.bullet2",
      "personal.guide.feature.ownership-transfer.bullet3",
    ],
    route: "/personal/ownership-transfers",
    availabilityNoteKey: "personal.guide.availableWhenApplicable",
  },
];

export const PERSONAL_GUIDE_FEATURE_CODES: ReadonlySet<string> = new Set(
  PERSONAL_GUIDE_FEATURES.map((feature) => feature.code),
);

export const PERSONAL_GUIDE_CATEGORY_TITLE_KEYS: Record<PersonalGuideCategory, MessageKey> = {
  account: "personal.guide.category.account",
  people: "personal.guide.category.people",
  money: "personal.guide.category.money",
  productivity: "personal.guide.category.productivity",
  shopping: "personal.guide.category.shopping",
  activity: "personal.guide.category.activity",
  business: "personal.guide.category.business",
};

export function getPersonalGuideFeature(code: string): PersonalGuideFeature | undefined {
  return PERSONAL_GUIDE_FEATURES.find((feature) => feature.code === code);
}

export function personalGuideFeaturesByCategory(
  category: PersonalGuideCategory,
): PersonalGuideFeature[] {
  return PERSONAL_GUIDE_FEATURES.filter((feature) => feature.category === category);
}
