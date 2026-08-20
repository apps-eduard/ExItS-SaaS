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
  /** Structural group (e.g. By Product) — children are injected at resolve/render time. */
  kind?: "link" | "group";
};

export type NavigationSectionDefinition = {
  id: string;
  labelKey: MessageKey;
  order: number;
  items: NavigationItemDefinition[];
};

export type ResolvedNavigationItem = {
  id: string;
  labelKey?: MessageKey;
  /** Runtime label for dynamic catalog children (not i18n keys). */
  label?: string;
  icon: string;
  href?: string;
  presentation: "link" | "planned" | "context" | "underDevelopment" | "group";
  children?: ResolvedNavigationItem[];
};

export type ResolvedNavigationSection = {
  id: string;
  labelKey: MessageKey;
  items: ResolvedNavigationItem[];
};
