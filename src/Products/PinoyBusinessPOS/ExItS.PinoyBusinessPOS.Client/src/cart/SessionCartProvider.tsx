import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import {
  cartLineKey,
  isByWeightSellingMode,
  resolveSellUnitPrice,
  roundMoney,
  roundQuantity,
} from "@/cart/sell-cart-helpers";
import { registerCartLineCountGetter } from "@/pwa/apply-pwa-update";

export type SessionCartLine = {
  /** Stable key: productId + selected sell unit (or base). */
  lineKey: string;
  productId: string;
  sku: string | null;
  name: string;
  sellingMode: string;
  productUnitId: string | null;
  unitLabel: string;
  multiplierToBase: number;
  /** Catalog price for the selected sell unit (client preview only). */
  unitPrice: number;
  /**
   * Cashier-entered quantity: count for PerItem / pack units, kilograms for ByWeight.
   * Line amount = quantity × unitPrice.
   */
  quantity: number;
  baseUnitOfMeasure: string;
};

export type AddCartLineOptions = {
  quantity?: number;
  unit?: PosCatalogProductUnitDto | null;
  /** When true, replace existing line quantity instead of adding. */
  replaceQuantity?: boolean;
};

type SessionCartContextValue = {
  lines: SessionCartLine[];
  /** Distinct cart lines (not summed pack quantities). */
  lineCount: number;
  /** Sum of entered quantities — used for legacy summary “N items” when all PerItem. */
  quantityTotal: number;
  subtotal: number;
  addProduct: (product: PosCatalogProductDto, quantity?: number) => void;
  addLine: (product: PosCatalogProductDto, options?: AddCartLineOptions) => void;
  setLineQuantity: (lineKey: string, quantity: number) => void;
  incrementLine: (lineKey: string) => void;
  decrementLine: (lineKey: string) => void;
  removeLine: (lineKey: string) => void;
  clear: () => void;
  getLine: (lineKey: string) => SessionCartLine | undefined;
  getEnteredQuantity: (productId: string, productUnitId: string | null) => number;
};

const SessionCartContext = createContext<SessionCartContextValue | null>(null);

function normalizeSellingMode(mode: string | null | undefined): string {
  const trimmed = (mode ?? "").trim();
  return trimmed.length > 0 ? trimmed : "PerItem";
}

function unitLabelFor(
  product: PosCatalogProductDto,
  unit: PosCatalogProductUnitDto | null | undefined,
): string {
  if (unit) {
    return unit.shortLabel?.trim() || unit.displayName;
  }
  if (isByWeightSellingMode(product.sellingMode)) {
    return "kg";
  }
  return product.unitOfMeasure;
}

function toCartLine(
  product: PosCatalogProductDto,
  quantity: number,
  unit: PosCatalogProductUnitDto | null | undefined,
): SessionCartLine {
  const productUnitId = unit?.unitId ?? null;
  const multiplier = unit && unit.multiplierToBase > 0 ? unit.multiplierToBase : 1;
  return {
    lineKey: cartLineKey(product.productId, productUnitId),
    productId: product.productId,
    sku: product.sku ?? null,
    name: product.name,
    sellingMode: normalizeSellingMode(product.sellingMode),
    productUnitId,
    unitLabel: unitLabelFor(product, unit),
    multiplierToBase: multiplier,
    unitPrice: resolveSellUnitPrice(product, unit),
    quantity: roundQuantity(quantity),
    baseUnitOfMeasure: product.unitOfMeasure,
  };
}

export function SessionCartProvider({ children }: { children: ReactNode }) {
  const [lines, setLines] = useState<SessionCartLine[]>([]);

  const addLine = useCallback((product: PosCatalogProductDto, options?: AddCartLineOptions) => {
    const delta = options?.quantity ?? 1;
    if (!(delta > 0)) {
      return;
    }
    const unit = options?.unit ?? null;
    const key = cartLineKey(product.productId, unit?.unitId ?? null);
    const replace = options?.replaceQuantity === true;

    setLines((current) => {
      const existingIndex = current.findIndex((line) => line.lineKey === key);
      if (existingIndex >= 0) {
        const nextQty = replace
          ? roundQuantity(delta)
          : roundQuantity(current[existingIndex]!.quantity + delta);
        if (nextQty <= 0) {
          return current.filter((_, index) => index !== existingIndex);
        }
        const refreshed = toCartLine(product, nextQty, unit);
        return current.map((line, index) => (index === existingIndex ? refreshed : line));
      }
      return [...current, toCartLine(product, delta, unit)];
    });
  }, []);

  const addProduct = useCallback(
    (product: PosCatalogProductDto, quantity = 1) => {
      addLine(product, { quantity: Math.max(quantity, 1) });
    },
    [addLine],
  );

  const setLineQuantity = useCallback((lineKey: string, quantity: number) => {
    setLines((current) => {
      if (!(quantity > 0)) {
        return current.filter((line) => line.lineKey !== lineKey);
      }
      const next = roundQuantity(quantity);
      return current.map((line) => (line.lineKey === lineKey ? { ...line, quantity: next } : line));
    });
  }, []);

  const incrementLine = useCallback((lineKey: string) => {
    setLines((current) =>
      current.map((line) =>
        line.lineKey === lineKey ? { ...line, quantity: roundQuantity(line.quantity + 1) } : line,
      ),
    );
  }, []);

  const decrementLine = useCallback((lineKey: string) => {
    setLines((current) =>
      current
        .map((line) =>
          line.lineKey === lineKey ? { ...line, quantity: roundQuantity(line.quantity - 1) } : line,
        )
        .filter((line) => line.quantity > 0),
    );
  }, []);

  const removeLine = useCallback((lineKey: string) => {
    setLines((current) => current.filter((line) => line.lineKey !== lineKey));
  }, []);

  const clear = useCallback(() => {
    setLines((current) => (current.length === 0 ? current : []));
  }, []);

  const getLine = useCallback(
    (lineKey: string) => lines.find((line) => line.lineKey === lineKey),
    [lines],
  );

  const getEnteredQuantity = useCallback(
    (productId: string, productUnitId: string | null) => {
      const key = cartLineKey(productId, productUnitId);
      return lines.find((line) => line.lineKey === key)?.quantity ?? 0;
    },
    [lines],
  );

  const lineCount = lines.length;
  const quantityTotal = useMemo(
    () => lines.reduce((total, line) => total + line.quantity, 0),
    [lines],
  );

  const subtotal = useMemo(
    () => roundMoney(lines.reduce((total, line) => total + line.unitPrice * line.quantity, 0)),
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
      quantityTotal,
      subtotal,
      addProduct,
      addLine,
      setLineQuantity,
      incrementLine,
      decrementLine,
      removeLine,
      clear,
      getLine,
      getEnteredQuantity,
    }),
    [
      addLine,
      addProduct,
      clear,
      decrementLine,
      getEnteredQuantity,
      getLine,
      incrementLine,
      lineCount,
      lines,
      quantityTotal,
      removeLine,
      setLineQuantity,
      subtotal,
    ],
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

export function lineAmount(line: SessionCartLine): number {
  return roundMoney(line.unitPrice * line.quantity);
}
