import { useEffect, useState } from "react";
import { getCatalogProductImage } from "@/api/pos/pos-catalog-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";

/**
 * Loads a catalog product thumb as an object URL when the product reports hasImage.
 * Revokes the URL on unmount / product change. Never invents image paths.
 *
 * Dependencies use primitive workspace ids (not the workspace object) so parent
 * re-renders that allocate a new scope object do not re-fetch every visible card —
 * that storm was especially costly on desktop where more tiles are on screen.
 */
export function useCatalogProductImageUrl(
  workspace: PosWorkspaceScope | null,
  productId: string | null | undefined,
  hasImage: boolean | undefined,
  imageVersion?: number | null,
): string | null {
  const [url, setUrl] = useState<string | null>(null);
  const organizationId = workspace?.organizationId ?? null;
  const branchId = workspace?.branchId ?? null;

  useEffect(() => {
    if (!organizationId || !branchId || !productId || !hasImage) {
      setUrl(null);
      return;
    }

    let cancelled = false;
    let objectUrl: string | null = null;
    const controller = new AbortController();
    const scope: PosWorkspaceScope = { organizationId, branchId };

    void getCatalogProductImage(scope, productId, "thumb", controller.signal)
      .then((blob) => {
        if (cancelled) {
          return;
        }
        objectUrl = URL.createObjectURL(blob);
        if (cancelled) {
          URL.revokeObjectURL(objectUrl);
          objectUrl = null;
          return;
        }
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
  }, [organizationId, branchId, productId, hasImage, imageVersion]);

  return url;
}
