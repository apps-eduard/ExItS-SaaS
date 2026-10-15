import type { ComponentType } from "react";
import { cn } from "@/lib/cn";

/** Props shared with the existing icon components so Personal can pass either. */
export type IconComponentProps = {
  className?: string;
  strokeWidth?: number | string;
  "aria-hidden"?: boolean | "true" | "false";
};

export type IconComponent = ComponentType<IconComponentProps>;

/** Glyphs requested for Personal. Names must stay alphabetical for the font stylesheet. */
const PERSONAL_SYMBOLS = [
  "account_balance_wallet",
  "apartment",
  "arrow_forward",
  "bolt",
  "business_center",
  "check_box",
  "checklist",
  "chevron_right",
  "dark_mode",
  "delete",
  "door_open",
  "edit",
  "event_busy",
  "event_upcoming",
  "expand_more",
  "explore",
  "group",
  "handshake",
  "home",
  "info",
  "left_panel_close",
  "left_panel_open",
  "light_mode",
  "link",
  "list_alt",
  "login",
  "menu",
  "monitoring",
  "more_horiz",
  "notifications",
  "palette",
  "payments",
  "person_add",
  "playlist_add",
  "qr_code",
  "refresh",
  "settings",
  "share",
  "store",
  "swap_horiz",
  "today",
  "view_sidebar",
  "visibility",
] as const;

const SYMBOL_STYLESHEET_ID = "personal-material-symbols";

/** Load the Personal Material Symbols subset. POS does not call this. */
export function ensurePersonalSymbols(): void {
  if (typeof document === "undefined") {
    return;
  }
  const href =
    "https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@24,400,0,0" +
    `&icon_names=${PERSONAL_SYMBOLS.join(",")}&display=block`;
  const existing = document.getElementById(SYMBOL_STYLESHEET_ID) as HTMLLinkElement | null;
  if (existing) {
    if (existing.getAttribute("href") !== href) {
      existing.href = href;
    }
    return;
  }
  const link = document.createElement("link");
  link.id = SYMBOL_STYLESHEET_ID;
  link.rel = "stylesheet";
  link.href = href;
  document.head.appendChild(link);
}

ensurePersonalSymbols();

/** Stable icon component for one Material Symbol name. */
export function symbol(name: (typeof PERSONAL_SYMBOLS)[number]): IconComponent {
  function MaterialSymbolIcon({ className, "aria-hidden": ariaHidden = true }: IconComponentProps) {
    return (
      <span className={cn("material-symbols-outlined", className)} aria-hidden={ariaHidden}>
        {name}
      </span>
    );
  }
  MaterialSymbolIcon.displayName = `MaterialSymbol(${name})`;
  return MaterialSymbolIcon;
}
