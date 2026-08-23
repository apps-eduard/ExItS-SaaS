import type { LucideIcon } from "lucide-react";
import {
  Apple,
  Baby,
  Beef,
  Bone,
  CakeSlice,
  Candy,
  Cigarette,
  Coffee,
  Cookie,
  Croissant,
  CupSoda,
  Droplets,
  Egg,
  Fish,
  GlassWater,
  Grid2X2,
  IceCreamCone,
  Leaf,
  Milk,
  Package,
  Pill,
  Pizza,
  Sandwich,
  ShoppingBasket,
  Soup,
  Sparkles,
  SprayCan,
  Wheat,
  Wine,
} from "lucide-react";

/**
 * Map category display names to Lucide icons.
 * Categories have no image field in the catalog API — icons are visual-only fallbacks.
 */
export function resolveSellCategoryIcon(name: string | null | undefined): LucideIcon {
  const key = (name ?? "").trim().toLowerCase();
  if (!key || key === "all" || key === "amin" || key === "tanan") {
    return Grid2X2;
  }

  if (/bak(e|ed|ery)|bread|pastry|cake|cookie|biscuit|snack/.test(key)) {
    return Cookie;
  }
  if (/beverage|drink|soda|juice|coffee|tea|water/.test(key)) {
    return CupSoda;
  }
  if (/alcohol|wine|beer|liquor/.test(key)) {
    return Wine;
  }
  if (/dairy|milk|cheese|yogurt/.test(key)) {
    return Milk;
  }
  if (/meat|poultry|chicken|pork|beef/.test(key)) {
    return Beef;
  }
  if (/seafood|fish|shrimp/.test(key)) {
    return Fish;
  }
  if (/produce|fruit|vegetable|veggie|salad/.test(key)) {
    return Apple;
  }
  if (/rice|grain|cereal|flour|wheat/.test(key)) {
    return Wheat;
  }
  if (/canned|can |tin /.test(key)) {
    return Package;
  }
  if (/condiment|sauce|spice|season/.test(key)) {
    return Soup;
  }
  if (/frozen|ice cream/.test(key)) {
    return IceCreamCone;
  }
  if (/clean|detergent|soap|hygiene/.test(key)) {
    return SprayCan;
  }
  if (/personal|beauty|care/.test(key)) {
    return Sparkles;
  }
  if (/baby|infant/.test(key)) {
    return Baby;
  }
  if (/pharma|medicine|drug|vitamin/.test(key)) {
    return Pill;
  }
  if (/tobacco|cigarette/.test(key)) {
    return Cigarette;
  }
  if (/oil|liquid/.test(key)) {
    return Droplets;
  }
  if (/egg/.test(key)) {
    return Egg;
  }
  if (/bone|pet/.test(key)) {
    return Bone;
  }
  if (/candy|sweet|chocolate/.test(key)) {
    return Candy;
  }
  if (/sandwich|burger|fast food/.test(key)) {
    return Sandwich;
  }
  if (/pizza/.test(key)) {
    return Pizza;
  }
  if (/croissant|pastry/.test(key)) {
    return Croissant;
  }
  if (/cake|dessert/.test(key)) {
    return CakeSlice;
  }
  if (/coffee/.test(key)) {
    return Coffee;
  }
  if (/water/.test(key)) {
    return GlassWater;
  }
  if (/organic|natural|leaf/.test(key)) {
    return Leaf;
  }
  if (/grocery|general|misc|other/.test(key)) {
    return ShoppingBasket;
  }

  return Package;
}
