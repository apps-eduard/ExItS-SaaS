import { symbol, type IconComponent } from "@/components/icons/MaterialSymbol";
import type { MessageKey } from "@/i18n/messages";

export type PersonalNavItemId =
  | "home"
  | "utang"
  | "todo"
  | "orders"
  | "stores"
  | "customerLinks"
  | "people"
  | "notifications"
  | "invitations"
  | "qr"
  | "workplaces"
  | "ownership"
  | "guide"
  | "profile"
  | "startBusiness"
  | "subscriptions"
  | "preferences";

export type PersonalNavGroupId = "primary" | "commerce" | "social" | "business";

export type PersonalNavItem = {
  id: PersonalNavItemId;
  to: string;
  labelKey: MessageKey;
  icon: IconComponent;
  testId: string;
  matchPrefixes: string[];
  end?: boolean;
};

export type PersonalNavAccordionGroup = {
  id: PersonalNavGroupId;
  titleKey: MessageKey;
  icon: IconComponent;
  items: readonly PersonalNavItem[];
};

export type PersonalNavGroup =
  | { id: "primary"; items: readonly PersonalNavItem[] }
  | PersonalNavAccordionGroup;

/** Bottom nav + desktop sidebar primary destinations. */
export const PERSONAL_PRIMARY_NAV_ITEMS: readonly PersonalNavItem[] = [
  {
    id: "home",
    to: "/personal",
    labelKey: "personal.nav.home",
    icon: symbol("home"),
    testId: "personal-nav-home",
    matchPrefixes: ["/personal"],
    end: true,
  },
  {
    id: "utang",
    to: "/personal/utang",
    labelKey: "personal.nav.utang",
    icon: symbol("account_balance_wallet"),
    testId: "personal-nav-utang",
    matchPrefixes: ["/personal/utang"],
  },
  {
    id: "todo",
    to: "/personal/todo",
    labelKey: "personal.nav.todo",
    icon: symbol("check_box"),
    testId: "personal-nav-todo",
    matchPrefixes: ["/personal/todo"],
  },
  {
    id: "orders",
    to: "/personal/orders",
    labelKey: "personal.nav.orders",
    icon: symbol("list_alt"),
    testId: "personal-nav-orders",
    matchPrefixes: ["/personal/orders"],
  },
] as const;

export function buildPersonalSidebarGroups(): readonly PersonalNavGroup[] {
  return [
    { id: "primary", items: PERSONAL_PRIMARY_NAV_ITEMS },
    {
      id: "commerce",
      titleKey: "personal.more.group.commerce",
      icon: symbol("store"),
      items: [
        {
          id: "stores",
          to: "/personal/linked-merchants",
          labelKey: "personal.more.stores",
          icon: symbol("store"),
          testId: "personal-sidebar-stores",
          matchPrefixes: ["/personal/linked-merchants"],
        },
        {
          id: "customerLinks",
          to: "/personal/customer-links",
          labelKey: "personal.customerLinks.title",
          icon: symbol("link"),
          testId: "personal-sidebar-customer-links",
          matchPrefixes: ["/personal/customer-links", "/personal/blocked-businesses"],
        },
      ],
    },
    {
      id: "social",
      titleKey: "personal.more.group.social",
      icon: symbol("group"),
      items: [
        {
          id: "people",
          to: "/personal/people",
          labelKey: "personal.home.people",
          icon: symbol("group"),
          testId: "personal-nav-people",
          matchPrefixes: ["/personal/people"],
        },
        {
          id: "notifications",
          to: "/personal/notifications",
          labelKey: "personal.social.notificationsTitle",
          icon: symbol("notifications"),
          testId: "personal-nav-notifications",
          matchPrefixes: ["/personal/notifications"],
        },
        {
          id: "invitations",
          to: "/personal/invitations",
          labelKey: "personal.social.invitationsTitle",
          icon: symbol("person_add"),
          testId: "personal-nav-invitations",
          matchPrefixes: ["/personal/invitations", "/personal/utang/invitations"],
        },
        {
          id: "qr",
          to: "/personal/my-qr",
          labelKey: "personal.social.qrTitle",
          icon: symbol("qr_code"),
          testId: "personal-nav-qr",
          matchPrefixes: ["/personal/my-qr"],
        },
        {
          id: "workplaces",
          to: "/personal/workplaces",
          labelKey: "personal.workplaces.moreTile",
          icon: symbol("business_center"),
          testId: "personal-nav-workplaces",
          matchPrefixes: ["/personal/workplaces", "/personal/staff-invitations"],
        },
        {
          id: "ownership",
          to: "/personal/ownership-transfers",
          labelKey: "personal.ownershipTransfers.moreTile",
          icon: symbol("swap_horiz"),
          testId: "personal-nav-ownership",
          matchPrefixes: ["/personal/ownership-transfers"],
        },
      ],
    },
    {
      id: "business",
      titleKey: "personal.more.group.business",
      icon: symbol("settings"),
      items: [
        {
          id: "guide",
          to: "/personal/guide",
          labelKey: "personal.guide.title",
          icon: symbol("explore"),
          testId: "personal-nav-guide",
          matchPrefixes: ["/personal/guide"],
        },
        {
          id: "profile",
          to: "/personal/profile",
          labelKey: "personal.profile.edit",
          icon: symbol("edit"),
          testId: "personal-nav-profile",
          matchPrefixes: ["/personal/profile"],
        },
        {
          id: "subscriptions",
          to: "/personal/subscriptions",
          labelKey: "personal.subscriptions.title",
          icon: symbol("payments"),
          testId: "personal-nav-subscriptions",
          matchPrefixes: ["/personal/subscriptions"],
        },
        {
          id: "startBusiness",
          to: "/personal/businesses",
          labelKey: "personal.more.startBusiness",
          icon: symbol("apartment"),
          testId: "personal-nav-start-business",
          matchPrefixes: ["/personal/businesses", "/personal/explore-pos", "/personal/start-business"],
        },
        {
          id: "preferences",
          to: "/settings/preferences",
          labelKey: "preferences.title",
          icon: symbol("settings"),
          testId: "personal-nav-preferences",
          matchPrefixes: ["/settings/preferences"],
        },
      ],
    },
  ];
}

export function flattenPersonalNavItems(
  groups: readonly PersonalNavGroup[],
): readonly PersonalNavItem[] {
  const items: PersonalNavItem[] = [];
  for (const group of groups) {
    items.push(...group.items);
  }
  return items;
}

export function matchPersonalNavItem(
  pathname: string,
  items: ReadonlyArray<PersonalNavItem>,
): PersonalNavItemId | null {
  let best: PersonalNavItem | null = null;
  for (const item of items) {
    if (item.end) {
      if (pathname === item.to) {
        return item.id;
      }
      continue;
    }
    for (const prefix of item.matchPrefixes) {
      if (pathname === prefix || pathname.startsWith(`${prefix}/`)) {
        if (!best || prefix.length > (best.matchPrefixes[0]?.length ?? 0)) {
          best = item;
        }
      }
    }
  }
  return best?.id ?? null;
}
