import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";

type SellingModeContextValue = {
  isSellingMode: boolean;
  returnRoute: string | null;
  enter: (returnRoute: string) => void;
  exit: () => void;
  clear: () => void;
};

const SellingModeContext = createContext<SellingModeContextValue | null>(null);

export function SellingModeProvider({ children }: { children: ReactNode }) {
  const [isSellingMode, setIsSellingMode] = useState(false);
  const [returnRoute, setReturnRoute] = useState<string | null>(null);

  const enter = useCallback((route: string) => {
    setIsSellingMode(true);
    setReturnRoute(route.trim() || "/role/owner");
  }, []);

  const exit = useCallback(() => {
    setIsSellingMode(false);
  }, []);

  const clear = useCallback(() => {
    setIsSellingMode(false);
    setReturnRoute(null);
  }, []);

  const value = useMemo(
    () => ({ isSellingMode, returnRoute, enter, exit, clear }),
    [clear, enter, exit, isSellingMode, returnRoute],
  );

  return <SellingModeContext.Provider value={value}>{children}</SellingModeContext.Provider>;
}

export function useSellingMode(): SellingModeContextValue {
  const context = useContext(SellingModeContext);
  if (!context) {
    throw new Error("useSellingMode must be used within SellingModeProvider");
  }
  return context;
}
