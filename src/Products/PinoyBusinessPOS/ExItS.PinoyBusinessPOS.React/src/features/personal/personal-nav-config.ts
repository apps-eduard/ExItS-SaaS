import type { LucideIcon } from "lucide-react";
import {
  ArrowLeftRight,
  Bell,
  BriefcaseBusiness,
  Building2,
  CheckSquare,
  Compass,
  Home,
  Link2,
  ListOrdered,
  QrCode,
  Settings,
  Store,
  UserPen,
  UserPlus,
  Users,
  Wallet,
} from "lucide-react";
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
  | "preferences";

export type PersonalNavGroupId = "primary" | "commerce" | "social" | "business";

export type PersonalNavItem = {
  id: PersonalNavItemId;
  to: string;
  labelKey: MessageKey;
  icon: LucideIcon;
  testId: string;
  matchPrefixes: string[];
  end?: boolean;
};

export type PersonalNavAccordionGroup = {
  id: PersonalNavGroupId;
  titleKey: MessageKey;
  icon: LucideIcon;
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
    icon: Home,
    testId: "personal-nav-home",
    matchPrefixes: ["/personal"],
    end: true,
  },
  {
    id: "utang",
    to: "/personal/utang",
    labelKey: "personal.nav.utang",
    icon: Wallet,
    testId: "personal-nav-utang",
    matchPrefixes: ["/personal/utang"],
  },
  {
    id: "todo",
    to: "/personal/todo",
    labelKey: "personal.nav.todo",
    icon: CheckSquare,
    testId: "personal-nav-todo",
    matchPrefixes: ["/personal/todo"],
  },
  {
    id: "orders",
    to: "/personal/orders",
    labelKey: "personal.nav.orders",
    icon: ListOrdered,
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
      icon: Store,
      items: [
        {
          id: "stores",
          to: "/personal/linked-merchants",
          labelKey: "personal.more.stores",
          icon: Store,
          testId: "personal-sidebar-stores",
          matchPrefixes: ["/personal/linked-merchants"],
        },
        {
          id: "customerLinks",
          to: "/personal/customer-links",
          labelKey: "personal.customerLinks.title",
          icon: Link2,
          testId: "personal-sidebar-customer-links",
          matchPrefixes: ["/personal/customer-links", "/personal/blocked-businesses"],
        },
      ],
    },
    {
      id: "social",
      titleKey: "personal.more.group.social",
      icon: Users,
      items: [
        {
          id: "people",
          to: "/personal/people",
          labelKey: "personal.home.people",
          icon: Users,
          testId: "personal-nav-people",
          matchPrefixes: ["/personal/people"],
        },
        {
          id: "notifications",
          to: "/personal/notifications",
          labelKey: "personal.social.notificationsTitle",
          icon: Bell,
          testId: "personal-nav-notifications",
          matchPrefixes: ["/personal/notifications"],
        },
        {
          id: "invitations",
          to: "/personal/invitations",
          labelKey: "personal.social.invitationsTitle",
          icon: UserPlus,
          testId: "personal-nav-invitations",
          matchPrefixes: ["/personal/invitations", "/personal/utang/invitations"],
        },
        {
          id: "qr",
          to: "/personal/my-qr",
          labelKey: "personal.social.qrTitle",
          icon: QrCode,
          testId: "personal-nav-qr",
          matchPrefixes: ["/personal/my-qr"],
        },
        {
          id: "workplaces",
          to: "/personal/workplaces",
          labelKey: "personal.workplaces.moreTile",
          icon: BriefcaseBusiness,
          testId: "personal-nav-workplaces",
          matchPrefixes: ["/personal/workplaces", "/personal/staff-invitations"],
        },
        {
          id: "ownership",
          to: "/personal/ownership-transfers",
          labelKey: "personal.ownershipTransfers.moreTile",
          icon: ArrowLeftRight,
          testId: "personal-nav-ownership",
          matchPrefixes: ["/personal/ownership-transfers"],
        },
      ],
    },
    {
      id: "business",
      titleKey: "personal.more.group.business",
      icon: Settings,
      items: [
        {
          id: "guide",
          to: "/personal/guide",
          labelKey: "personal.guide.title",
          icon: Compass,
          testId: "personal-nav-guide",
          matchPrefixes: ["/personal/guide"],
        },
        {
          id: "profile",
          to: "/personal/profile",
          labelKey: "personal.profile.edit",
          icon: UserPen,
          testId: "personal-nav-profile",
          matchPrefixes: ["/personal/profile"],
        },
        {
          id: "startBusiness",
          to: "/personal/explore-pos",
          labelKey: "personal.more.startBusiness",
          icon: Building2,
          testId: "personal-nav-start-business",
          matchPrefixes: ["/personal/explore-pos", "/personal/start-business"],
        },
        {
          id: "preferences",
          to: "/settings/preferences",
          labelKey: "preferences.title",
          icon: Settings,
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
