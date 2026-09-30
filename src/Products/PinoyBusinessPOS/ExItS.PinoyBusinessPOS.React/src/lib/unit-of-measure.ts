/**
 * Compact unit symbol / abbreviation for qty displays (stepper middle, line labels).
 * Catalog stores full codes (Kilogram); UI shows symbols (kg).
 */
export function formatUnitOfMeasureSymbol(unitOfMeasure?: string | null): string {
  const unit = (unitOfMeasure ?? "").trim();
  if (!unit) {
    return "pc";
  }
  switch (unit.toLowerCase()) {
    case "kilogram":
    case "kilograms":
    case "kg":
    case "kilo":
      return "kg";
    case "gram":
    case "grams":
    case "g":
      return "g";
    case "liter":
    case "litre":
    case "liters":
    case "litres":
    case "l":
      return "L";
    case "milliliter":
    case "millilitre":
    case "milliliters":
    case "millilitres":
    case "ml":
      return "mL";
    case "meter":
    case "metre":
    case "meters":
    case "metres":
    case "m":
      return "m";
    case "piece":
    case "pieces":
    case "pc":
    case "pcs":
      return "pc";
    case "pack":
    case "packs":
      return "pack";
    case "box":
    case "boxes":
      return "box";
    case "bottle":
    case "bottles":
      return "bottle";
    case "can":
    case "cans":
      return "can";
    case "sachet":
    case "sachets":
      return "sachet";
    default:
      return unit;
  }
}
