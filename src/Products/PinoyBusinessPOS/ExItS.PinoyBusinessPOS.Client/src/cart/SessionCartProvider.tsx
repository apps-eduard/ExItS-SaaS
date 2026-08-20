import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { registerCartLineCountGetter } from "@/pwa/apply-pwa-update";

export type SessionCartLine = {
  productId: string;
  sku: string | null;
  name: string;
  unitPrice: number;
  quantity: number;
};

type SessionCartContextValue = {
  lines: SessionCartLine[];
  lineCount: number;
  subtotal: number;
  addProduct: (product: PosCatalogProductDto, quantity?: number) => void;
  incrementLine: (productId: string) => void;
  decrementLine: (productId: string) => void;
  removeLine: (productId: string) => void;
  clear: () => void;
};

const SessionCartContext = createContext<SessionCartContextValue | null>(null);

function toCartLine(product: PosCatalogProductDto, quantity: number): SessionCartLine {
  return {
    productId: product.productId,
    sku: product.sku ?? null,
    name: product.name,
    unitPrice: product.sellingPrice,
    quantity,
  };
}

export function SessionCartProvider({ children }: { children: ReactNode }) {
  const [lines, setLines] = useState<SessionCartLine[]>([]);

  const addProduct = useCallback((product: PosCatalogProductDto, quantity = 1) => {
    const delta = Math.max(quantity, 1);
    setLines((current) => {
      const existing = current.find((line) => line.productId === product.productId);
      if (existing) {
        return current.map((line) =>
          line.productId === product.productId
            ? { ...line, quantity: line.quantity + delta }
            : line,
        );
      }
      return [...current, toCartLine(product, delta)];
    });
  }, []);

  const incrementLine = useCallback((productId: string) => {
    setLines((current) =>
      current.map((line) =>
        line.productId === productId ? { ...line, quantity: line.quantity + 1 } : line,
      ),
    );
  }, []);

  const decrementLine = useCallback((productId: string) => {
    setLines((current) =>
      current
        .map((line) =>
          line.productId === productId ? { ...line, quantity: line.quantity - 1 } : line,
        )
        .filter((line) => line.quantity > 0),
    );
  }, []);

  const removeLine = useCallback((productId: string) => {
    setLines((current) => current.filter((line) => line.productId !== productId));
  }, []);

  const clear = useCallback(() => {
    setLines([]);
  }, []);

  const lineCount = useMemo(() => lines.reduce((total, line) => total + line.quantity, 0), [lines]);

  const subtotal = useMemo(
    () => lines.reduce((total, line) => total + line.unitPrice * line.quantity, 0),
    [lines],
  );

  useEffect(() => {
    registerCartLineCountGetter(() => lineCount);
    return () => {
      registerCartLineCountGetter(null);
    };
  }, [lineCount]);

  const value = useMemo<SessionCartContextValue>(
    () => ({
      lines,
      lineCount,
      subtotal,
      addProduct,
      incrementLine,
      decrementLine,
      removeLine,
      clear,
    }),
    [addProduct, clear, decrementLine, incrementLine, lineCount, lines, removeLine, subtotal],
  );

  return <SessionCartContext.Provider value={value}>{children}</SessionCartContext.Provider>;
}

export function useSessionCart(): SessionCartContextValue {
  const context = useContext(SessionCartContext);
  if (!context) {
    throw new Error("useSessionCart must be used within SessionCartProvider");
  }
  return context;
}

export function useSessionCartOptional(): SessionCartContextValue | null {
  return useContext(SessionCartContext);
}
