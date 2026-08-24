import { useEffect, useState } from "react";
import { getCustomerStorefrontProductImage } from "@/api/pos/pos-customer-orders-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";

/**
 * Loads a storefront product thumb from the seller org customer-order API.
 * Revokes the object URL on unmount / product change. Never invents image paths.
 */
export function useStorefrontProductImageUrl(
  workspace: PosWorkspaceScope | null,
  sellerOrganizationId: string | null | undefined,
  productId: string | null | undefined,
  hasImage: boolean | undefined,
  imageVersion?: number | null,
): string | null {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!workspace || !sellerOrganizationId || !productId || !hasImage) {
      setUrl(null);
      return;
    }

    let cancelled = false;
    let objectUrl: string | null = null;
    const controller = new AbortController();

    void getCustomerStorefrontProductImage(
      workspace,
      sellerOrganizationId,
      productId,
      "thumb",
      controller.signal,
    )
      .then((blob) => {
        if (cancelled) {
          return;
        }
        objectUrl = URL.createObjectURL(blob);
        setUrl(objectUrl);
      })
      .catch(() => {
        if (!cancelled) {
          setUrl(null);
        }
      });

    return () => {
      cancelled = true;
      controller.abort();
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [workspace, sellerOrganizationId, productId, hasImage, imageVersion]);

  return url;
}
