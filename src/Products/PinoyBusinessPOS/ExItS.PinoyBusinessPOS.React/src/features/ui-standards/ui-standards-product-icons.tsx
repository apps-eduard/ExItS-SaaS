import type { LucideIcon } from "lucide-react";
import {
  ArrowLeftRight,
  Building2,
  CalendarDays,
  CirclePlus,
  LayoutDashboard,
  Package,
  Settings2,
  ShoppingBag,
  Sparkles,
  SquareMousePointer,
  UserRound,
} from "lucide-react";
import type { UiStandardFilterCategory } from "@/features/ui-standards/ui-standard-catalog";

/** Shell / page icons for Organization Hybrid Glass leaf pages. */
export const UI_STANDARD_NAV_ICONS: Partial<Record<UiStandardFilterCategory, LucideIcon>> = {
  "button-glass": Sparkles,
  "fab-glass": CirclePlus,
  "input-glass": Sparkles,
  "date-glass": CalendarDays,
  "quick-action-glass": SquareMousePointer,
  "card-glass-menu": Sparkles,
  "card-glass-soft-kpi": Sparkles,
  "card-glass-hybrid-kpi": Sparkles,
  "card-glass-solid": Sparkles,
  "card-glass-timeline": Sparkles,
  "card-glass-table": Sparkles,
  "card-glass-settings": Sparkles,
  "org-ehg-dashboard": LayoutDashboard,
  "org-ehg-inventory": Package,
  "org-ehg-orders": ShoppingBag,
  "org-ehg-stock-requests": ArrowLeftRight,
  "org-ehg-branch-settings": Settings2,
  "org-ehg-button-glass": Sparkles,
  "org-ehg-fab-glass": CirclePlus,
  "org-ehg-input-glass": Sparkles,
  "org-ehg-date-glass": CalendarDays,
  "org-ehg-quick-action-glass": SquareMousePointer,
  "org-ehg-card-glass-menu": Sparkles,
  "org-ehg-card-glass-soft-kpi": Sparkles,
  "org-ehg-card-glass-hybrid-kpi": Sparkles,
  "org-ehg-card-glass-solid": Sparkles,
  "org-ehg-card-glass-timeline": Sparkles,
  "org-ehg-card-glass-table": Sparkles,
  "org-ehg-card-glass-settings": Sparkles,
};

export const UI_STANDARDS_SHELL_ICONS = {
  personal: UserRound,
  organization: Building2,
} as const;
