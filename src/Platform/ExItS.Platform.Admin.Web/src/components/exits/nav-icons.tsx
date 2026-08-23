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
  Key,
  Landmark,
  LayoutDashboard,
  Package,
  Receipt,
  Repeat,
  ScrollText,
  Send,
  Settings,
  Shield,
  ShieldCheck,
  Star,
  Store,
  Tag,
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
  key: Key,
  landmark: Landmark,
  "layout-dashboard": LayoutDashboard,
  package: Package,
  receipt: Receipt,
  repeat: Repeat,
  "scroll-text": ScrollText,
  send: Send,
  settings: Settings,
  shield: Shield,
  "shield-check": ShieldCheck,
  star: Star,
  store: Store,
  tag: Tag,
  upload: Upload,
  user: User,
  "user-plus": UserPlus,
  users: Users,
};

export function NavIcon({
  name,
  className,
  active = false,
  compact = false,
}: {
  name: string;
  className?: string;
  active?: boolean;
  /** Icon-rail (collapsed sidebar) sizing */
  compact?: boolean;
}) {
  const Icon = icons[name] ?? LayoutDashboard;
  return (
    <span
      className={cn(
        "grid shrink-0 place-items-center rounded-md transition-[background-color,color,transform] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
        compact ? "size-9" : "size-8",
        active
          ? "bg-[var(--exits-primary-soft)] text-primary"
          : compact
            ? "text-muted group-hover/nav:text-primary group-hover/nav:bg-[var(--exits-primary-soft)]/70"
            : "text-muted group-hover/nav:text-foreground",
        className,
      )}
    >
      <Icon aria-hidden="true" size={compact ? 20 : 18} strokeWidth={active ? 2.25 : 2} />
    </span>
  );
}
