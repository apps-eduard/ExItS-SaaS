/**
 * Builds the sell line math preview (qty × unit price = amount).
 * Pass already-formatted money strings (e.g. via formatPeso) so currency
 * glyphs are never stored in i18n templates.
 */
export function formatSellLinePreview(
  template: string,
  parts: {
    qty: string;
    unit: string;
    /** Unit price including per-unit suffix, e.g. "₱220.00 / kg". */
    price: string;
    amount: string;
  },
): string {
  return template
    .replaceAll("{qty}", parts.qty)
    .replaceAll("{unit}", parts.unit)
    .replaceAll("{price}", parts.price)
    .replaceAll("{amount}", parts.amount);
}
