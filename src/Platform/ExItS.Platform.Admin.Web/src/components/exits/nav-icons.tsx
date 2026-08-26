import type { LucideIcon } from "lucide-react";
import {
  Activity,
  AlertCircle,
  Boxes,
  Box,
  Building,
  Building2,
  CheckSquare,
  CreditCard,
  FileText,
  FlaskConical,
  Folder,
  Gauge,
  Key,
  Landmark,
  LayoutDashboard,
  LifeBuoy,
  Package,
  Receipt,
  Repeat,
  ScrollText,
  Search,
  Send,
  Settings,
  Shield,
  ShieldCheck,
  Star,
  Store,
  Tag,
  Timer,
  Upload,
  User,
  UserPlus,
  Users,
} from "lucide-react";
import { cn } from "@/lib/utils";

const icons: Record<string, LucideIcon> = {
  activity: Activity,
  "alert-circle": AlertCircle,
  box: Box,
  boxes: Boxes,
  building: Building,
  "building-2": Building2,
  "check-square": CheckSquare,
  "credit-card": CreditCard,
  "file-text": FileText,
  "flask-conical": FlaskConical,
  folder: Folder,
  gauge: Gauge,
  key: Key,
  landmark: Landmark,
  "layout-dashboard": LayoutDashboard,
  "life-buoy": LifeBuoy,
  package: Package,
  receipt: Receipt,
  repeat: Repeat,
  "scroll-text": ScrollText,
  search: Search,
  send: Send,
  settings: Settings,
  shield: Shield,
  "shield-check": ShieldCheck,
  star: Star,
  store: Store,
  tag: Tag,
  timer: Timer,
  upload: Upload,
  user: User,
  "user-plus": UserPlus,
  users: Users,
};

type NavIconSize = "md" | "sm" | "rail";

const sizeClasses: Record<NavIconSize, string> = {
  md: "size-8",
  sm: "size-7",
  rail: "size-9",
};

const iconSizes: Record<NavIconSize, number> = {
  md: 18,
  sm: 16,
  rail: 20,
};

export function NavIcon({
  name,
  className,
  active = false,
  compact = false,
  size,
}: {
  name: string;
  className?: string;
  active?: boolean;
  /** @deprecated Prefer `size="rail"` when the sidebar is collapsed. */
  compact?: boolean;
  size?: NavIconSize;
}) {
  const resolvedSize: NavIconSize = size ?? (compact ? "rail" : "md");
  const Icon = icons[name] ?? LayoutDashboard;
  return (
    <span
      className={cn(
        "grid shrink-0 place-items-center rounded-md transition-[background-color,color,transform] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
        sizeClasses[resolvedSize],
        active
          ? "bg-[var(--exits-primary-soft)] text-primary"
          : resolvedSize === "rail"
            ? "text-muted group-hover/nav:text-primary group-hover/nav:bg-[var(--exits-primary-soft)]/70"
            : "text-muted group-hover/nav:text-foreground",
        className,
      )}
    >
      <Icon
        aria-hidden="true"
        size={iconSizes[resolvedSize]}
        strokeWidth={active ? 2.25 : 2}
      />
    </span>
  );
}
