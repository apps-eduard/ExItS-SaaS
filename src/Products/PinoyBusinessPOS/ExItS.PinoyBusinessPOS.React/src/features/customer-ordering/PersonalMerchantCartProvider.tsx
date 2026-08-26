import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import {
  cartItemCount,
  cartMerchandiseSubtotal,
  clearCartLines,
  clearPersonalMerchantCart,
  decrementCartLine,
  EMPTY_PERSONAL_MERCHANT_CART,
  ensureMerchantCart,
  getCartQuantity,
  incrementCartLine,
  type PersonalMerchantCartState,
} from "@/features/customer-ordering/personal-merchant-cart";

type PersonalMerchantCartContextValue = {
  cart: PersonalMerchantCartState;
  itemCount: number;
  merchandiseSubtotal: number;
  ensureMerchant: (sellerOrganizationId: string, displayName: string | null) => void;
  increment: (product: CustomerStorefrontProductDto) => void;
  decrement: (productId: string) => void;
  clearLines: () => void;
  clearAll: () => void;
  quantityOf: (productId: string) => number;
};

const PersonalMerchantCartContext = createContext<PersonalMerchantCartContextValue | null>(null);

export function PersonalMerchantCartProvider({ children }: { children: ReactNode }) {
  const [cart, setCart] = useState<PersonalMerchantCartState>(EMPTY_PERSONAL_MERCHANT_CART);

  const ensureMerchant = useCallback((sellerOrganizationId: string, displayName: string | null) => {
    setCart((prev) => ensureMerchantCart(prev, sellerOrganizationId, displayName));
  }, []);

  const increment = useCallback((product: CustomerStorefrontProductDto) => {
    setCart((prev) => incrementCartLine(prev, product));
  }, []);

  const decrement = useCallback((productId: string) => {
    setCart((prev) => decrementCartLine(prev, productId));
  }, []);

  const clearLines = useCallback(() => {
    setCart((prev) => clearCartLines(prev));
  }, []);

  const clearAll = useCallback(() => {
    setCart(clearPersonalMerchantCart());
  }, []);

  const quantityOf = useCallback((productId: string) => getCartQuantity(cart, productId), [cart]);

  const value = useMemo(
    () => ({
      cart,
      itemCount: cartItemCount(cart),
      merchandiseSubtotal: cartMerchandiseSubtotal(cart),
      ensureMerchant,
      increment,
      decrement,
      clearLines,
      clearAll,
      quantityOf,
    }),
    [cart, ensureMerchant, increment, decrement, clearLines, clearAll, quantityOf],
  );

  return (
    <PersonalMerchantCartContext.Provider value={value}>
      {children}
    </PersonalMerchantCartContext.Provider>
  );
}

export function usePersonalMerchantCart(): PersonalMerchantCartContextValue {
  const ctx = useContext(PersonalMerchantCartContext);
  if (!ctx) {
    throw new Error("usePersonalMerchantCart requires PersonalMerchantCartProvider");
  }
  return ctx;
}
