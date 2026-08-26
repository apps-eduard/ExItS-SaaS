import { User } from "lucide-react";
import { initialsFor } from "@/features/personal/people-status";
import { cn } from "@/lib/cn";

const sizeClasses = {
  sm: "size-8 text-[length:var(--exits-text-xs)]",
  md: "size-11 text-sm",
  lg: "size-14 text-base",
} as const;

export type PersonAvatarSize = keyof typeof sizeClasses;

export function PersonAvatar({
  name,
  size = "md",
  className,
}: {
  name: string;
  size?: PersonAvatarSize;
  className?: string;
}) {
  const initials = initialsFor(name);
  const showFallbackIcon = initials === "?";

  return (
    <span
      className={cn(
        "flex shrink-0 items-center justify-center rounded-full bg-primary font-bold text-primary-foreground",
        sizeClasses[size],
        className,
      )}
      aria-hidden="true"
      data-testid="person-avatar"
    >
      {showFallbackIcon ? (
        <User className={cn(size === "lg" ? "size-6" : "size-4")} aria-hidden="true" />
      ) : (
        initials
      )}
    </span>
  );
}
