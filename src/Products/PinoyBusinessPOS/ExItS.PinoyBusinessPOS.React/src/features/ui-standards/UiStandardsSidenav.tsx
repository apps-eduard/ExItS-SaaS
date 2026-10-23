import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { PanelLeft, PanelLeftClose } from "lucide-react";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { cn } from "@/lib/cn";

/** Aligns with `.ui-standards-layout` compact breakpoint (`max-width: 719px`). */
export const UI_STANDARDS_DESKTOP_MIN_PX = 720;

type UiStandardsSidenavContextValue = {
  open: boolean;
  setOpen: (open: boolean) => void;
  toggle: () => void;
};

const UiStandardsSidenavContext = createContext<UiStandardsSidenavContextValue | null>(null);

function readDesktopDefault(): boolean {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return true;
  }
  return window.matchMedia(`(min-width: ${UI_STANDARDS_DESKTOP_MIN_PX}px)`).matches;
}

/**
 * Holds UI Standards category sidenav open state for the page + AppTopBar toggle.
 * Mobile default: hidden. Desktop default: open.
 */
export function UiStandardsSidenavProvider({ children }: { children: ReactNode }) {
  const isDesktop = useMediaMin(UI_STANDARDS_DESKTOP_MIN_PX);
  const [open, setOpen] = useState(readDesktopDefault);

  useEffect(() => {
    setOpen(isDesktop);
  }, [isDesktop]);

  const toggle = useCallback(() => {
    setOpen((current) => !current);
  }, []);

  const value = useMemo(
    () => ({
      open,
      setOpen,
      toggle,
    }),
    [open, toggle],
  );

  return (
    <UiStandardsSidenavContext.Provider value={value}>{children}</UiStandardsSidenavContext.Provider>
  );
}

export function useUiStandardsSidenav(): UiStandardsSidenavContextValue {
  const value = useContext(UiStandardsSidenavContext);
  if (!value) {
    throw new Error("useUiStandardsSidenav requires UiStandardsSidenavProvider");
  }
  return value;
}

export function useUiStandardsSidenavOptional(): UiStandardsSidenavContextValue | null {
  return useContext(UiStandardsSidenavContext);
}

/** Topbar control — hide / show the UI Standards category sidenav. */
export function UiStandardsSidenavToggle({ className }: { className?: string }) {
  const { open, toggle } = useUiStandardsSidenav();

  return (
    <button
      type="button"
      className={cn(
        "relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
      aria-label={open ? "Hide sidenav" : "Show sidenav"}
      aria-expanded={open}
      aria-controls="ui-standards-sidenav"
      data-testid="ui-standards-sidenav-toggle"
      onClick={toggle}
    >
      {open ? (
        <PanelLeftClose className="size-5" aria-hidden strokeWidth={2.25} />
      ) : (
        <PanelLeft className="size-5" aria-hidden strokeWidth={2.25} />
      )}
    </button>
  );
}
