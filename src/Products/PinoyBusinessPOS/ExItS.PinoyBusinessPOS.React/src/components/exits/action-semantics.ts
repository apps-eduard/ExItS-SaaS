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
import type { VariantProps } from "class-variance-authority";
import type { buttonVariants } from "@/components/ui/button";

/**
 * Canonical ExItS action semantics — icon + default button intent.
 * Prefer ONE Primary action per action group (hierarchy, not verb alone).
 *
 * @see Docs/UI/EXITS_UI_STANDARD.md
 * @see /ui-standard
 */

export type ButtonIntent = NonNullable<VariantProps<typeof buttonVariants>["variant"]>;

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
  /** Default Button variant when this action is the main action in its group. */
  defaultIntent: ButtonIntent | "ghost";
  /** Prefer outline when this action is secondary to another primary in the same group. */
  secondaryIntent?: ButtonIntent | "ghost";
};

export const EXITS_ACTIONS = {
  save: {
    id: "save",
    icon: Save,
    defaultIntent: "default",
  },
  create: {
    id: "create",
    icon: Plus,
    defaultIntent: "default",
    secondaryIntent: "outline",
  },
  add: {
    id: "add",
    icon: Plus,
    defaultIntent: "default",
    secondaryIntent: "outline",
  },
  edit: {
    id: "edit",
    icon: Pencil,
    defaultIntent: "outline",
  },
  cancel: {
    id: "cancel",
    icon: null,
    defaultIntent: "ghost",
    secondaryIntent: "outline",
  },
  delete: {
    id: "delete",
    icon: Trash2,
    defaultIntent: "destructive",
  },
  deactivate: {
    id: "deactivate",
    icon: CircleOff,
    defaultIntent: "warning",
  },
  activate: {
    id: "activate",
    icon: CircleCheck,
    defaultIntent: "success",
  },
  view: {
    id: "view",
    icon: Eye,
    defaultIntent: "outline",
  },
  print: {
    id: "print",
    icon: Printer,
    defaultIntent: "outline",
  },
  download: {
    id: "download",
    icon: Download,
    defaultIntent: "outline",
  },
  retry: {
    id: "retry",
    icon: RotateCw,
    defaultIntent: "outline",
  },
  search: {
    id: "search",
    icon: Search,
    defaultIntent: "outline",
  },
  filter: {
    id: "filter",
    icon: SlidersHorizontal,
    defaultIntent: "outline",
  },
  close: {
    id: "close",
    icon: X,
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

/**
 * Resolve button intent for an action.
 * When `isPrimaryInGroup` is false, prefer secondaryIntent when defined.
 */
export function getActionIntent(
  action: ExitsActionId,
  options?: { isPrimaryInGroup?: boolean },
): ButtonIntent | "ghost" {
  const def = EXITS_ACTIONS[action];
  const isPrimary = options?.isPrimaryInGroup !== false;
  if (!isPrimary && def.secondaryIntent) {
    return def.secondaryIntent;
  }
  return def.defaultIntent;
}
