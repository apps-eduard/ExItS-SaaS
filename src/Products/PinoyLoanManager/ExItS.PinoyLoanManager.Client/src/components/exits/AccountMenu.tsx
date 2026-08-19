import { useEffect, useId, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

function initials(displayName?: string, username?: string) {
  const source = (displayName || username || "?").trim();
  const parts = source.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }
  return source.slice(0, 2).toUpperCase();
}

export function AccountMenu() {
  const { t } = useI18n();
  const { session, signOut } = useSession();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const menuId = useId();
  const label = session?.displayName || session?.username || t("app.name");

  useEffect(() => {
    if (!open) {
      return;
    }
    const onPointer = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", onPointer);
    return () => document.removeEventListener("mousedown", onPointer);
  }, [open]);

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        className="inline-flex size-9 items-center justify-center rounded-full bg-primary text-[length:var(--exits-text-xs)] font-bold text-primary-foreground"
        aria-expanded={open}
        aria-haspopup="menu"
        aria-controls={menuId}
        aria-label={label}
        onClick={() => setOpen((current) => !current)}
      >
        {initials(session?.displayName, session?.username)}
      </button>
      {open ? (
        <div
          id={menuId}
          role="menu"
          className="absolute right-0 z-50 mt-2 w-56 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3 shadow-sm"
        >
          <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">{label}</p>
          {session?.username && session.username !== session.displayName ? (
            <p className="mt-1 mb-0 truncate text-[length:var(--exits-text-xs)] text-muted">
              {session.username}
            </p>
          ) : null}
          <Button
            type="button"
            variant="ghost"
            className="mt-3 w-full"
            role="menuitem"
            onClick={() => {
              setOpen(false);
              void signOut();
            }}
          >
            {t("auth.signOut")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
