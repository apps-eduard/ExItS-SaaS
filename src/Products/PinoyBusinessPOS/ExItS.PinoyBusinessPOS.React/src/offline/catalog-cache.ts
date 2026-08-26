import type { PosCatalogProductDto, PosProductCategoryDto } from "@/api/pos/pos-catalog-types";
import type { OfflineDb } from "@/offline/db";
import type { CachedCatalogCategoryRecord, CachedCatalogProductRecord } from "@/offline/types";

/**
 * Read-only Sell catalog cache (RMAP-21D).
 * Write-through from a successful online browse only — never invents products, prices, or stock.
 * Every read fails closed to an empty list so an unavailable cache can never look like an empty shop
 * with authority.
 */

export async function replaceCatalogCache(
  db: OfflineDb,
  products: ReadonlyArray<PosCatalogProductDto>,
  categories: ReadonlyArray<PosProductCategoryDto>,
): Promise<void> {
  const cachedAtUtc = new Date().toISOString();

  const productTx = db.transaction("catalogProducts", "readwrite");
  await productTx.store.clear();
  for (const product of products) {
    const record: CachedCatalogProductRecord = {
      productId: product.productId,
      cachedAtUtc,
      product,
    };
    await productTx.store.put(record);
  }
  await productTx.done;

  const categoryTx = db.transaction("catalogCategories", "readwrite");
  await categoryTx.store.clear();
  for (const category of categories) {
    const record: CachedCatalogCategoryRecord = {
      categoryId: category.categoryId,
      cachedAtUtc,
      category,
    };
    await categoryTx.store.put(record);
  }
  await categoryTx.done;
}

export async function listCachedCatalogProducts(db: OfflineDb): Promise<PosCatalogProductDto[]> {
  try {
    const rows = await db.getAll("catalogProducts");
    return rows.map((row) => row.product).filter((product) => product != null);
  } catch {
    return [];
  }
}

export async function listCachedCatalogCategories(db: OfflineDb): Promise<PosProductCategoryDto[]> {
  try {
    const rows = await db.getAll("catalogCategories");
    return rows.map((row) => row.category).filter((category) => category != null);
  } catch {
    return [];
  }
}
