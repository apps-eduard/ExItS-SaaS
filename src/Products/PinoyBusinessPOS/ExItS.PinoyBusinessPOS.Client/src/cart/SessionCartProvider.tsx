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
  activeSellUnits,
  cartLineKey,
  isByWeightSellingMode,
  resolveSellUnitPrice,
  roundMoney,
  roundQuantity,
} from "@/cart/sell-cart-helpers";
import { registerCartLineCountGetter } from "@/pwa/apply-pwa-update";

export type SessionCartLinePriceOverride = {
  requestedUnitPrice: number;
  reason: string;
  /** Catalog unit price when the override was applied (ExpectedBaselineUnitPrice). */
  expectedBaselineUnitPrice: number;
};

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
  /** Catalog price for the selected sell unit (client preview / override baseline only). */
  unitPrice: number;
  /**
   * Cashier-entered quantity: count for PerItem / pack units, kilograms for ByWeight.
   * Line amount = quantity × effective unit price (override or catalog).
   */
  quantity: number;
  baseUnitOfMeasure: string;
  /** When false (and not ByWeight), quantity must be a whole number. */
  allowsCustomQuantity: boolean;
  /** Pending per-sale price override — never mutates catalog / Today's Price. */
  priceOverride?: SessionCartLinePriceOverride | null;
  /** Catalog thumb when the product was added — used for cart line imagery only. */
  hasImage?: boolean;
  imageVersion?: number | null;
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
  setLinePriceOverride: (lineKey: string, override: SessionCartLinePriceOverride | null) => void;
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

function resolveAllowsCustomQuantity(
  product: PosCatalogProductDto,
  unit: PosCatalogProductUnitDto | null | undefined,
): boolean {
  if (isByWeightSellingMode(product.sellingMode)) {
    return true;
  }
  return unit?.allowsCustomQuantity === true;
}

function isWholeQuantityRequired(line: SessionCartLine): boolean {
  return !line.allowsCustomQuantity && !isByWeightSellingMode(line.sellingMode);
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
    allowsCustomQuantity: resolveAllowsCustomQuantity(product, unit),
    hasImage: product.hasImage === true,
    imageVersion: product.imageVersion ?? null,
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
    const allowsCustom = resolveAllowsCustomQuantity(product, unit);
    if (
      !allowsCustom &&
      !isByWeightSellingMode(product.sellingMode) &&
      delta !== Math.trunc(delta)
    ) {
      return;
    }
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
        const preserved = current[existingIndex]!.priceOverride;
        return current.map((line, index) =>
          index === existingIndex ? { ...refreshed, priceOverride: preserved ?? null } : line,
        );
      }
      return [...current, toCartLine(product, delta, unit)];
    });
  }, []);

  const addProduct = useCallback(
    (product: PosCatalogProductDto, quantity = 1) => {
      const sellUnits = activeSellUnits(product);
      const unit = sellUnits.length === 1 ? sellUnits[0]! : null;
      addLine(product, { quantity: Math.max(quantity, 1), unit });
    },
    [addLine],
  );

  const setLineQuantity = useCallback((lineKey: string, quantity: number) => {
    setLines((current) => {
      if (!(quantity > 0)) {
        return current.filter((line) => line.lineKey !== lineKey);
      }
      const existing = current.find((line) => line.lineKey === lineKey);
      if (!existing) {
        return current;
      }
      if (isWholeQuantityRequired(existing) && quantity !== Math.trunc(quantity)) {
        return current;
      }
      const next = isWholeQuantityRequired(existing)
        ? Math.trunc(quantity)
        : roundQuantity(quantity);
      return current.map((line) => (line.lineKey === lineKey ? { ...line, quantity: next } : line));
    });
  }, []);

  const incrementLine = useCallback((lineKey: string) => {
    setLines((current) =>
      current.map((line) => {
        if (line.lineKey !== lineKey) {
          return line;
        }
        const step = isWholeQuantityRequired(line) ? 1 : 0.001;
        return { ...line, quantity: roundQuantity(line.quantity + step) };
      }),
    );
  }, []);

  const decrementLine = useCallback((lineKey: string) => {
    setLines((current) =>
      current
        .map((line) => {
          if (line.lineKey !== lineKey) {
            return line;
          }
          const step = isWholeQuantityRequired(line) ? 1 : 0.001;
          return { ...line, quantity: roundQuantity(line.quantity - step) };
        })
        .filter((line) => line.quantity > 0),
    );
  }, []);

  const removeLine = useCallback((lineKey: string) => {
    setLines((current) => current.filter((line) => line.lineKey !== lineKey));
  }, []);

  const setLinePriceOverride = useCallback(
    (lineKey: string, override: SessionCartLinePriceOverride | null) => {
      setLines((current) =>
        current.map((line) =>
          line.lineKey === lineKey ? { ...line, priceOverride: override } : line,
        ),
      );
    },
    [],
  );

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
    () => roundMoney(lines.reduce((total, line) => total + lineAmount(line), 0)),
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
      setLinePriceOverride,
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
      setLinePriceOverride,
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

export function effectiveUnitPrice(line: SessionCartLine): number {
  return line.priceOverride?.requestedUnitPrice ?? line.unitPrice;
}

export function lineAmount(line: SessionCartLine): number {
  return roundMoney(effectiveUnitPrice(line) * line.quantity);
}
