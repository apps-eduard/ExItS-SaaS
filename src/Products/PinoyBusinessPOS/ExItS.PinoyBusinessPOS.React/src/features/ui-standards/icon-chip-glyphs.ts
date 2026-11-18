import {
  AlertTriangle,
  Bell,
  Building2,
  CalendarDays,
  CheckCircle2,
  CreditCard,
  FileText,
  Home,
  Info,
  Package,
  Pencil,
  Plus,
  Receipt,
  Search,
  Settings,
  ShoppingCart,
  Store,
  Trash2,
  UserRound,
  Users,
  Wallet,
  type LucideIcon,
} from "lucide-react";

export type IconChipGlyphId =
  | "store"
  | "users"
  | "wallet"
  | "settings"
  | "cart"
  | "home"
  | "package"
  | "receipt"
  | "user"
  | "plus"
  | "search"
  | "bell"
  | "calendar"
  | "file"
  | "pencil"
  | "trash"
  | "building"
  | "card"
  | "check"
  | "alert"
  | "info";

export type IconChipGlyphEntry = {
  id: IconChipGlyphId;
  label: string;
  icon: LucideIcon;
};

/** Curated Lucide marks for product / UI Standards icon chip playground. */
export const ICON_CHIP_GLYPH_CATALOG: ReadonlyArray<IconChipGlyphEntry> = [
  { id: "store", label: "Store", icon: Store },
  { id: "users", label: "Users", icon: Users },
  { id: "wallet", label: "Wallet", icon: Wallet },
  { id: "settings", label: "Settings", icon: Settings },
  { id: "cart", label: "Shopping cart", icon: ShoppingCart },
  { id: "home", label: "Home", icon: Home },
  { id: "package", label: "Package", icon: Package },
  { id: "receipt", label: "Receipt", icon: Receipt },
  { id: "user", label: "User", icon: UserRound },
  { id: "plus", label: "Plus", icon: Plus },
  { id: "search", label: "Search", icon: Search },
  { id: "bell", label: "Bell", icon: Bell },
  { id: "calendar", label: "Calendar", icon: CalendarDays },
  { id: "file", label: "Document", icon: FileText },
  { id: "pencil", label: "Pencil", icon: Pencil },
  { id: "trash", label: "Trash", icon: Trash2 },
  { id: "building", label: "Building", icon: Building2 },
  { id: "card", label: "Credit card", icon: CreditCard },
  { id: "check", label: "Check circle", icon: CheckCircle2 },
  { id: "alert", label: "Alert", icon: AlertTriangle },
  { id: "info", label: "Info", icon: Info },
];

export const ICON_CHIP_DEFAULT_GLYPH_ID: IconChipGlyphId = "store";