import type { MessageKey } from "@/lib/i18n/messages";

export type NavigationLifecycle =
  "AVAILABLE" | "PLANNED_DISABLED" | "CONTEXT_REQUIRED" | "DEV_TEST_ONLY";

export type PermissionRequirement =
  | { kind: "authenticated" }
  | { kind: "any"; codes: readonly string[] }
  | { kind: "platformAdministrator" };

export type NavigationItemDefinition = {
  id: string;
  labelKey: MessageKey;
  icon: string;
  href?: string;
  lifecycle: NavigationLifecycle;
  permission: PermissionRequirement;
  order: number;
};

export type NavigationSectionDefinition = {
  id: string;
  labelKey: MessageKey;
  order: number;
  items: NavigationItemDefinition[];
};

export type ResolvedNavigationItem = NavigationItemDefinition & {
  presentation: "link" | "planned" | "context";
};

export type ResolvedNavigationSection = {
  id: string;
  labelKey: MessageKey;
  items: ResolvedNavigationItem[];
};
