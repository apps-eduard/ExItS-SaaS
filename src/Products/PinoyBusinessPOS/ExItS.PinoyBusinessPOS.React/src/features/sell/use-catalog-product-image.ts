import { useEffect, useState } from "react";
import { getCatalogProductImage } from "@/api/pos/pos-catalog-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";

/**
 * Loads a catalog product thumb as an object URL when the product reports hasImage.
 * Revokes the URL on unmount / product change. Never invents image paths.
 */
export function useCatalogProductImageUrl(
  workspace: PosWorkspaceScope | null,
  productId: string | null | undefined,
  hasImage: boolean | undefined,
  imageVersion?: number | null,
): string | null {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!workspace || !productId || !hasImage) {
      setUrl(null);
      return;
    }

    let cancelled = false;
    let objectUrl: string | null = null;
    const controller = new AbortController();

    void getCatalogProductImage(workspace, productId, "thumb", controller.signal)
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
  }, [workspace, productId, hasImage, imageVersion]);

  return url;
}
