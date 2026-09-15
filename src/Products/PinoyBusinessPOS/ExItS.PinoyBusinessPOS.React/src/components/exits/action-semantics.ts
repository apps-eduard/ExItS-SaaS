import type { LucideIcon } from "lucide-react";
import {
  CircleCheck,
  CircleOff,
  Download,
  Eye,
  Filter,
  Pencil,
  Plus,
  Printer,
  RotateCw,
  Save,
  Search,
  SlidersHorizontal,
  Trash2,
  X,
} from "lucide-react";
import type {
  ButtonAppearance,
  ButtonIntentTone,
  ButtonLegacyVariant,
} from "@/components/ui/button";

/**
 * Canonical ExItS action semantics — icon + intent + appearance.
 * Prefer ONE Primary action per action group (hierarchy, not verb alone).
 *
 * @see Docs/UI/EXITS_UI_STANDARD.md
 * @see /ui-standards
 */

/** @deprecated Prefer ButtonIntentTone. Legacy Button `variant` alias. */
export type ButtonIntent = ButtonLegacyVariant | "ghost";

export type ExitsActionId =
  | "save"
  | "create"
  | "add"
  | "edit"
  | "cancel"
  | "delete"
  | "deactivate"
  | "activate"
  | "view"
  | "print"
  | "download"
  | "retry"
  | "search"
  | "filter"
  | "close";

export type ExitsActionDefinition = {
  id: ExitsActionId;
  /** Canonical Lucide icon. Cancel has none by default. */
  icon: LucideIcon | null;
  intent: ButtonIntentTone;
  appearance: ButtonAppearance;
  secondaryIntent?: ButtonIntentTone;
  secondaryAppearance?: ButtonAppearance;
  /**
   * @deprecated Prefer intent + appearance. Legacy Button variant when primary in group.
   */
  defaultIntent: ButtonLegacyVariant;
  /** @deprecated Prefer secondaryIntent + secondaryAppearance. */
  secondaryIntentLegacy?: ButtonLegacyVariant;
};

function toLegacyVariant(
  intent: ButtonIntentTone,
  appearance: ButtonAppearance,
): ButtonLegacyVariant {
  if (appearance === "ghost") {
    return "ghost";
  }
  if (appearance === "outline" && intent === "neutral") {
    return "outline";
  }
  switch (intent) {
    case "primary":
      return "default";
    case "neutral":
      return appearance === "solid" || appearance === "elevated" || appearance === "gradient"
        ? "secondary"
        : "outline";
    case "success":
      return "success";
    case "info":
      return "info";
    case "warning":
      return "warning";
    case "danger":
      return "destructive";
    default:
      return "default";
  }
}

export const EXITS_ACTIONS = {
  save: {
    id: "save",
    icon: Save,
    intent: "primary",
    appearance: "solid",
    defaultIntent: "default",
  },
  create: {
    id: "create",
    icon: Plus,
    intent: "primary",
    appearance: "solid",
    secondaryIntent: "neutral",
    secondaryAppearance: "outline",
    defaultIntent: "default",
    secondaryIntentLegacy: "outline",
  },
  add: {
    id: "add",
    icon: Plus,
    intent: "primary",
    appearance: "solid",
    secondaryIntent: "neutral",
    secondaryAppearance: "outline",
    defaultIntent: "default",
    secondaryIntentLegacy: "outline",
  },
  edit: {
    id: "edit",
    icon: Pencil,
    intent: "neutral",
    appearance: "outline",
    defaultIntent: "outline",
  },
  cancel: {
    id: "cancel",
    icon: null,
    intent: "neutral",
    appearance: "ghost",
    secondaryIntent: "neutral",
    secondaryAppearance: "outline",
    defaultIntent: "ghost",
    secondaryIntentLegacy: "outline",
  },
  delete: {
    id: "delete",
    icon: Trash2,
    intent: "danger",
    appearance: "outline",
    defaultIntent: "destructive",
  },
  deactivate: {
    id: "deactivate",
    icon: CircleOff,
    intent: "warning",
    appearance: "outline",
    defaultIntent: "warning",
  },
  activate: {
    id: "activate",
    icon: CircleCheck,
    intent: "success",
    appearance: "outline",
    defaultIntent: "success",
  },
  view: {
    id: "view",
    icon: Eye,
    intent: "neutral",
    appearance: "outline",
    defaultIntent: "outline",
  },
  print: {
    id: "print",
    icon: Printer,
    intent: "neutral",
    appearance: "outline",
    defaultIntent: "outline",
  },
  download: {
    id: "download",
    icon: Download,
    intent: "neutral",
    appearance: "outline",
    defaultIntent: "outline",
  },
  retry: {
    id: "retry",
    icon: RotateCw,
    intent: "neutral",
    appearance: "outline",
    defaultIntent: "outline",
  },
  search: {
    id: "search",
    icon: Search,
    intent: "neutral",
    appearance: "outline",
    defaultIntent: "outline",
  },
  filter: {
    id: "filter",
    icon: SlidersHorizontal,
    intent: "neutral",
    appearance: "outline",
    defaultIntent: "outline",
  },
  close: {
    id: "close",
    icon: X,
    intent: "neutral",
    appearance: "ghost",
    defaultIntent: "ghost",
  },
} as const satisfies Record<ExitsActionId, ExitsActionDefinition>;

/** Alias icons for documented alternatives (same semantic). */
export const EXITS_ACTION_ICON_ALIASES = {
  filterAlt: Filter,
} as const;

export function getActionIcon(action: ExitsActionId): LucideIcon | null {
  return EXITS_ACTIONS[action].icon;
}

export type ActionButtonStyle = {
  intent: ButtonIntentTone;
  appearance: ButtonAppearance;
};

/**
 * Resolve canonical intent + appearance for an action.
 * When `isPrimaryInGroup` is false, prefer secondary pair when defined.
 */
export function getActionButtonStyle(
  action: ExitsActionId,
  options?: { isPrimaryInGroup?: boolean },
): ActionButtonStyle {
  const def = EXITS_ACTIONS[action];
  const isPrimary = options?.isPrimaryInGroup !== false;
  if (!isPrimary && def.secondaryIntent && def.secondaryAppearance) {
    return { intent: def.secondaryIntent, appearance: def.secondaryAppearance };
  }
  return { intent: def.intent, appearance: def.appearance };
}

/**
 * @deprecated Prefer getActionButtonStyle (intent + appearance).
 * Resolve legacy Button `variant` for an action.
 */
export function getActionIntent(
  action: ExitsActionId,
  options?: { isPrimaryInGroup?: boolean },
): ButtonLegacyVariant {
  const style = getActionButtonStyle(action, options);
  const def = EXITS_ACTIONS[action];
  const isPrimary = options?.isPrimaryInGroup !== false;
  if (!isPrimary && def.secondaryIntentLegacy) {
    return def.secondaryIntentLegacy;
  }
  if (!isPrimary && def.secondaryIntent && def.secondaryAppearance) {
    return toLegacyVariant(def.secondaryIntent, def.secondaryAppearance);
  }
  return def.defaultIntent ?? toLegacyVariant(style.intent, style.appearance);
}
